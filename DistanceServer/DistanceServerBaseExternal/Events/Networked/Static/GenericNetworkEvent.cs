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

    public override void ReceiveRPC(Distance::BitStreamReader bitStreamReader)
    {
        T data = DeserializeRPC(bitStreamReader);
        foreach (var handler in subscriptions)
        {
            handler(data);
        }
    }

    List<GenericNetworkEventHandler> subscriptions = new List<GenericNetworkEventHandler>();
    public delegate void GenericNetworkEventHandler(T data);
    public virtual void Connect(GenericNetworkEventHandler handler)
    {
        subscriptions.Add(handler);
    }
}