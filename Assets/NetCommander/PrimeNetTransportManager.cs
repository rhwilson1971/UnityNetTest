using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System;
using System.Xml.Serialization;
using System.Threading.Tasks;

namespace RMSIDCUTILS.NetCommander
{
    public interface INetTransportManager
    { 
        void StartSocketClient();
        void StartupServer();

        void OnDataReceived(object sender, DataReceivedEvent e);
        void OnServerSocketConnect(IAsyncResult ar);
        void OnClientDisconnected(PrimeNetTransportClient client);

        void SetConnection(ConnectionInfo connection);

        void Broadcast(PrimeNetMessage message);

        void DirectMessage(Guid id, PrimeNetMessage message);
    }

    public class PrimetNetTransportManager : INetTransportManager
    {
        #region Private Properties
        TcpListener _listener;
        ConnectionInfo _conn;
        List<PrimeNetTransportClient> _clientList = new List<PrimeNetTransportClient>();
        bool _isConnecting = true;
        ManualResetEvent _quitAppEvent = new ManualResetEvent(false);
        #endregion

        #region Constructors
        public PrimetNetTransportManager(ConnectionInfo connection)
        {
            _conn = connection;
        }

        public PrimetNetTransportManager()
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

        #region Unity Overrides
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
        #endregion

        #region INetTransportManager Interface

        public List<PrimeNetTransportClient> ClientList
        {
            get { return _clientList;  }
            private set
            {
                if(_clientList == null)
                {
                    _clientList = new List<PrimeNetTransportClient>();
                }
            }
        }

        public void StartupServer()
        {
            ListenForConnections();
        }

        public void SetConnection(ConnectionInfo connectionInfo)
        {
            _conn = connectionInfo;
        }

        public void ListenForConnections()
        {
            // Security.PrefetchSocketPolicy(_ipAddress, _port);
            Debug.Log("Listen for connections ");

            try
            {
                _listener = new TcpListener(_conn.HosHostAddress, (int)_conn.Port);
                _listener.Start();
                PrimeNetMessage message = new PrimeNetMessage()
                {
                    NetMessage = EPrimeNetMessage.ServerListening,
                    SenderIP = _conn.HosHostAddress.ToString(),
                    DestinationIP = _conn.HosHostAddress.ToString(),
                    MessageBody = "Asynchronously listening for connections"
                };

                PublishNetworkMessage(new NetworkMessageEvent(message));

                _listener.BeginAcceptSocket(OnServerSocketConnect, _listener);
            }
            catch(SocketException ex)
            {
                Debug.Log("There was a socket exception" + ex.Message);
            }
        }

        /// <summary>
        /// Asyc method that is called whenever the listener 
        /// </summary>
        /// <param name="ar"></param>
        public void OnServerSocketConnect(IAsyncResult ar)
        {
            Debug.Log("Client connecting to this server: " + _listener);
            if (_listener == null)
                return;
            Debug.Log("The listener is still active");
            Socket socket = _listener.EndAcceptSocket(ar);
            Debug.Log("what is state of socket? " + socket);

            var addressBytes = _conn.HosHostAddress.GetAddressBytes();

            Debug.Log("Addr1 " + _conn.HosHostAddress.Address);
            Debug.Log("Addr2 " + _conn.HosHostAddress.GetAddressBytes().ToString());

            PrimeNetTransportClient nc = new PrimeNetTransportClient(socket, true, _conn)
            {
                ClientNumber = _clientList.Count + 1,
                
                RemoteEndPoint = new IPEndPoint(
                    PrimeNetUtils.StringIPToLong(_conn.HosHostAddress.ToString()),                     
                    (int)_conn.Port)
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

            _listener.BeginAcceptSocket(OnServerSocketConnect, _listener);
        }

        public void OnServerConnect(IAsyncResult ar)
        {
            Debug.Log("Client connecting");

            TcpClient client = _listener.EndAcceptTcpClient(ar);
            PrimeNetTransportClient nc = new PrimeNetTransportClient(client)
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

            if(netMsg.NetMessage == EPrimeNetMessage.ClientDisconnected || netMsg.NetMessage == EPrimeNetMessage.ServerDisconnected)
            {
                Debug.Log("Disconnected");
                var id = int.Parse(netMsg.MessageBody);
                var client = _clientList.Find(i => i.ClientNumber == id);
                client.DataReceived -= OnDataReceived;

                _clientList.Remove(client);

                if(netMsg.NetMessage == EPrimeNetMessage.ServerDisconnected)
                {
                    StartSocketClient(); // go back into  connecting to server
                }
            }

            PublishNetworkMessage(new NetworkMessageEvent(netMsg));
        }

        public void OnClientDisconnected(PrimeNetTransportClient client)
        {
            Debug.Log("removing network client");
            StatusMessage("Client Diconnected");
            client.DataReceived -= OnDataReceived;
            _clientList.Remove(client);
        }

        public void StartupClient()
        {
            StatusMessage("Is the client check working? " + _conn.IsServer );
            if (_conn.IsServer)
            {
                return;
            }

            StatusMessage("Starting up the network client");
            Debug.Log("Startup client");

            TcpClient client = new TcpClient();
            PrimeNetTransportClient connectedClient = new PrimeNetTransportClient(client, false);

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
            
            IPEndPoint localEndPoint = new IPEndPoint(PrimeNetUtils.StringIPToLong(_conn.HosHostAddress.ToString()), (int)_conn.Port);
            Socket sender = new Socket(_conn.HosHostAddress.AddressFamily,SocketType.Stream, ProtocolType.Tcp);

            PrimeNetTransportClient client = new PrimeNetTransportClient(sender, false, _conn)
            {
                RemoteEndPoint = localEndPoint
            };

            client.DataReceived += OnDataReceived;
            _clientList.Add(client);

            BeginServerConnection(client, localEndPoint);
        }

        void PublishNetworkMessage(NetworkMessageEvent e)
        {
            if (NetworkMessageReceived != null)
            {
                NetworkMessageReceived?.Invoke(this, e);
            }
        }

        public void Shutdown()
        {
            Debug.Log("Shutting down the netserver");

            if (_conn.IsServer)
            {
                Debug.Log("Stopped listener");
                _listener.Stop();
                _listener = null;
                Debug.Log("listener is dead");
            }
            else
            {
                Debug.Log("App quit event set");
                _quitAppEvent.Set();
            }

            foreach (var client in _clientList)
            {
                Debug.Log("closing client");
                client.DataReceived -= OnDataReceived;
                client.Close();
            }

            _clientList.Clear();
            _isConnecting = true;
            _quitAppEvent.Reset();
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

        #endregion

        #region Private Implementation

        public static PrimeNetMessage LoadFromXMLString(string xmlText)
        {
            using (var stringReader = new System.IO.StringReader(xmlText))
            {
                var serializer = new XmlSerializer(typeof(PrimeNetMessage));
                return serializer.Deserialize(stringReader) as PrimeNetMessage;
            }
        }

        public void Broadcast2(PrimeNetMessage message)
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

        public void Broadcast(PrimeNetMessage message)
        {
            Debug.Log("Broadcasting message to socket {" + message.MessageBody);
            StatusMessage("broadcasting message " + message.MessageBody);

            foreach (var client in _clientList)
            {
                try
                {
                    if (client.IsSocketConnected())
                    {
                        message.SenderIP = _conn.HosHostAddress.ToString();
                        StatusMessage("Sending to a connected client");
                        client.SocketSend(message.Serialize());
                    }
                }
                catch(Exception ex)
                {
                    Debug.Log("Error sending message to a client - " + ex.Message);
                }
            }
        }

        public void DirectMessage(Guid id, PrimeNetMessage message)
        {
            Debug.Log("Sending a message to specific client {" + message.MessageBody + "}");
            // StatusMessage("Sending message " + message.MessageBody);

            var client = _clientList.Find(x => x.ClientID == id);

            if(client != null)
            {
                try
                {
                    if (client.IsSocketConnected())
                    {
                        message.SenderIP = _conn.HosHostAddress.ToString();
                        StatusMessage("Sending to a connected client");
                        client.SocketSend(message.Serialize());
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log("Error sending message to a client - " + ex.Message);
                }
            }
        }

        public void StatusMessage(string statusText)
        {
            var message = new PrimeNetMessage() { NetMessage = EPrimeNetMessage.Status, MessageBody = statusText };

            PublishNetworkMessage(new NetworkMessageEvent(message));
        }

        void ConnectToServer(PrimeNetTransportClient client)
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

        void ConnectToServer(PrimeNetTransportClient client, IPEndPoint endPoint)
        {
            Debug.Log("Connecting to the server...");
            _isConnecting = true;
            _quitAppEvent.Reset();

            while (_isConnecting)
            {
                try
                {
                    client.GetSocket().Connect(endPoint);
                    client.SocketRead();
                    _isConnecting = false;

                    client.StartHeartbeatTimer();

                    PrimeNetMessage m = new PrimeNetMessage()
                    {
                        NetMessage = EPrimeNetMessage.ServerConnected,
                        MessageBody = "Server connected",
                    };

                    PublishNetworkMessage(new NetworkMessageEvent(m));
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

        void BeginServerConnection(PrimeNetTransportClient client, IPEndPoint endPoint=null)
        {
            // create a connection thread that tries to connect every 1.5 seconds untill the server becomes available
            Task t = endPoint == null ? new Task(() => ConnectToServer(client)) : new Task(() => ConnectToServer(client, endPoint));
            t.Start();
        }
        #endregion
    }

    public enum EPrimeNetMessage
    {
        ClientReady,
        ClientConnecting,
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
        ServerListening,
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
        public const string DefaultIpAddress = "127.0.0.1";

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
                Debug.Log("Addr = " + ip.ToString());
            }

            Protocol = protocol;
        }

        public static IPAddress GetLastOperationalNetwork()
        {
            IPAddress primaryAddress = null;

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface netInt in adapters)
            {
                if (netInt.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties properties = netInt.GetIPProperties();

                    foreach (IPAddressInformation addrInfo in properties.UnicastAddresses)
                    {
                        // Ignore loop-back addresses & IPv6 internet protocol family
                        
                        if (addrInfo.Address.AddressFamily != AddressFamily.InterNetworkV6 && !IPAddress.IsLoopback(addrInfo.Address))
                        {
                            Debug.Log(string.Format("Network Interface: {0}\tAddress: {1}", netInt.Name, addrInfo.Address));
                            primaryAddress = addrInfo.Address;
                        }
                    }
                }
            }

            return primaryAddress;
        }

        public static void DisplayComputerNetworkAddresses()
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface netInt in adapters)
            {
                if (netInt.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties properties = netInt.GetIPProperties();

                    foreach (IPAddressInformation addrInfo in properties.UnicastAddresses)
                    {
                        // Ignore loop-back addresses & IPv6 internet protocol family
                        // !IPAddress.IsLoopback(uniCast.Address)
                        if (addrInfo.Address.AddressFamily != AddressFamily.InterNetworkV6)
                        {

                            Debug.Log(string.Format("Network Interface: {0}", netInt.Name));
                            Debug.Log(string.Format("\tAddress: {0}", addrInfo.Address));
                        }
                    }
                }
            }
        }
    }

    public static class Utils
    {
        public static string IPv4Address(this IPAddress address)
        {
            string myAddress = "127.0.0.1";

            if(address != null && address.AddressFamily == AddressFamily.InterNetwork)
            {
                var byteAddress = address.GetAddressBytes();
                myAddress = string.Format("{0}.{1}.{2}.{3}", byteAddress[0], byteAddress[1], byteAddress[2], byteAddress[3]);
            }
            return myAddress;
        }

        //public static long IPv4AddressLong(this IPAddress address)
        //{
        //    return (long)(uint)IPAddress.NetworkToHostOrder(
        //                 (int)IPAddress.Parse(address).Address);
        //}
    }
}