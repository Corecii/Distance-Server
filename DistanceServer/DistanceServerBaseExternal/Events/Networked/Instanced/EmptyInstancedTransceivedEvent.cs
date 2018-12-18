extern alias Distance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class EmptyInstancedTransceivedEvent<T> : InstancedNetworkEventBase<T> where T : class, new()
{
    public override T DeserializeRPC(Distance::BitStreamReader bitStreamReader)
    {
        return new T();
    }

    public override void Fire(UnityEngine.NetworkPlayer target, Distance::IBitSerializable data)
    {
        DistanceServerMain.SendRPC("ReceiveSerializeEvent", eventIndex, target, null, networkView);
    }

    public override void Fire(UnityEngine.RPCMode target, Distance::IBitSerializable data)
    {
        DistanceServerMain.SendRPC("ReceiveSerializeEvent", eventIndex, target, null, networkView);
    }
}
