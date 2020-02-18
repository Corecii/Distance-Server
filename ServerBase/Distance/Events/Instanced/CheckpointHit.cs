extern alias Distance;

using System;

namespace Events.Instanced
{
        public class CheckpointHit : InstancedTransceivedEvent<Distance::Events.Car.CheckpointHit.Data> { }
}
