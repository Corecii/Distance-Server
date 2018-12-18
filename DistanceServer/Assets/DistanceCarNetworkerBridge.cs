using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceCarNetworkerBridge : MonoBehaviour
{
    public DistanceCarBase Car = null;
    // TODO: Write OnSerializeNetworkView to receive car position and orientation data

    // Use this for initialization
    void Start()
    {
        Log.WriteLine("DistanceCarNetworker Started");
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    [RPC]
    void ReceiveSerializeEvent(byte[] bytes)
    {
        Car.ReceiveSerializeEvent(bytes);
    }

}
