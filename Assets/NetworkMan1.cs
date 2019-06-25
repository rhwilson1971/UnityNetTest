using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System;
using UnityEngine.UI;

public class NetworkMan1: MonoBehaviour
{
    #region Editor Properties
    public bool _IsServer;
    public int _Port;
    public string _IpAddress;
    public Text _Text;
    public string _message;
    #endregion

    private int _port = 30982;
    private string _ipAddress = "127.0.0.1";


    TcpListener _listener;
    TcpClient _client;

    List<NetworkClient> _clientList = new List<NetworkClient>();

    public static NetworkMan1 instance;

    public static string message;
    private string oldMessage;
    // ManualResetEvent connectDone = new ManualResetEvent(false);

    //public bool isServer;
    //[SerializeField] private int listenPort;

    private void Awake()
    {
        instance = this;
        Debug.Log("Is server? " + _IsServer);
        if(_IsServer == true)
        {
            Startup();
        }
        else
        {
            StartupClient();
        }
    }

    private void Update()
    {
        Debug.Log("Not update?");
        if(message != oldMessage)
        {
            Debug.Log(message);
            oldMessage = message;
        }

        Debug.Log("Updateing?");
        _Text.text = _message;
    }

    public void Startup()
    {
        if (_IsServer)
        {
            ListenForConnections();
        }
        else
        {
            Debug.Log("Ima a client");
            _Text.text = "I'm a client";
        }
    }

    public void ListenForConnections()
    {
        Debug.Log("Listen for connections ");
        _message = "Listening for connections";
        IPAddress ipAddress = IPAddress.Parse(_ipAddress);
        _listener = new TcpListener(ipAddress, _port);
        _listener.Start();
        _listener.BeginAcceptTcpClient(OnServerConnect, null); // async fnction
        _Text.text = "Non blocking Listening for connections";
    }

    public void OnServerConnect(IAsyncResult ar)
    {
        _Text.text = "A client is connecting";

        Debug.Log("Client connecting");
        TcpClient client = _listener.EndAcceptTcpClient(ar);
        NetworkClient nc = new NetworkClient(client);
        _clientList.Add(nc);

        Debug.Log("Listen for further connections");
        _listener.BeginAcceptTcpClient(OnServerConnect, null);
    }

    public void OnDisconnect(NetworkClient client)
    {
        Debug.Log("removing network client");
        _Text.text = "Client Diconnected";
        _clientList.Remove(client);
    }

    public void StartupClient()
    {
        _Text.text = "Starting up the network client";
        Debug.Log("Startup client");

        IPAddress ipAddress = IPAddress.Parse(_ipAddress);
        TcpClient client = new TcpClient();
        // IPAddress[] remoteHost = Dns.GetHostAddresses("localhost");
        
        NetworkClient connectedClient = new NetworkClient(client, false);

        _clientList.Add(connectedClient);
        client.BeginConnect(ipAddress, _port, (ar) => connectedClient.EndConnect(ar), null);
    }

    public void Send(string address, int port, string message)
    {
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);

        TcpClient client = new TcpClient(address, port);
        NetworkStream stream = client.GetStream();

        stream.Write(buffer, 0, buffer.Length);
    }

    public void SendToClients(string message)
    {
        _Text.text = "Sending a message back to the clients";
        if ( !_IsServer )
            return;

        _Text.text = "Sending a message back to the clients again # " + _clientList.Count; 

        foreach (var client in _clientList)
        {
            // _Text.text = "Sent";
            client.Send(message);
        }
    }

    public void SendToServer(string message)
    {
        Debug.Log("Sending message to server");
        if ( _IsServer )
            return;

        // should only be one, unless disconnect code is not written properly
        foreach(var client in _clientList)
        {
            Debug.Log("Found client");
            if(client.IsConnected() )
            {
                Debug.Log("sending message to server from client");
                client.Send(message);
            }
        }
    }
}
