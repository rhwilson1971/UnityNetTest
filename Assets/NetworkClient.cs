using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class NetworkClient
{
    readonly TcpClient _client;

    readonly byte[] buffer = new byte[5000];

    NetworkStream Stream
    {
        get { return _client.GetStream(); }
    }
    
    public NetworkClient(TcpClient client, bool isConnected = true)
    {
        Debug.Log("Initializing network client");
        
        _client = client;
        if (isConnected)
        {
            _client.NoDelay = true;
            NetworkMan1.instance._Text.text = "S: Client connected to this server";
            Stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
            NetworkMan1.instance._Text.text = "S: Read ended";
        }
    }

    internal void Close()
    {
        _client.Close();
    }

    void OnRead(IAsyncResult ar)
    {
        NetworkMan1.instance._Text.text = "S: Block on EndRead";
        int length = Stream.EndRead(ar);
        if (length <= 0)
        { // Connection closed
            NetworkMan1.instance._Text.text = "S: Connection Closed";
            NetworkMan1.instance.OnDisconnect(this);
            return;
        }

        NetworkMan1.instance._Text.text = "S: Reading data";
        string newMessage = System.Text.Encoding.UTF8.GetString(buffer, 0, length);
        NetworkMan1.message += newMessage + Environment.NewLine;

        var receivedData = System.Text.Encoding.Default.GetString(buffer);

        Debug.Log("Recieved message " + buffer);
        NetworkMan1.instance._Text.text = "S: Got some data. " + receivedData;
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
        NetworkMan1.instance._Text.text = "This client is starting the network connection";
        
        var client = (TcpClient)ar.AsyncState;


        client.EndConnect(ar);

        NetworkMan1.instance._Text.text = "This client has connected to the network server";
        Debug.Log("Client connect ended");
        Stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
    }

    public void Send(string message)
    {
        NetworkMan1.instance._Text.text = "Sending a message maybe";

        Debug.Log("Send message from client to server ");
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
        _client.GetStream().Write(buffer, 0, buffer.Length);
    }

    public bool IsConnected()
    {
        return _client != null ? _client.Connected : false;
    }

}
