


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RMSIDCUTILS.Network;

public class ButtonHandlers : MonoBehaviour
{
    public PrimeNetService _NetworkService;
    public InputField _MyMessage;

    public void ToServer_OnClick()
    {
        Debug.Log("ToServer");

        var message = new PrimeNetMessage
        {
            MessageBody = _MyMessage.text,
            NetMessage = EPrimeNetMessage.Generic
        };


        if(_NetworkService == null )
        {
            Debug.Log("Net service is null");
        }
        _NetworkService?.Broadcast(message);
    }

    public void ToClient_OnClick()
    {
        Debug.Log("To Client");

        var message = new PrimeNetMessage
        {
            MessageBody = _MyMessage.text,
            NetMessage = EPrimeNetMessage.Generic
        };

        _NetworkService?.Broadcast(message);
    }
}
