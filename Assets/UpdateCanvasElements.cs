using RMSIDCUTILS.NetCommander;
using System;
using UnityEngine;
using UnityEngine.UI;

public class UpdateCanvasElements : MonoBehaviour
{
    public Dropdown _ConnectedClients;
    public Text _DisplayText;
    public InputField _MyMessage;
    public PrimeNetService _NetService;
    private bool haveMessage = false;

    // Start is called before the first frame update
    private void Start()
    {
        // _NetService.MessageAvailable += OnNewMessageAvailable;
    }

    // Update is called once per frame
    private void Update()
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

                if (message.NetMessage == EPrimeNetMessage.ClientConnected)
                {
                    _DisplayText.text = "Process Message - A client has connected";

                    HandleConnectedClient(message);
                }

                if (message.NetMessage == EPrimeNetMessage.ClientDisconnected)
                {
                    _DisplayText.text = "Process Message - A client has disconnected " + message.MessageBody;

                    HandleDisconnectedClient(message);
                }

                if (message.NetMessage == EPrimeNetMessage.ServerConnected)
                {
                    _DisplayText.text = "Server disconnected";
                    HandleServerConnected(message);
                }

                if (message.NetMessage == EPrimeNetMessage.ServerListening)
                {
                    _DisplayText.text = "Server Listening";
                    HandleServerListening(message);
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
        Debug.Log("Registering for new messages from the NetworkService");
        _NetService.MessageAvailable += OnNewMessageAvailable;

        ConnectionInfo.GetComputerNetworkAddresses();
    }

    public void OnNewMessageAvailable(object sender, EventArgs e)
    {
        Debug.Log("OnNewMessageAvailable Received ");

        haveMessage = true;
    }

    private void HandleConnectedClient(PrimeNetMessage message)
    {
        if (_NetService._IsServer)
        {
            _ConnectedClients.options.Add(new Dropdown.OptionData(message.MessageBody));
        }
        else
        {
            _ConnectedClients.options.Add(new Dropdown.OptionData("Connected to remote server at " + _NetService._HostNameOrIP));
        }
    }

    private void HandleDisconnectedClient(PrimeNetMessage message)
    {
        Debug.Log("Disconn client");
        Debug.Log(message.SenderIP);
        Debug.Log(message.MessageBody);

        if (_NetService._IsServer)
        {
            _ConnectedClients.options.RemoveAll(item => item.text == message.MessageBody);
        }
        else
        {
            _ConnectedClients.options.Add(new Dropdown.OptionData("Disconnected from remote server at " + _NetService._HostNameOrIP));
        }
    }

    private void HandleServerDisconnected(PrimeNetMessage message)
    {
        Debug.Log("Disconn server");
        Debug.Log(message.SenderIP);
        Debug.Log(message.MessageBody);

        _DisplayText.text = "Server disconnected, retrying....";
    }

    private void HandleServerConnected(PrimeNetMessage message)
    {
        _DisplayText.text = "Server connected, you can send message";

        var button =
            GameObject.Find("ToClient").GetComponent<Button>();

        Debug.Log("Found To client button? " + button);

        if (button != null)
        {
            button.enabled = false;
        }

    }

    private void HandleServerListening(PrimeNetMessage message)
    {
        var button =
            GameObject.Find("ToServer").GetComponent<Button>();

        Debug.Log("Found button? " + button);

        if (button != null)
        {
            button.enabled = false;
        }
    }

    public void StartService()
    {

        Debug.Log("Starting net services" + _NetService.IsRunning);
         
        if (_NetService.IsRunning)
        {
            return;
        }

        Toggle toggle = FindObjectOfType<Toggle>();
        var ipAddressText = GameObject.Find("IPAddressText").GetComponent<Text>();
        var portText = GameObject.Find("PortText").GetComponent<Text>();

        if (!string.IsNullOrEmpty(ipAddressText.text) && !(string.IsNullOrEmpty(portText.text)))
        {
            _NetService.StartService(toggle.isOn, ipAddressText.text, int.Parse(portText.text));
        }
        else
        {
            _NetService.StartService(toggle.isOn, ipAddressText.text, int.Parse(portText.text));
        }

    }

    public void StopService()
    {
        if (_NetService.IsRunning)
        {
            _NetService.StopService();
        }

        _DisplayText.text = "Waiting to connect...";
        _MyMessage.text = "Waitning for network messages";
    }

    public void ToServer_OnClick()
    {
        Debug.Log("ToServer");

        if (string.IsNullOrEmpty(_MyMessage.text))
        {
            return;
        }

        var message = new PrimeNetMessage
        {
            MessageBody = _MyMessage.text,
            NetMessage = EPrimeNetMessage.Generic
        };

        if (_NetService == null)
        {
            Debug.Log("Net service is null");
        }
        _NetService?.Broadcast(message);
    }

    public void ToClient_OnClick()
    {
        Debug.Log("To Client");
        if (string.IsNullOrEmpty(_MyMessage.text))
        {
            return;
        }

        var message = new PrimeNetMessage
        {
            MessageBody = _MyMessage.text,
            NetMessage = EPrimeNetMessage.Generic
        };

        _NetService?.Broadcast(message);
    }
}