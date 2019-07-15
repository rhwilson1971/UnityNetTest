using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

namespace RMSIDCUTILS.Network
{
    public class PrimeNetClient
    {
        #region Local Properties
        private bool _isLocal = false;
        readonly TcpClient _client;
        readonly byte[] buffer = new byte[5000];
        readonly ConnectionInfo _connectInfo;

        NetworkStream Stream
        {
            get { return _client.GetStream(); }
        }
        #endregion

        #region Public Properties
        public Guid ClientID { get; set; }
        #endregion

        #region Constructors
        public event EventHandler<DataReceivedEvent> DataReceived;

        public PrimeNetClient()
        {
            ClientID = Guid.NewGuid();
        }

        public PrimeNetClient(ConnectionInfo info) : base()
        {
            _connectInfo = info ?? throw new ArgumentNullException("info", "The info object should contain a reference to a ConnectionInfo instance");
        }

        public PrimeNetClient(TcpClient client, bool isConnected = true) : base()
        {
            Debug.Log("Initializing network client");

            _client = client;
            if (isConnected)
            {
                _isLocal = true;
                _client.NoDelay = true;
                // NetworkManager.instance._Text.text = "S: Client connected to this server";
                Stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
                // NetworkManager.instance._Text.text = "S: Read ended";
            }
        }
        #endregion

        #region Private Implementation
        internal void Close()
        {
            _client.Close();
        }

        void OnRead(IAsyncResult ar)
        {
            Debug.Log("Beginning to receive data");
            
            int length = Stream.EndRead(ar);
            if (length <= 0)
            {
                Debug.Log("Someone disconnected");
                PrimeNetService.Instance._Text.text = "Someone disconnected";
                OnDataReceived(new DataReceivedEvent(""));
                return;
            }

            string newMessage = System.Text.Encoding.UTF8.GetString(buffer, 0, length);
            var receivedData = System.Text.Encoding.Default.GetString(buffer);

            Debug.Log("Recieved message " + receivedData);
            OnDataReceived(new DataReceivedEvent(receivedData));

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

        // Wrap event invocations inside a protected virtual method
        // to allow derived classes to override the event invocation behavior
        protected virtual void OnDataReceived(DataReceivedEvent e)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            DataReceived?.Invoke(this, e);
        }

        #endregion

        #region Public Interface
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