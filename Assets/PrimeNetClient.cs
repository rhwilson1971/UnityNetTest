using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace RMSIDCUTILS.Network
{
    public interface IPrimeNetClient
    {
        bool Poll();
        void Disconnect();
        void StartHeartbeatTimer();
    }


    public class PrimeNetClient : IPrimeNetClient
    {
        private PrimeNetHeartbeatTimer _hbTimer;

        #region Local Properties

        private ManualResetEvent _heartbeatEvent = new ManualResetEvent(false);


        private bool _isLocal = false;
        private readonly TcpClient _client;
        private readonly Socket _socket;
        private readonly byte[] buffer = new byte[5000];
        private readonly ConnectionInfo _connectInfo;
        private EndPoint _endPoint;

        private NetworkStream Stream
        {
            get { return _client.GetStream(); }
        }

        private NetworkStream _stream;
        private ManualResetEvent _connectionPollEvent = new ManualResetEvent(false);
        #endregion

        #region Public Properties
        public Guid ClientID { get; set; }
        public int ClientNumber { get; set; }
        public TcpClient GetClient() { return _client; }
        public Socket GetSocket() { return _socket; }
        public EndPoint RemoteEndPoint;
        #endregion

        #region Constructors
        public event EventHandler<DataReceivedEvent> DataReceived;

        public PrimeNetClient()
        {
            ClientID = Guid.NewGuid();
            Debug.Log("Creating a new GUID" + ClientID.ToString());
        }

        public PrimeNetClient(ConnectionInfo info) : this()
        {
            _connectInfo = info ?? throw new ArgumentNullException("info", "The info object should contain a reference to a ConnectionInfo instance");
        }

        public PrimeNetClient(TcpClient client, bool isConnected = true) : this()
        {
            Debug.Log("Initializing network client");

            _client = client;
            if (isConnected)
            {
                _isLocal = true;
                _client.NoDelay = true;
                Stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
            }
        }

        public PrimeNetClient(Socket socket, bool isConnected = true) : this()
        {
            Debug.Log("Initializing network socket");

            _socket = socket;

            if (isConnected)
            {
                Debug.Log("In is netclient connected");
                _isLocal = true;
                _socket.NoDelay = true;
                _stream = new NetworkStream(_socket);
                _stream.BeginRead(buffer, 0, buffer.Length, OnSocketRead, null);
                Debug.Log("In here?");
            }
        }
        #endregion

        #region Private Implementation
        internal void Close()
        {
            _client.Close();
        }

        private void OnSocketRead(IAsyncResult ar)
        {
            _heartbeatEvent.Set();

            Debug.Log("Beginning to receive data");
            int length = _stream.EndRead(ar);
            if (length <= 0)
            {
                Debug.Log("Someone disconnected");

                PrimeNetMessage
                     message = new PrimeNetMessage
                     {
                         MessageBody = "Disconnected from remote end",
                         NetMessage = _connectInfo.IsServer ? EPrimeNetMessage.ClientConnected : EPrimeNetMessage.ServerDisconnected,
                         SenderIP = _connectInfo.HosHostAddress.ToString()
                     };

                PublishDataReceived(new DataReceivedEvent(message.Serialize()));
                return;
            }

            string newMessage = System.Text.Encoding.UTF8.GetString(buffer, 0, length);
            var receivedData = System.Text.Encoding.Default.GetString(buffer);

            Debug.Log("Recieved message " + receivedData);
            PublishDataReceived(new DataReceivedEvent(receivedData));

            // Clear current buffer and look for more data from the server
            Array.Clear(buffer, 0, buffer.Length);
            _stream.BeginRead(buffer, 0, buffer.Length, OnSocketRead, null);
        }

        private void OnRead(IAsyncResult ar)
        {
            _connectionPollEvent.Reset();

            Debug.Log("Beginning to receive data");
            int length = Stream.EndRead(ar);
            if (length <= 0)
            {
                Debug.Log("Someone disconnected");

                PrimeNetMessage
                     message = new PrimeNetMessage
                     {
                         MessageBody = "Disconnected from remote end",
                         NetMessage = _connectInfo.IsServer ? EPrimeNetMessage.ClientConnected : EPrimeNetMessage.ServerDisconnected,
                         SenderIP = _connectInfo.HosHostAddress.ToString()
                     };

                PublishDataReceived(new DataReceivedEvent(message.Serialize()));
                return;
            }

            string newMessage = System.Text.Encoding.UTF8.GetString(buffer, 0, length);
            var receivedData = System.Text.Encoding.Default.GetString(buffer);

            Debug.Log("Recieved message " + receivedData);
            PublishDataReceived(new DataReceivedEvent(receivedData));

            // Clear current buffer and look for more data from the server
            Array.Clear(buffer, 0, buffer.Length);
            Stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
        }

        /// <summary>
        // This Async method is called when the client actually connects
        // to the server
        /// </summary>
        /// <param name="ar"></param>
        internal void EndConnect(IAsyncResult ar)
        {
            Debug.Log("Client Connected");

            var client = (TcpClient)ar.AsyncState;
            client.EndConnect(ar); // this will block until the TCP connection is completed

            // Once connection completes, this will setup this TCP connection to wait for data 
            // on it's network stream, when data is received, the OnRead method will be called
            // this is a non-blocking call, so this method will end once BeginRead starts
            Debug.Log("Client connect ended");
            Stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
        }

        public void Read()
        {
            Stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
        }

        public void SocketRead()
        {
            Debug.Log("Socket read setup ");
            if (_stream == null)
            {
                _stream = new NetworkStream(_socket);
            }
            _stream.BeginRead(buffer, 0, buffer.Length, OnSocketRead, null);
        }

        // Wrap event invocations inside a protected virtual method
        // to allow derived classes to override the event invocation behavior
        protected virtual void PublishDataReceived(DataReceivedEvent e)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            DataReceived?.Invoke(this, e);
        }

        #endregion

        #region Public Interface
        public void SocketSend(string message)
        {
            Debug.Log("Send message from client to server ");
            byte[] data = System.Text.Encoding.UTF8.GetBytes(message);

            if (_socket == null)
            {
                return;
            }

            Debug.Log("Is this local? " + _isLocal);

            // setup async reading before sending the message, since connection blocks
            if (_isLocal == false) // this is a remote client connecting to a TCP Server 
            {
                Debug.Log("Re-registering?");
                _stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
            }

            Debug.Log("Actually sending message");
            _stream.Write(data, 0, data.Length);
            _stream.Flush();
        }

        public void Send(string message)
        {
            Debug.Log("Send message from client to server ");
            byte[] data = System.Text.Encoding.UTF8.GetBytes(message);

            if (_client == null)
            {
                //StatusMessage(EPrimeNetMessage.Generic, "Send: Client is null");
                return;
            }

            var stream = _client.GetStream();
            if (stream == null)
            {
                //NetworkService.instance._Text.text = "Send: Stream is null";
                return;
            }

            // setup async reading before sending the message, since connection blocks
            if (_isLocal == false) // this is a remote client connecting to a TCP Server 
            {
                Debug.Log("Re-registering?");
                stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
            }

            Debug.Log("Actually sending message");
            //NetworkMan1.instance._Text.text = string.Format("Buffer length is {0}", data.Length);
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }

        public bool IsConnected()
        {
            return _client != null ? _client.Connected : false;
        }

        public bool IsSocketConnected()
        {
            bool isConnected = false;

            // lock (_socket)
            {
                // .Connect throws an exception if unsuccessful
                //_socket.Connect(RemoteEndPoint);

                // This is how you can determine whether a socket is still connected.
                bool blockingState = _socket.Blocking;
                try
                {
                    byte[] tmp = new byte[1];

                    _socket.Blocking = false;
                    _socket.Send(tmp, 0, 0);
                    isConnected = true;
                    Debug.Log("The socket should be connected");
                }
                catch (SocketException e)
                {
                    // 10035 == WSAEWOULDBLOCK
                    if (e.NativeErrorCode.Equals(10035))
                    {
                        Debug.Log("Still Connected, but the Send would block");
                    }
                    else
                    {
                        Debug.Log(string.Format("Disconnected: error code {0}!", e.NativeErrorCode));
                    }
                }
                finally
                {
                    _socket.Blocking = blockingState;
                }
            }
            return isConnected;
        }
        #endregion

        #region IPrimeNetClient Interfaces
        public bool Poll()
        {
            return IsSocketConnected();
        }

        public void Disconnect()
        {
            if (_hbTimer != null)
            {
                _hbTimer.Stop();
            }

            PrimeNetMessage message = new PrimeNetMessage
            {
                MessageBody = "Disconnected from remote end",
                NetMessage = _connectInfo.IsServer ? EPrimeNetMessage.ClientConnected : EPrimeNetMessage.ServerDisconnected,
                SenderIP = _connectInfo.HosHostAddress.ToString()
            };

            PublishDataReceived(new DataReceivedEvent(message.Serialize()));
        }

        public void StartHeartbeatTimer()
        {
            _hbTimer = new PrimeNetHeartbeatTimer(this, 3);
            _hbTimer.Start();
        }
        #endregion
    }

    public class DataReceivedEvent : EventArgs
    {
        public DataReceivedEvent(string data)
        {
            Data = data;
        }
        public string Data { get; private set; }
    }
}