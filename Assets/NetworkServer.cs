using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System;
using UnityEngine.UI;

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
        void OnClientDisconnected(NetworkClient client);
    }

    public class NetworkServer : MonoBehaviour, INetworkServer
    {
        #region Editor Properties
        [Header("Connection Properties")]
        public bool _IsServer;
        public int _Port;
        public string _IpAddress;
        public Text _Text;
        public string _message;
        #endregion

        #region Private Vars
        private int _port = 30982;
        private string _ipAddress = "127.0.0.1";

        TcpListener _listener;
        #endregion

        List<NetworkClient> _clientList = new List<NetworkClient>();

        public static NetworkServer instance;

        private void Awake()
        {
            instance = this;
            Debug.Log("Is server? " + _IsServer);

            if (_IsServer == true)
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
            if (_IsServer)
            {
                ListenForConnections();
            }
            else // 
            {
                Debug.Log("Ima a client");
                _Text.text = "I'm a client";
            }
        }

        public void ListenForConnections()
        {
            // Security.PrefetchSocketPolicy(_ipAddress, _port);
            Debug.Log("Listen for connections ");
            _message = "Listening for connections";
            IPAddress ipAddress = IPAddress.Parse(_ipAddress);
            _listener = new TcpListener(ipAddress, _port);
            _listener.Start();

            _Text.text = "Asynchronously listening for connections";
            _listener.BeginAcceptTcpClient(OnServerConnect, null); // async function
        }

        public void OnServerConnect(IAsyncResult ar)
        {
            _Text.text = "A client is connecting";

            Debug.Log("Client connecting");
            TcpClient client = _listener.EndAcceptTcpClient(ar);
            NetworkClient nc = new NetworkClient(client);
            _clientList.Add(nc);

            nc.DataReceived += OnDataReceived;

            Debug.Log("Listen for further connections");
            _listener.BeginAcceptTcpClient(OnServerConnect, null);
        }

        public void OnDataReceived(object sender, DataReceivedEvent e)
        {
            Debug.Log("I got a message " + e.Data);

            if(e.Data.StartsWith("id"))
            {
                var guid = Guid.Parse(e.Data.Split(':')[1]);
                _Text.text = guid.ToString();

                var cli = _clientList.Find(c => c.ClientID == guid);

                if (cli != null)
                    _clientList.Remove(cli);
            }

            _Text.text = "I go me a message! " + e.Data;
        }

        public void OnClientDisconnected(NetworkClient client)
        {
            Debug.Log("removing network client");
            _Text.text = "Client Diconnected";
            client.DataReceived -= OnDataReceived;
            _clientList.Remove(client);
        }

        public void StartupClient()
        {
            if (_IsServer)
            {
                return;
            }

            _Text.text = "Starting up the network client";
            Debug.Log("Startup client");

            IPAddress ipAddress = IPAddress.Parse(_ipAddress);
            TcpClient client = new TcpClient();

            NetworkClient connectedClient = new NetworkClient(client, false);
            connectedClient.DataReceived += OnDataReceived;

            _clientList.Add(connectedClient);
            client.BeginConnect(ipAddress, _port, (ar) => connectedClient.EndConnect(ar), null);
        }

        public void SendToClients(string message)
        {
            Debug.Log("Sending a message to the clients from the server ");
            _Text.text = "Sending a message back to the clients";
            if (!_IsServer || string.IsNullOrEmpty(message))
                return;

            _Text.text =
                string.Format("Sending a message back to the clients again - connected client count: {0}", _clientList.Count);

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
    }

    public class ConnectInfo
    {
        public bool IsServer { get; set; }
        public string HostOrIP { get; set; }
        public ushort Port { get; set; }

        public static IPAddress GetIPAddress(string hostOrAddress)
        {
            IPAddress returnedIP = null;
            var ips =
                Dns.GetHostAddresses(hostOrAddress);

            foreach (var ip in ips)
            {
                returnedIP = ip;
                Debug.Log(string.Format("The resolved ip address from {0} is {1} ", hostOrAddress, ip.ToString()));
            }

            return returnedIP;
        }

    }
}