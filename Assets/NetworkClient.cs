using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

namespace RMSIDCUTILS.Network
{
    public class NetworkClient
    {
        #region Local Properties
        private bool _isLocal = false;
        readonly TcpClient _client;
        readonly byte[] buffer = new byte[5000];
        readonly ConnectInfo _connectInfo;

        NetworkStream Stream
        {
            get { return _client.GetStream(); }
        }
        #endregion  

        public Guid ClientID { get; set; }
        public event EventHandler<DataReceivedEvent> DataReceived;

        public NetworkClient()
        {
            ClientID = Guid.NewGuid();
        }

        public NetworkClient(ConnectInfo info) : base()
        {
            _connectInfo = info ?? throw new ArgumentNullException("info", "The info object should contain a reference to a ConnectionInfo instance");
        }

        public NetworkClient(TcpClient client, bool isConnected = true) : base()
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

        internal void Close()
        {
            _client.Close();
        }

        void OnRead(IAsyncResult ar)
        {
            NetworkManager.instance._Text.text = "S: Block on EndRead";
            int length = Stream.EndRead(ar);
            if (length <= 0)
            {
                // NetworkManager.instance._Text.text = "S: Connection Closed";
                // NetworkManager.instance.OnClientDisconnected(this);

                // <message type="Disconnected">{0}</message>
                // <message type="{0}" id="{1}">{2}</message>
                // <message type="event" id="{0}">Ready</message>
                // <message type="event" id="{0}">Connected</message>
                // <message type="action" id="">Play</message>
                // <message type="action" id="">Pause</message>
                // <message type="action" id="">Rewind</message>


                OnDataReceived(new DataReceivedEvent(string.Format("id:{0}", ClientID.ToString())));
                return;
            }

            NetworkManager.instance._Text.text = "S: Reading data";
            string newMessage = System.Text.Encoding.UTF8.GetString(buffer, 0, length);
            // NetworkManager.message += newMessage + Environment.NewLine;

            var receivedData = System.Text.Encoding.Default.GetString(buffer);

            Debug.Log("Recieved message " + buffer);
            //NetworkManager.instance._Text.text = "S: Got some data. " + receivedData;

            OnDataReceived(new DataReceivedEvent(receivedData));

            // Look for more data from the server
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
            NetworkManager.instance._Text.text = "This client is starting the network connection";

            var client = (TcpClient)ar.AsyncState;

            client.EndConnect(ar);

            NetworkManager.instance._Text.text = "This client has connected to the network server";
            Debug.Log("Client connect ended");
            Stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
        }

        public void Send(string message)
        {
            NetworkManager.instance._Text.text = "Sending a message maybe";

            Debug.Log("Send message from client to server ");
            byte[] data = System.Text.Encoding.UTF8.GetBytes(message);

            if (_client == null)
            {
                NetworkManager.instance._Text.text = "Send: Client is null";
                return;
            }

            var stream = _client.GetStream();

            if (stream == null)
            {
                NetworkManager.instance._Text.text = "Send: Stream is null";
                return;
            }

            // setup async reading before sending the message, since connection blocks
            if (_isLocal == false) // this is a remote client connecting to a TCP Server 
            {
                NetworkManager.instance._Text.text = "Setup the read buffer";
                stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
            }

            //NetworkMan1.instance._Text.text = string.Format("Buffer length is {0}", data.Length);
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }

        public bool IsConnected()
        {
            return _client != null ? _client.Connected : false;
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