extern alias Distance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class InstancedNetworkEventBase<T> : InstancedNetworkEventNonGeneric
{
    public virtual T DeserializeRPC(Distance::BitStreamReader bitStreamReader)
    {
        Log.WriteLine("Received RPC to unimplemented InstancedNetworkEventBase");
        return default(T);
    }

    public override string GetDebugRPCString(Distance::BitStreamReader bitStreamReader)
    {
        T data = DeserializeRPC(bitStreamReader);
        var txt = "";
        txt += $"\t{data.GetType().Name}\n";
        foreach (var prop in data.GetType().GetProperties())
        {
            txt += $"\t\t{prop.Name}  {prop.GetValue(data, null)}\n";
        }
        foreach (var field in data.GetType().GetFields())
        {
            txt += $"\t\t{field.Name}  {field.GetValue(data)}\n";
        }
        return txt;
    }

    public override void ReceiveRPC(Distance::BitStreamReader bitStreamReader)
    {
        T data = DeserializeRPC(bitStreamReader);
        foreach (var handler in subscriptions)
        {
            handler(data);
        }
        ReceiveInstanced(instance, data);
    }

    public void ReceiveRPC(T data)
    {
        foreach (var handler in subscriptions)
        {
            handler(data);
        }
    }

    public void WithNonInstanced(InstancedNetworkEventBase<T> evt)
    {
        Connect((T data) =>
        {
            evt.ReceiveRPC(data);
        });
    }

    List<GenericNetworkEventHandler> subscriptions = new List<GenericNetworkEventHandler>();
    public delegate void GenericNetworkEventHandler(T data);
    public virtual void Connect(GenericNetworkEventHandler handler)
    {
        subscriptions.Add(handler);
    }

    public void ReceiveInstanced(object instance, Distance::BitStreamReader bitStreamReader)
    {
        T data = DeserializeRPC(bitStreamReader);
        ReceiveRPC(data);
        foreach (var handler in instancedSubscriptions)
        {
            handler(instance, data);
        }
    }

    public void ReceiveInstanced(object instance, T data)
    {
        foreach (var handler in instancedSubscriptions)
        {
            handler(instance, data);
        }
    }

    public void WithInstanced(InstancedNetworkEventBase<T> evt)
    {
        Connect((object instance, T data) =>
        {
            evt.ReceiveInstanced(instance, data);
        });
    }

    List<GenericInstancedNetworkEventHandler> instancedSubscriptions = new List<GenericInstancedNetworkEventHandler>();
    public delegate void GenericInstancedNetworkEventHandler(object instance, T data);
    public virtual void Connect(GenericInstancedNetworkEventHandler handler)
    {
        instancedSubscriptions.Add(handler);
    }

    public void With(InstancedNetworkEventBase<T> evt)
    {
        WithNonInstanced(evt);
        WithInstanced(evt);
    }

    public override void NonGenericWith(InstancedNetworkEventNonGeneric evt)
    {
        With((InstancedNetworkEventBase<T>)evt);
    }
}