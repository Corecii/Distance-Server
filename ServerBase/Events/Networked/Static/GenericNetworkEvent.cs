extern alias Distance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class GenericNetworkEvent<T> : NetworkEvent where T : struct, Distance::IBitSerializable
{
    public T DeserializeRPC(Distance::BitStreamReader bitStreamReader)
    {
        T data = Activator.CreateInstance<T>();
        data.Serialize(bitStreamReader);
        return data;
    }

    public override string GetDebugRPCString(Distance::BitStreamReader bitStreamReader)
    {
        T data = DeserializeRPC(bitStreamReader);
        var txt = "";
        txt += $"\t{data.GetType()}\n";
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
            conn.Handler(data, info);
        }
    }

    List<NetworkEventConnection> subscriptions = new List<NetworkEventConnection>();
    public delegate void GenericNetworkEventHandler(T data, UnityEngine.NetworkMessageInfo info);
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
        public GenericNetworkEvent<T> Event;
        public GenericNetworkEventHandler Handler;
        public int Priority { get; set; }
        public NetworkEventConnection(GenericNetworkEvent<T> evt, GenericNetworkEventHandler handler, int priority)
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
}