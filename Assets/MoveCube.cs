using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCube : MonoBehaviour
{
    public GameObject _theCube;

    // Start is called before the first frame update
    void Start()
    {
        _theCube.transform.Translate(Vector3.up * 1.1f);
    }
    
    // Update is called once per frame
    void Update()
    {
        //_theCube.transform.Translate(Vector3.forward * Time.deltaTime);

        // Move the object upward in world space 1 unit/second.
       // _theCube.transform.Translate(Vector3.up * Time.deltaTime, Space.World);
    }
}
