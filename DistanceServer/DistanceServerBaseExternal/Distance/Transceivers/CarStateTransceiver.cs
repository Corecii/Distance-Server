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

    public CarLogicBridge CarLogicBridge;

    public CarStateTransceiver()
    {
        stateTransceiver = new Distance::CarStateTransceiver();
    }

    public override void Awake()
    {
        // Do CarStateTransceiver Awake
        CarLogicBridge = new CarLogicBridge();
        PrivateUtilities.setPrivateField(stateTransceiver, "carLogic_", CarLogicBridge);
        PrivateUtilities.setPrivateField(stateTransceiver, "mode_", new GameModeBridge());
        // Do NetworkStateTransceiverGeneric Awake
        PrivateUtilities.setPrivateField(stateTransceiver, "buffer_", new Distance::SortedCircularBuffer<Distance::CarDirectives>(32));
        stateTransceiver.Clear();
    }

    public override void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        if (stream.isReading)
        {
            // Bypass SortedCircularBuffer logic. We assume that this is the newest CarDirectives data and update the main CarDirectives.
            // TODO: use SortedCircularBuffer. Find what index is the newest, streamed in data and call Serialize on it.
            // (We have to do this because Serialize is not called normally, which means that correct network data never gets turned into a non-zero CarDirectives)
            var newStream = ((Distance::BitStreamUnity)PrivateUtilities.getPrivateField(typeof(Distance::NetworkStateTransceiver), stateTransceiver, "stream_"));
            newStream.Encapsulate(stream);
            double timestamp = double.MinValue;
            newStream.Serialize(ref timestamp);
            CarLogicBridge.CarDirectives_.StreamIn(newStream);
            PrivateUtilities.callPrivateMethod(CarLogicBridge.CarDirectives_, "Serialize", new Distance::BitEncoder(CarLogicBridge.CarDirectives_.Bits_));
        }
        else
        {
            stateTransceiver.OnSerializeNetworkView(stream, info);
        }
    }

    public override void Start()
    {
        PrivateUtilities.callPrivateMethod(stateTransceiver, "Start");
    }

    public override void Update()
    {
        // For CarStateTransceiver, all this does is copy data from the buffer to the main CarDirectives
        // Since we are bypassing this at the moment, it would just copy zeros over already non-zero data.
        //PrivateUtilities.callPrivateMethod(stateTransceiver, "Update");
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