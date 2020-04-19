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

    public override void ReceiveRPC(Distance::BitStreamReader bitStreamReader, UnityEngine.NetworkMessageInfo info)
    {
        T data = DeserializeRPC(bitStreamReader);
        foreach (var conn in subscriptions)
        {
            conn.Handler(data);
        }
        ReceiveInstanced(instance, data);
    }

    public void ReceiveRPC(T data)
    {
        foreach (var conn in subscriptions)
        {
            conn.Handler(data);
        }
    }

    public void WithNonInstanced(InstancedNetworkEventBase<T> evt)
    {
        Connect((T data) =>
        {
            evt.ReceiveRPC(data);
        });
    }

    List<NetworkEventConnection> subscriptions = new List<NetworkEventConnection>();
    public delegate void GenericNetworkEventHandler(T data);
    public virtual IEventConnection Connect(GenericNetworkEventHandler handler)
    {
        return Connect(0, handler);
    }
    public virtual IEventConnection Connect(int priority, GenericNetworkEventHandler handler)
    {
        var connection = new NetworkEventConnection(this, handler, priority);
        subscriptions.Add(connection);
        subscriptions.Sort((a, b) => a.Priority - b.Priority);
        return connection;
    }

    public class NetworkEventConnection : IEventConnection
    {
        public InstancedNetworkEventBase<T> Event;
        public GenericNetworkEventHandler Handler;
        public int Priority { get; set; }
        public NetworkEventConnection(InstancedNetworkEventBase<T> evt, GenericNetworkEventHandler handler, int priority)
        {
            Event = evt;
            Handler = handler;
            Priority = priority;
        }

        public void Disconnect()
        {
            Event.subscriptions.Remove(this);
        }
    }

    public void ReceiveInstanced(object instance, Distance::BitStreamReader bitStreamReader)
    {
        T data = DeserializeRPC(bitStreamReader);
        ReceiveRPC(data);
        foreach (var conn in instancedSubscriptions)
        {
            conn.Handler(instance, data);
        }
    }

    public void ReceiveInstanced(object instance, T data)
    {
        foreach (var conn in instancedSubscriptions)
        {
            conn.Handler(instance, data);
        }
    }

    public void WithInstanced(InstancedNetworkEventBase<T> evt)
    {
        Connect((object instance, T data) =>
        {
            evt.ReceiveInstanced(instance, data);
        });
    }



    List<InstancedNetworkEventConnection> instancedSubscriptions = new List<InstancedNetworkEventConnection>();
    public delegate void GenericInstancedNetworkEventHandler(object instance, T data);
    public virtual IEventConnection Connect(GenericInstancedNetworkEventHandler handler)
    {
        return Connect(0, handler);
    }
    public virtual IEventConnection Connect(int priority, GenericInstancedNetworkEventHandler handler)
    {
        var connection = new InstancedNetworkEventConnection(this, handler, priority);
        instancedSubscriptions.Add(connection);
        instancedSubscriptions.Sort((a, b) => a.Priority - b.Priority);
        return connection;
    }

    public class InstancedNetworkEventConnection : IEventConnection
    {
        public InstancedNetworkEventBase<T> Event;
        public GenericInstancedNetworkEventHandler Handler;
        public int Priority { get; set; }
        public InstancedNetworkEventConnection(InstancedNetworkEventBase<T> evt, GenericInstancedNetworkEventHandler handler, int priority)
        {
            Event = evt;
            Handler = handler;
            Priority = priority;
        }

        public void Disconnect()
        {
            Event.instancedSubscriptions.Remove(this);
        }
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