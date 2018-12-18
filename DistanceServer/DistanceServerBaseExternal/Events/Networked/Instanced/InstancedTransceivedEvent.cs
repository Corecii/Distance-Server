extern alias Distance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class InstancedTransceivedEvent<T> : InstancedNetworkEventBase<T> where T : struct, Distance::IBitSerializable
{
    public override T DeserializeRPC(Distance::BitStreamReader bitStreamReader)
    {
        T data = Activator.CreateInstance<T>();
        data.Serialize(bitStreamReader);
        return data;
    }

    public override void Fire(UnityEngine.NetworkPlayer target, Distance::IBitSerializable data)
    {
        DistanceServerMain.SendRPC("ReceiveSerializeEvent", eventIndex, target, data, networkView);
    }

    public override void Fire(UnityEngine.RPCMode target, Distance::IBitSerializable data)
    {
        DistanceServerMain.SendRPC("ReceiveSerializeEvent", eventIndex, target, data, networkView);
    }
}