extern alias Distance;

using System;
using UnityEngine;

namespace Events.Instanced
{
        public class CarRespawn : InstancedTransceivedEvent<Distance::Events.Player.CarRespawn.Data> { }
}
