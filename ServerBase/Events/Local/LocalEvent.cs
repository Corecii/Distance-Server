using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class LocalEvent<T>
{
    public void Fire(T data)
    {
        foreach (var conn in subscriptions.ToArray())
        {
            conn.Handler(data);
        }
    }

    List<EventConnection> subscriptions = new List<EventConnection>();
    public delegate void LocalEventHandler(T data);
    public virtual EventConnection Connect(LocalEventHandler handler)
    {
        return Connect(0, handler);
    }
    public virtual EventConnection Connect(int priority, LocalEventHandler handler)
    {
        var connection = new EventConnection(this, handler, priority);
        subscriptions.Add(connection);
        subscriptions.Sort((a, b) => a.Priority - b.Priority);
        return connection;
    }

    public class EventConnection : IEventConnection
    {
        public LocalEvent<T> Event;
        public LocalEventHandler Handler;
        public int Priority { get; set; }
        public EventConnection(LocalEvent<T> evt, LocalEventHandler handler, int priority)
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
