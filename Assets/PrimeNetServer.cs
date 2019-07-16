﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System;
using UnityEngine.UI;
using System.Xml.Serialization;
using System.Threading.Tasks;

namespace RMSIDCUTILS.Network
{
    public interface INetworkServer
    { 
        void StartupClient();
        void StartupServer();

        void SendToClients(string message);
        void SendToServer(string message);

        void OnDataReceived(object sender, DataReceivedEvent e);
        void OnServerConnect(IAsyncResult ar);
        void OnClientDisconnected(PrimeNetClient client);
    }

    public class PrimeNetServer : INetworkServer
    {
        #region Private Properties
        TcpListener _listener;
        ConnectionInfo _conn;
        List<PrimeNetClient> _clientList = new List<PrimeNetClient>();
        bool _isConnecting = true;
        ManualResetEvent _quitAppEvent = new ManualResetEvent(false);
        #endregion

        #region Constructors
        public PrimeNetServer(ConnectionInfo connection)
        {
            _conn = connection;
        }

        public PrimeNetServer()
        {
            _conn = new ConnectionInfo()
            {
                HosHostAddress = IPAddress.Parse("127.0.0.1"),
                IsServer = true,
                Port = ConnectionInfo.DefaultPort,
                Protocol = 0
            };
        }
        #endregion

        #region Message Handlers
        public event EventHandler<NetworkMessageEvent> NetworkMessageReceived;
        #endregion

        private void Awake()
        {
            //instance = this;
            //Debug.Log("Is server? " + _IsServer);

            //_conn.IsServer = _IsServer;
            //_conn.HosHostAddress = IPAddress.Parse(_IpAddress);
            //_conn.Port = (uint)_Port;

            //Startup();
        }

        public void Startup()
        {
            if(_conn.IsServer)
            {
                StartupServer();
            }
            else
            {
                StartSocketClient();
            }
        }

        public void StartupServer()
        {
            ListenForConnections();
        }

        public void ListenForConnections()
        {
            // Security.PrefetchSocketPolicy(_ipAddress, _port);
            Debug.Log("Listen for connections ");

            _listener = new TcpListener(_conn.HosHostAddress, (int)_conn.Port);
            _listener.Start();

            StatusMessage("Asynchronously listening for connections");
            // _listener.BeginAcceptTcpClient(OnServerConnect, null); // async function
            _listener.BeginAcceptSocket(OnServerSocketConnect, null);
        }

        public void OnServerSocketConnect(IAsyncResult ar)
        {
            Debug.Log("Client connecting");

            Socket socket = _listener.EndAcceptSocket(ar);

            PrimeNetClient nc = new PrimeNetClient(socket)
            {
                ClientNumber = _clientList.Count + 1,
                RemoteEndPoint = new IPEndPoint(_conn.HosHostAddress.Address, (int)_conn.Port)
            };

            _clientList.Add(nc);

            nc.DataReceived += OnDataReceived;

            Debug.Log("Listen for further connections");
            StatusMessage("The server is listening for new connections");

            var message = new PrimeNetMessage()
            {
                MessageBody = nc.ClientNumber.ToString(),
                NetMessage = EPrimeNetMessage.ClientConnected
            };

            NetworkMessageEvent e = new NetworkMessageEvent(message);
            PublishNetworkMessage(e);

            _listener.BeginAcceptSocket(OnServerSocketConnect, null);
        }

        public void OnServerConnect(IAsyncResult ar)
        {
            Debug.Log("Client connecting");

            TcpClient client = _listener.EndAcceptTcpClient(ar);
            PrimeNetClient nc = new PrimeNetClient(client)
            {
                ClientNumber = _clientList.Count + 1
            };
            _clientList.Add(nc);

            //PrimeNetService.Instance._Text.text = "The client connected: " + nc.ClientID;
            nc.DataReceived += OnDataReceived;

            Debug.Log("Listen for further connections");
            StatusMessage("The server is listening for new connections");

            var message = new PrimeNetMessage()
            {
                MessageBody = nc.ClientNumber.ToString(),
                NetMessage = EPrimeNetMessage.ClientConnected
            };

            NetworkMessageEvent e = new NetworkMessageEvent(message);
            PublishNetworkMessage(e);

            _listener.BeginAcceptTcpClient(OnServerConnect, null);
        }

        public void OnDataReceived(object sender, DataReceivedEvent e)
        {
            Debug.Log("Got a message from a network client ");
            Debug.Log("what message ? " + e);
            Debug.Log("what message ? " + e.Data);

            var netMsg = PrimeNetMessage.Deserialize(e.Data);
            Debug.Log("message desrialized Body of message is {" + netMsg.MessageBody + "}");
            PublishNetworkMessage(new NetworkMessageEvent(netMsg));
        }

        public void OnClientDisconnected(PrimeNetClient client)
        {
            Debug.Log("removing network client");
            StatusMessage("Client Diconnected");
            client.DataReceived -= OnDataReceived;
            _clientList.Remove(client);
        }

        public void StartupClient()
        {
            if (_conn.IsServer)
            {
                return;
            }

            StatusMessage("Starting up the network client");
            Debug.Log("Startup client");

            TcpClient client = new TcpClient();
            PrimeNetClient connectedClient = new PrimeNetClient(client, false);

            connectedClient.DataReceived += OnDataReceived;
            _clientList.Add(connectedClient);

            BeginServerConnection(connectedClient);
        }

        public void StartSocketClient()
        {
            if (_conn.IsServer)
            {
                return;
            }

            StatusMessage("Starting up the network client");
            Debug.Log("Startup client");

            // TcpClient client = new TcpClient();
            // PrimeNetClient connectedClient = new PrimeNetClient(client, false);

            //connectedClient.DataReceived += OnDataReceived;
            //_clientList.Add(connectedClient);

            // BeginServerConnection(connectedClient);

            
            
            IPEndPoint localEndPoint = new IPEndPoint(_conn.HosHostAddress.Address, (int)_conn.Port);
            Socket sender = new Socket(_conn.HosHostAddress.AddressFamily,
                               SocketType.Stream, ProtocolType.Tcp);

            PrimeNetClient client = new PrimeNetClient(sender, false)
            {
                RemoteEndPoint = localEndPoint
            };

            client.DataReceived += OnDataReceived;
            _clientList.Add(client);

            BeginServerConnection(client, localEndPoint);
        }

        public void SendToClients(string message)
        {
            Debug.Log("Sending a message to the clients from the server ");
            
            StatusMessage("Sending a message back to the clients");
            if (!_conn.IsServer || string.IsNullOrEmpty(message))
                return;

            var status =
                string.Format("Sending a message back to the clients again - connected client count: {0}", _clientList.Count);
            StatusMessage(status);

            foreach (var client in _clientList)
            {
                client.Send(message);
            }
        }

        public void SendToServer(string message)
        {
            Debug.Log("Sending message to server");
            if (_conn.IsServer)
                return;

            // should only be one, unless disconnect code is not written properly
            foreach (var client in _clientList)
            {
                Debug.Log("Found client");
                if (client.IsConnected())
                {
                    Debug.Log("sending message to server from client");
                    client.Send(message);
                }
            }
        }

        public void PublishNetworkMessage(NetworkMessageEvent e)
        {
            if (NetworkMessageReceived != null)
            {
                NetworkMessageReceived?.Invoke(this, e);
            }
        }

        public void Shutdown()
        {
            foreach(var client in _clientList)
            {
                if (client.IsConnected())
                    client.Close();
            }

            _listener.Stop();
        }

        public void Send(Guid id, PrimeNetMessage message)
        {
            foreach(var c in _clientList)
            {
                if (c.ClientID == id)
                {
                    c.Send(message.Serialize());
                }
            }
        }

        //public string ToXML()
        //{
        //    using (var stringwriter = new System.IO.StringWriter())
        //    {
        //        var serializer = new XmlSerializer(this.GetType());
        //        serializer.Serialize(stringwriter, this);
        //        return stringwriter.ToString();
        //    }
        //}

        public static PrimeNetMessage LoadFromXMLString(string xmlText)
        {
            using (var stringReader = new System.IO.StringReader(xmlText))
            {
                var serializer = new XmlSerializer(typeof(PrimeNetMessage));
                return serializer.Deserialize(stringReader) as PrimeNetMessage;
            }
        }

        public void Broadcast(PrimeNetMessage message)
        {
            Debug.Log("broadcasting message " + message.MessageBody);
            StatusMessage("broadcasting message " + message.MessageBody);

            foreach (var client in _clientList)
            {
                if (client.IsConnected())
                {
                    message.SenderIP = _conn.HosHostAddress.ToString();
                    StatusMessage("Sending to a connected client");
                    client.Send(message.Serialize());
                }
            }
        }

        public void Broadcast2(PrimeNetMessage message)
        {
            Debug.Log("broadcasting message " + message.MessageBody);
            StatusMessage("broadcasting message " + message.MessageBody);

            foreach (var client in _clientList)
            {
                if (client.IsSocketConnected())
                {
                    message.SenderIP = _conn.HosHostAddress.ToString();
                    StatusMessage("Sending to a connected client");
                    client.SocketSend(message.Serialize());
                }
            }
        }

        public void StatusMessage(string statusText)
        {
            var message = new PrimeNetMessage() { NetMessage = EPrimeNetMessage.Status, MessageBody = statusText };

            PublishNetworkMessage(new NetworkMessageEvent(message));
        }

        void ConnectToServer(PrimeNetClient client)
        {
            _isConnecting = true;
            ManualResetEvent tryConnect = new ManualResetEvent(false); // this is a wait timer essentially
            while (_isConnecting)
            {
                try
                {
                    client.GetClient().Connect(_conn.HosHostAddress, (int)_conn.Port);
                    client.Read();
                    _isConnecting = false;
                    // _connectFinished.Reset();
                }
                catch(ObjectDisposedException ex)
                {
                    Debug.Log(ex.Message);
                }
                catch(SocketException ex)
                {
                    Debug.Log(ex.Message);
                }

                if(_isConnecting == true )
                {
                    tryConnect.WaitOne(1000); // wait for 1 second
                }
            }
        }

        void ConnectToServer(PrimeNetClient client, IPEndPoint endPoint)
        {
            Debug.Log("Connecting to the server...");
            _isConnecting = true;
            // ManualResetEvent tryConnect = new ManualResetEvent(false); // this is a wait timer essentially
            _quitAppEvent.Reset();

            while (_isConnecting)
            {
                try
                {
                    client.GetSocket().Connect(endPoint);
                    client.SocketRead();
                    _isConnecting = false;
                }
                catch (ObjectDisposedException ex)
                {
                    Debug.Log(ex.Message);
                }
                catch (SocketException ex)
                {
                    Debug.Log(ex.Message);
                }

                if (_isConnecting == true)
                {
                    // this signal is from the outside, if the application is told to 
                    // quit, stop this task.  If th task expires, and not connection is there, keep trying to connect
                    // when connected, set _isConnecting to false, which also stops this task
                    var isSignaled = _quitAppEvent.WaitOne(1000);

                    if(isSignaled)
                    {
                        _isConnecting = false;
                    }
                }
            }
        }

        void StopConnection()
        {
            if (_isConnecting)
            {
                _quitAppEvent.Set();
            }
        }

        //void ConnectionCompleted(IAsyncResult ar)
        //{
        //    Debug.Log("Client Connected");

        //    var client = (PrimeNetClient)ar.AsyncState;
        //    client.EndConnect(ar); // this will block until the TCP connection is completed


        //    // Once connection completes, this will setup this TCP connection to wait for data 
        //    // on it's network stream, when data is received, the OnRead method will be called
        //    // this is a non-blocking call, so this method will end once BeginRead starts
        //    Debug.Log("Client connect ended");
        //    client.Read();

        //    _connectFinished.Set();
        //}

        void BeginServerConnection(PrimeNetClient client, IPEndPoint endPoint=null)
        {
            // create a connection thread that tries to connect every 1.5 seconds untill the server becomes available
            if (null == endPoint)
            {
                Task t = new Task(() => ConnectToServer(client));
                t.Start();
            }
            else
            {
                Task t = new Task(() => ConnectToServer(client, endPoint));
                t.Start();
            }
        }
    }

    public enum EPrimeNetMessage
    {
        ClientReady,
        ClientConnected,
        ClientDisconnected,
        Pause,
        Play,
        Rewind,
        Stop,
        Load,
        ServerReady,
        ServerConnected,
        ServerDisconnected,
        Generic,
        Status
    }

    public class PrimeNetMessage
    {
        public EPrimeNetMessage NetMessage { get; set; }
        public string MessageBody { get; set; }
        public string SenderIP { get; set; }
        public string DestinationIP { get; set; }

        public string Serialize()
        {
            using (var stringwriter = new System.IO.StringWriter())
            {
                var serializer = new XmlSerializer(this.GetType());
                serializer.Serialize(stringwriter, this);
                return stringwriter.ToString();
            }
        }
        public static PrimeNetMessage Deserialize(string message)
        {
            using (var stringReader = new System.IO.StringReader(message))
            {
                var serializer = new XmlSerializer(typeof(PrimeNetMessage));
                return serializer.Deserialize(stringReader) as PrimeNetMessage;
            }
        }
    }

    public class NetworkMessageEvent
    {
        public NetworkMessageEvent(PrimeNetMessage data)
        {
            Debug.Log("Creating prime net message");
            Data = data;
        }
        public PrimeNetMessage Data { get; private set; }
    }

    public class ConnectionInfo
    {
        public const int DefaultPort = 50515;

        public bool IsServer;
        public uint Port;
        public uint Protocol;
        public IPAddress HosHostAddress;

        public ConnectionInfo() : this(false, DefaultPort, "127.0.0.1", 0)
        {

        }

        public ConnectionInfo(bool isServer, uint port, string host, uint protocol = 0)
        {
            IsServer = isServer;
            Port = port;
            var ips = Dns.GetHostAddresses(host);

            foreach (var ip in ips)
            {
                HosHostAddress = ip;
            }

            Protocol = protocol;
        }
    }
}