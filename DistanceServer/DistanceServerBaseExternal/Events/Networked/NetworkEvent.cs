extern alias Distance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class NetworkEvent
{
    public virtual string GetDebugRPCString(Distance::BitStreamReader bitStreamReader)
    {
        return "\tUnimplemented NetworkEvent";
    }

    public virtual void ReceiveRPC(Distance::BitStreamReader bitStreamReader)
    {
        Log.WriteLine("Received RPC to unimplemented NetworkEvent");
    }

    public virtual void Fire(UnityEngine.NetworkPlayer target, Distance::IBitSerializable data)
    {
        Log.WriteLine("Fired RPC to unimplemented NetworkEvent");
    }

    public virtual void Fire(UnityEngine.RPCMode target, Distance::IBitSerializable data)
    {
        Log.WriteLine("Fired RPC to unimplemented NetworkEvent");
    }
}