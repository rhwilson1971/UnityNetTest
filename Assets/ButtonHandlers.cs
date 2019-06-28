using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RMSIDCUTILS.Network;

public class ButtonHandlers : MonoBehaviour
{
    public NetworkService _NetworkManager;

    public InputField _MyMessage;

    public void ToServer_OnClick()
    {
        if (_NetworkManager != null)
        {
            Debug.Log("ToServer");

            var message = new PrimeNetMessage
            {
                Data = _MyMessage.text,
                NetMessage = EPrimeNetMessage.Generic
            };

            _NetworkManager.Broadcast(message);


            // _NetworkManager.SendToServer(_MyMessage.text);
        }
    }

    public void ToClient_OnClick()
    {
        Debug.Log("To Client");

        var message = new PrimeNetMessage
        {
            Data = _MyMessage.text,
            NetMessage = EPrimeNetMessage.Generic
        };

        _NetworkManager.Broadcast(message);

        // _NetworkManager.SendToClients(_MyMessage.text);
    }
}
