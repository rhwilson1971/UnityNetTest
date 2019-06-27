using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RMSIDCUTILS.Network;

public class ButtonHandlers : MonoBehaviour
{
    public NetworkServer _NetworkManager;

    public InputField _MyMessage;

    public void ToServer_OnClick()
    {
        if (_NetworkManager != null)
        {
            Debug.Log("ToServer");
            _NetworkManager.SendToServer(_MyMessage.text);
        }
    }

    public void ToClient_OnClick()
    {
        Debug.Log("To Client");
        _NetworkManager.SendToClients(_MyMessage.text);
    }
}
