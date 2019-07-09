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
    public interface INetworkService
    { 
        void StartupClient();
        void StartupServer();

        void SendToClients(string message);
        void SendToServer(string message);

        void OnDataReceived(object sender, DataReceivedEvent e);
        void OnServerConnect(IAsyncResult ar);
        void OnClientDisconnected(PrimeNetClient client);
    }

    public class PrimeNetServer : MonoBehaviour, INetworkService
    {
        #region Editor Properties
        [Header("Connection Properties")]
        public bool _IsServer;
        public int _Port;
        public string _IpAddress;
        public Text _Text;
        public string _message;
        #endregion

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

        public static PrimeNetServer instance;

        private void Awake()
        {
            instance = this;
            Debug.Log("Is server? " + _IsServer);

            _conn.IsServer = _IsServer;
            _conn.HosHostAddress = IPAddress.Parse(_IpAddress);
            _conn.Port = (uint)_Port;

            Startup();
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
            _message = "Listening for connections";
            //IPAddress ipAddress = IPAddress.Parse(_ipAddress);
            _listener = new TcpListener(_conn.HosHostAddress, (int)_conn.Port);
            _listener.Start();

            // _Text.text = "Asynchronously listening for connections";

            StatusMessage(EPrimeNetMessage.ServerConnected, "Asynchronously listening for connections");
            _listener.BeginAcceptTcpClient(OnServerConnect, null); // async function
        }

        public void OnServerConnect(IAsyncResult ar)
        {
            StatusMessage(EPrimeNetMessage.ClientConnected, "A client is connecting");

            Debug.Log("Client connecting");
            TcpClient client = _listener.EndAcceptTcpClient(ar);
            PrimeNetClient nc = new PrimeNetClient(client);
            _clientList.Add(nc);

            nc.DataReceived += OnDataReceived;

            Debug.Log("Listen for further connections");
            _listener.BeginAcceptTcpClient(OnServerConnect, null);
        }

        public void OnDataReceived(object sender, DataReceivedEvent e)
        {
            Debug.Log("Got a message from a network client ");
            var netMsg = PrimeNetMessage.Deserialize(e.Data);
            Debug.Log("message desrialized");
            _Text.text = netMsg.MessageBody;

            

            HandleNetworkMessage(new NetworkMessageEvent(netMsg));
        }

        public void OnClientDisconnected(PrimeNetClient client)
        {
            Debug.Log("removing network client");
            StatusMessage(EPrimeNetMessage.ClientDisconnected, "Client Diconnected");
            client.DataReceived -= OnDataReceived;
            _clientList.Remove(client);
        }

        public void StartupClient()
        {
            if (_IsServer)
            {
                return;
            }

            StatusMessage(EPrimeNetMessage.ClientReady, "Starting up the network client");
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
            
            StatusMessage(EPrimeNetMessage.Generic, "Sending a message back to the clients");
            if (!_IsServer || string.IsNullOrEmpty(message))
                return;

            var status =
                string.Format("Sending a message back to the clients again - connected client count: {0}", _clientList.Count);
            StatusMessage(EPrimeNetMessage.Generic, status);

            foreach (var client in _clientList)
            {
                client.Send(message);
            }
        }

        public void SendToServer(string message)
        {
            Debug.Log("Sending message to server");
            if (_IsServer)
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

        public void HandleNetworkMessage(NetworkMessageEvent e)
        {
            Debug.Log("Is the handler null? " + NetworkMessageReceived);
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

            foreach(var client in _clientList)
            {
                if (client.IsConnected())
                {
                    client.Send(message.Serialize());
                }
            }
        }

        public void StatusMessage(EPrimeNetMessage status, string statusText)
        {
            //PrimeNetMessage message = new PrimeNetMessage() { Data = statusText, NetMessage = status };
            // HandleNetworkMessage(new NetworkMessageEvent(message));
            _Text.text = statusText;
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
        Generic
    }

    public class PrimeNetMessage
    {
        public EPrimeNetMessage NetMessage { get; set; }
        public string MessageBody { get; set; }

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