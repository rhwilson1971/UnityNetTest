using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RMSIDCUTILS.Network;

public class DisplayNetworkUpdates : MonoBehaviour
{
    [Header("Network Updates")]
    public Text _DisplayText;
    public PrimeNetServer _NetService;

    // Start is called before the first frame update
    void Start()
    {
        _NetService.NetworkMessageReceived += OnNetworkMessageRecieved;
        Debug.Log("Registering for net message");
        _DisplayText.text = "Registering for new net messages";
    }

    private void OnNetworkMessageRecieved(object sender, NetworkMessageEvent e)
    {
        var netMessage = e.Data;
        _DisplayText.text = netMessage.MessageBody;
        Debug.Log("got net message");
    }

    // Update is called once per frame
    void Update()
    {
        // _DisplayText.text 
    }
}
