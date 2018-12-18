using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetMQConsole
{

    public class Entry : DistanceServerPlugin
    {
        // BROKEN: because unity does not work with NetMQ properly and cannot send responses. The unity portion works fine when not in unity.

        public override string Author { get; } = "Corecii; Discord: Corecii#3019";
        public override string DisplayName { get; } = "NetMQ Console";
        public override int Priority { get; } = -9;

        NetMqReqRouter publisher;

        int reqCount = 0;
        public override void Start()
        {
            publisher = new NetMqReqRouter("tcp://localhost:45682", 10, (task) =>
            {
                Log.Debug($"Processing request {task.Request}");
                // TODO: parse json
                // TODO: check method validity and get method
                // TODO: run method, return results
                return $"{reqCount++}";
            });
            publisher.Start();
            Server.OnDestroyEvent.Connect(() => {
                publisher.Stop();
            });

            Server.OnUpdateEvent.Connect(() =>
            {
                publisher.Respond();
            });
        }
    }
}
