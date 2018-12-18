extern alias Distance;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class RigidbodyStateTransceiver : NetworkStateTransceiverExternalBase
{
    public Distance::RigidbodyStateTransceiver stateTransceiver;

    public RigidbodyStateTransceiver()
    {
        stateTransceiver = new Distance::RigidbodyStateTransceiver();
    }

    public override void Awake()
    {
        // Do RigidbodyStateTransceiver Awake
        PrivateUtilities.setPrivateField(stateTransceiver, "rigidbody_", transceiver.GetComponent<Rigidbody>());
        PrivateUtilities.setPrivateField(stateTransceiver, "setPositionImmediate_", true);
        // Do NetworkStateTransceiverGeneric Awake
        PrivateUtilities.setPrivateField(stateTransceiver, "buffer_", new Distance::SortedCircularBuffer<Distance::RigidbodyStateTransceiver.Snapshot>(32));
        stateTransceiver.Clear();
    }

    public override void OnEnable()
    {
        var coco_ = (IEnumerator)PrivateUtilities.callPrivateMethod(stateTransceiver, "PostFixedUpdateCoroutine");
        PrivateUtilities.setPrivateField(stateTransceiver, "coco_", coco_);
        transceiver.StartCoroutine(coco_);
    }

    public override void OnDisable()
    {
        transceiver.StopCoroutine((IEnumerator)PrivateUtilities.getPrivateField(stateTransceiver, "coco_"));
    }

    public override void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        stateTransceiver.OnSerializeNetworkView(stream, info);
        var buffer = (Distance::SortedCircularBuffer<Distance::RigidbodyStateTransceiver.Snapshot>)PrivateUtilities.getPrivateField(stateTransceiver, "buffer_");
        Log.Debug($"Transceiver\nTime: {(Distance::NetworkTransceiver.Time_)}\nFirst: {buffer.First_.pos}\nLast: {buffer.Last_.pos}");
    }

    public override void Start()
    {
        PrivateUtilities.callPrivateMethod(stateTransceiver, "Start");
    }

    public override void FixedUpdate()
    {
        PrivateUtilities.callPrivateMethod(stateTransceiver, "FixedUpdate");
    }

    public override void Update()
    {
        PrivateUtilities.callPrivateMethod(stateTransceiver, "Update");
    }

    public override void LateUpdate() { }

    public override void OnDestroy() { }

    public void Clear()
    {
        stateTransceiver.Clear();
    }
}