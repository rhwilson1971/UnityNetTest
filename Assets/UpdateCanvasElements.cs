using RMSIDCUTILS.NetCommander;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateCanvasElements : MonoBehaviour
{
    public Dropdown _ConnectedClients;
    public Text _DisplayText;
    public InputField _MyMessage;
    public PrimeNetService _NetService;
    private bool haveMessage = false;
    private static int counter = 0;
    private List<Button> _connectionList = new List<Button>();
    private Dictionary<int, Button> _guiConnectedClients = new Dictionary<int, Button>();
    private Color ColorConnect = new Color(0.082f, 0.651f, 0.267f, 1.0f);
    private Color ColorUnkown = new Color(1.0f, 0.851f, 0f, 1.0f);
    private Color ColorDisconnect = new Color(1.0f, 0.0f, 0f, 1.0f);


    private readonly int ServerBtnPosIndex = 0;

    // Start is called before the first frame update
    private void Start()
    {
        // _NetService.MessageAvailable += OnNewMessageAvailable;
        if (Application.platform == RuntimePlatform.Android)
        {
            _DisplayText.text = "starting?";
        }
        
        InitClients();
    }

    // Update is called once per frame
    private void Update()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            _DisplayText.text = "update?";
        }
        LookForNewMessages();
    }

    private void LateUpdate()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            _DisplayText.text = "late_update?";
        }
    }

    private void Awake()
    {
        Debug.Log("Registering for new messages from the NetworkService");
        _NetService.MessageAvailable += OnNewMessageAvailable;

        ConnectionInfo.DisplayComputerNetworkAddresses();
        var addr = ConnectionInfo.GetLastOperationalNetwork();

        Debug.Log("Gimme da light -> " + addr.ToString());
    }

    public void OnNewMessageAvailable(object sender, EventArgs e)
    {
        Debug.Log("OnNewMessageAvailable Received ");

        haveMessage = true;
    }

    private void HandleClientConnected(PrimeNetMessage message)
    {
        //if (_NetService._IsServer)
        //{
        //    _ConnectedClients.options.Add(new Dropdown.OptionData(message.MessageBody));

        //    // adding a new client
        //    var id = int.Parse(message.MessageBody);
        //    var client =
        //        _NetService.GetClients().Find(a => a.ClientNumber == id);

        //    Debug.Log("Found a client " + client);
        //    Debug.Log("IP " + client.GetRemoteIPAddress());


        //    if (!_guiConnectedClients.ContainsKey(client.ClientNumber))
        //    {
        //        var name = "NetButtonClient" + client.ClientNumber;
        //        var button = _connectionList.Find(b => b.name == name);

        //        if (button != null)
        //        {
        //            _guiConnectedClients.Add(client.ClientNumber, button);
        //            var theText = button.GetComponentInChildren<Text>();
        //            theText.text = string.Format("Client - {0}", client.GetRemoteIPAddress());

        //            button.GetComponent<Image>().color = ColorConnect;
        //        }
        //    }
        //}
        //else
        //{
        //    _ConnectedClients.options.Add(new Dropdown.OptionData("Connected to remote server at " + _NetService._HostNameOrIP));
        //}

        UpdateGui(message);
    }

    private void HandleClientDisconnected(PrimeNetMessage message)
    {
        Debug.Log("Disconn client");
        Debug.Log(message.SenderIP);
        Debug.Log(message.MessageBody);

        //if (_NetService._IsServer)
        //{
        //    _ConnectedClients.options.RemoveAll(item => item.text == message.MessageBody);
        //}
        //else
        //{
        //    _ConnectedClients.options.Add(new Dropdown.OptionData("Disconnected from remote server at " + _NetService._HostNameOrIP));
        //}

        UpdateGui(message);
    }

    private void HandleServerDisconnected(PrimeNetMessage message)
    {
        Debug.Log("Disconn server");
        Debug.Log(message.SenderIP);
        Debug.Log(message.MessageBody);

        _DisplayText.text = "Server disconnected, retrying....";

        UpdateGui(message);
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


        UpdateGui(message);
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

        //var btnServer =
        //    _connectionList.Find(b => b.name.Contains("Server"));

        //Debug.Log("is there btn " + btnServer);

        //var inputText =
        //    btnServer.GetComponentInChildren<Text>();

        //inputText.text = message.SenderIP;

        UpdateGui(message);
    }

    public void StartService()
    {
        // _MyMessage.text = "Starting client first?";
        _DisplayText.text = "Starting client noew?";

        Debug.Log("Starting net services" + _NetService.IsRunning);

        if (_NetService.IsRunning)
        {
            return;
        }

        //if (Application.platform == RuntimePlatform.Android)
        //{
        //    _DisplayText.text = "Starting andr client?";
        //    _MyMessage.text = "Starting android client?";
        //    var address = "192.168.1.6";

        //    if (null != address)
        //    {
        //        _MyMessage.text = "Starting the service";
        //        _NetService.StartService(false, address.ToString(), 40930);
        //    }
        //    else
        //    {
        //        _DisplayText.text = "Didn't Starting andr client?";
        //        _MyMessage.text = "Didnt starting android client?";
        //    }
        //}
        //else
        {
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
    }

    public void StopService()
    {
        if (_NetService.IsRunning)
        {
            _NetService.StopService();
        }

        _DisplayText.text = "Waiting to connect ...";
        // _MyMessage.text = "Waitning for network messages";
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

    public void LookForNewMessages()
    {
        // _DisplayText.text = "Are you working at all?";
        if (Application.platform == RuntimePlatform.Android)
        {
            // _DisplayText.text = "Getting updates at all?";
        }
        else
        {
            // _DisplayText.text = "Getting updates at all? " + counter++;
        }

        if (haveMessage)
        {
            var message = _NetService.Dequeue();

            if (message != null)
            {
                _DisplayText.text = message.MessageBody;

                if (message.NetMessage == EPrimeNetMessage.ClientConnected)
                {
                    _DisplayText.text = "Process Message - A client has connected";

                    HandleClientConnected(message);
                }

                if (message.NetMessage == EPrimeNetMessage.ClientDisconnected)
                {
                    _DisplayText.text = "Process Message - A client has disconnected " + message.MessageBody;

                    HandleClientDisconnected(message);
                }

                if (message.NetMessage == EPrimeNetMessage.ServerConnected)
                {
                    _DisplayText.text = "This client has connected to the serverConnected to server";
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

    private void OnDestroy()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            _DisplayText.text = "destroyed?";
        }
    }

    private void UpdateGui(PrimeNetMessage message)
    {
        Color currentColor = Color.red;

        var status = false;

        switch (message.NetMessage)
        {
            case EPrimeNetMessage.ClientConnected:
                currentColor = Color.green;
                status = true;
                break;

            case EPrimeNetMessage.ClientDisconnected:
                currentColor = Color.green;
                status = true;
                break;

            case EPrimeNetMessage.ServerConnected:
                currentColor = Color.green;
                _guiConnectedClients[ServerBtnPosIndex].GetComponent<Image>().color = currentColor;
                status = true;
                break;

            case EPrimeNetMessage.ServerDisconnected:
                currentColor = Color.red;
                _guiConnectedClients[ServerBtnPosIndex].GetComponent<Image>().color = currentColor;
                status = true;
                break;

            case EPrimeNetMessage.ServerListening:
                currentColor = Color.green;
                break;
        }

        if (status)
        {

            Debug.Log(message.NetMessage);

            var id = int.Parse(message.MessageBody); // message contains the client number
            var client = _NetService.GetClients().Find(a => a.ClientNumber == id);

            if (_NetService._IsServer)
            {
                _ConnectedClients.options.Add(new Dropdown.OptionData(message.MessageBody));

                Debug.Log("Found a client " + client);
                Debug.Log("IP " + client.GetRemoteIPAddress());

                if (!_guiConnectedClients.ContainsKey(client.ClientNumber))
                {
                    var name = "NetButtonClient" + client.ClientNumber;
                    var button = _connectionList.Find(b => b.name == name);

                    if (button != null)
                    {
                        _guiConnectedClients.Add(client.ClientNumber, button);
                        var theText = button.GetComponentInChildren<Text>();
                        theText.text = string.Format("Client - {0}", client.GetRemoteIPAddress());

                        button.GetComponent<Image>().color = currentColor;
                    }
                }
            }
            else // if client
            {
                _ConnectedClients.options.Add(new Dropdown.OptionData("Connected to remote server at " + _NetService._HostNameOrIP));

                switch(message.NetMessage)
                {
                    case EPrimeNetMessage.ServerConnected:
                        {
                            var serverButton = _guiConnectedClients[0];

                            serverButton.GetComponent<Image>().color = Color.green;
                            serverButton.GetComponentInChildren<Text>().text = "Server: " + message.SenderIP;
                        
                        }
                        break;

                    case EPrimeNetMessage.ServerDisconnected:
                        {
                            var serverButton = _guiConnectedClients[0];

                            serverButton.GetComponent<Image>().color = Color.red;
                            serverButton.GetComponentInChildren<Text>().text = "Server: " + message.SenderIP;
                        }
                        break;
                }
            }
        }
    }

    private void InitClients()
    {
        var scrollViewContent = GameObject.Find("ConnectionListView/Viewport/Content");
        var buttons = FindObjectsOfType<Button>();

        foreach (var btn in buttons)
        {
            Debug.Log(btn.name);
            if (!btn.name.StartsWith("Net"))
            {
                continue;
            }

            var buttonText = btn.GetComponentInChildren<Text>();
            btn.GetComponent<Image>().color = Color.red;

            if (btn.name.Contains("Server"))
            {
                buttonText.text = "Server";
                _guiConnectedClients.Add(ServerBtnPosIndex, btn);
            }
            else
            {
                buttonText.text = "No Client";
            }
            _connectionList.Add(btn);

            Debug.Log(_connectionList.Count);
        }
    }
}