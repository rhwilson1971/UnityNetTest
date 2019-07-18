using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RMSIDCUTILS.Network;
using System;

public class UpdateCanvasElements : MonoBehaviour
{

    public Dropdown _ConnectedClients;
    public Text _DisplayText;
    public PrimeNetService _NetService;
    bool haveMessage = false;

    // Start is called before the first frame update
    void Start()
    {
        // _NetService.MessageAvailable += OnNewMessageAvailable;
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void LateUpdate()
    {
        if (haveMessage)
        {
            var message = _NetService.Dequeue();
            if (message != null)
            {
                _DisplayText.text = message.MessageBody;

                if(message.NetMessage == EPrimeNetMessage.ClientConnected)
                {
                    _DisplayText.text = "Process Message - A client has connected";

                    HandleConnectedClient(message);
                }

                if(message.NetMessage == EPrimeNetMessage.ClientDisconnected)
                {
                    _DisplayText.text = "Process Message - A client has disconnected " + message.MessageBody;

                    HandleDisconnectedClient(message);
                }
            }
            else
            {
                haveMessage = false;
            }
        }
    }

    private void Awake()
    {
        Debug.Log("What happened in awake?");
        _NetService.MessageAvailable += OnNewMessageAvailable;
    }

    public void OnNewMessageAvailable(object sender, EventArgs e)
    {
        Debug.Log("OnNewMessageAvailable Received ");

        haveMessage = true;
    }

    void HandleConnectedClient(PrimeNetMessage message)
    {
        if (_NetService._IsManager)
        {
            _ConnectedClients.options.Add(new Dropdown.OptionData(message.MessageBody));
        }
        else
        {
            _ConnectedClients.options.Add(new Dropdown.OptionData("Connected to remote server at " + _NetService._HostNameOrIP));
        }
    }

    void HandleDisconnectedClient(PrimeNetMessage message)
    {
        Debug.Log("Disconn client");
        Debug.Log(message.SenderIP);
        Debug.Log(message.MessageBody);

        if(_NetService._IsManager)
            _ConnectedClients.options.RemoveAll(item => item.text == message.MessageBody);
        else
            _ConnectedClients.options.Add(new Dropdown.OptionData("Disconnected from remote server at " + _NetService._HostNameOrIP));

    }

}
