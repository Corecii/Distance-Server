using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class EventCleaner
{
    private List<IEventConnection> subscriptions = new List<IEventConnection>();

    public EventCleaner() {}

    public EventCleaner(IEventConnection conn)
    {
        Add(conn);
    }

    public EventCleaner(IEnumerable<IEventConnection> conns)
    {
        Add(conns);
    }

    public EventCleaner(params IEventConnection[] conns)
    {
        Add(conns);
    }

    public EventCleaner Add(IEventConnection conn)
    {
        subscriptions.Add(conn);
        return this;
    }

    public EventCleaner Add(IEnumerable<IEventConnection> conns)
    {
        subscriptions.AddRange(conns);
        return this;
    }

    public EventCleaner Add(params IEventConnection[] conns)
    {
        subscriptions.AddRange(conns);
        return this;
    }

    public EventCleaner Clean()
    {
        subscriptions.ForEach((conn) => conn.Disconnect());
        subscriptions.Clear();
        return this;
    }
}
