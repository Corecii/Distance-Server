using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class LocalEventEmpty
{
    public void Fire()
    {
        foreach (var conn in subscriptions.ToArray())
        {
            conn.Handler();
        }
    }

    List<EventConnection> subscriptions = new List<EventConnection>();
    public delegate void LocalEventHandler();
    public virtual IEventConnection Connect(LocalEventHandler handler)
    {
        return Connect(0, handler);
    }
    public virtual IEventConnection Connect(int priority, LocalEventHandler handler)
    {
        // Lower priority comes first
        var connection = new EventConnection(this, handler, priority);
        subscriptions.Add(connection);
        subscriptions.Sort((a, b) => a.Priority - b.Priority);
        return connection;
    }

    public class EventConnection : IEventConnection
    {
        public LocalEventEmpty Event;
        public LocalEventHandler Handler;
        public int Priority { get; set; }
        public EventConnection(LocalEventEmpty evt, LocalEventHandler handler, int priority)
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
