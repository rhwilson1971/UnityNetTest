using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RMSIDCUTILS.Network;

public class ButtonHandlers : MonoBehaviour
{
    public PrimeNetServer _NetworkManager;
    public PrimeNetService _NetworkService;

    public InputField _MyMessage;

    public void ToServer_OnClick()
    {
        if (_NetworkManager != null)
        {
            Debug.Log("ToServer");

            var message = new PrimeNetMessage
            {
                MessageBody = _MyMessage.text,
                NetMessage = EPrimeNetMessage.Generic
            };

            // _NetworkManager.Broadcast(message);
            // _NetworkManager.SendToServer(_MyMessage.text);
            _NetworkService.Broadcast(message);
        }
    }

    public void ToClient_OnClick()
    {
        Debug.Log("To Client");

        var message = new PrimeNetMessage
        {
            MessageBody = _MyMessage.text,
            NetMessage = EPrimeNetMessage.Generic
        };

        _NetworkService.Broadcast(message);
        // _NetworkManager.Broadcast(message);
        // _NetworkManager.SendToClients(_MyMessage.text);
    }
}
