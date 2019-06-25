using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class TextUpdateScript : NetworkBehaviour
{
    private string _mySyncText;

    public Text _MyText;

    private void Awake()
    {
        
    }

    // Start is called before the first frame update
    void Start()
    {
        // _MyText.text = "Screw you bro!";
    }

    private void LateUpdate()
    {
        // _MyText.text = _mySyncText;
        // Debug.Log(_mySyncText);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
