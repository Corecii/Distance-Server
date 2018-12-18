extern alias Distance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class CarStateTransceiver : NetworkStateTransceiverExternalBase
{
    Distance::CarStateTransceiver stateTransceiver;

    public CarStateTransceiver()
    {
        stateTransceiver = new Distance::CarStateTransceiver();
    }

    public override void Awake()
    {
        // Do CarStateTransceiver Awake
        PrivateUtilities.setPrivateField(stateTransceiver, "carLogic_", new CarLogicBridge());
        PrivateUtilities.setPrivateField(stateTransceiver, "mode_", new GameModeBridge());
        // Do NetworkStateTransceiverGeneric Awake
        PrivateUtilities.setPrivateField(stateTransceiver, "buffer_", new Distance::SortedCircularBuffer<Distance::CarDirectives>(32));
        stateTransceiver.Clear();
    }

    public override void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        stateTransceiver.OnSerializeNetworkView(stream, info);
    }

    public override void Start()
    {
        PrivateUtilities.callPrivateMethod(stateTransceiver, "Start");
    }

    public override void Update()
    {
        PrivateUtilities.callPrivateMethod(stateTransceiver, "Update");
    }

    public override void FixedUpdate() { }

    public override void LateUpdate() { }

    public override void OnDestroy() { }

    public override void OnDisable() { }

    public override void OnEnable() { }

    public void Clear()
    {
        stateTransceiver.Clear();
    }
}