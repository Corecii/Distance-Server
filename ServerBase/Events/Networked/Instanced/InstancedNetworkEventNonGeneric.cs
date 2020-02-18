using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class InstancedNetworkEventNonGeneric : NetworkEvent
{
    public UnityEngine.NetworkView networkView;
    public int eventIndex;
    public object instance;

    public virtual void NonGenericWith(InstancedNetworkEventNonGeneric evt)
    {
        Log.WriteLine("NonGenericWith called on unimplemented InstancedNetworkEventNonGeneric");
    }
}
