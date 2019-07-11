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
        // Debug.Log("Is there a service? " + _NetService);
        if(_NetService != null)
        {
            
        }
    }

    private void Awake()
    {
        Debug.Log("What happened in awak?");
        _NetService.MessageAvailable += OnNewMessageAvailable;
    }

    public void OnNewMessageAvailable(object sender, EventArgs e)
    {
        Debug.Log("hello");
    }

}
