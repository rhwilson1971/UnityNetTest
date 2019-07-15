using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System;
using UnityEngine.UI;
using System.Xml.Serialization;

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
                StartupClient();
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
            _listener.BeginAcceptTcpClient(OnServerConnect, null); // async function
        }

        public void OnServerConnect(IAsyncResult ar)
        {
            // StatusMessage("A client is connecting");

            Debug.Log("Client connecting");
            TcpClient client = _listener.EndAcceptTcpClient(ar);
            PrimeNetClient nc = new PrimeNetClient(client);
            _clientList.Add(nc);

            //PrimeNetService.Instance._Text.text = "The client connected: " + nc.ClientID;
            nc.DataReceived += OnDataReceived;

            Debug.Log("Listen for further connections");
            StatusMessage("The server is listening for new connections");

            var message = new PrimeNetMessage()
            {
                MessageBody = nc.ClientID.ToString(),
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
            client.BeginConnect(_conn.HosHostAddress, (int)_conn.Port, (ar) => connectedClient.EndConnect(ar), null);
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
                    StatusMessage("Sending to a connected client");
                    client.Send(message.Serialize());
                }
            }
        }

        public void StatusMessage(string statusText)
        {
            var message = new PrimeNetMessage() { NetMessage = EPrimeNetMessage.Status, MessageBody = statusText };

            PublishNetworkMessage(new NetworkMessageEvent(message));
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