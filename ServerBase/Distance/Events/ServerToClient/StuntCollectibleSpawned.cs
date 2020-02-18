extern alias Distance;

using System;
using UnityEngine;

namespace Events.ServerToClient
{
		public class StuntCollectibleSpawned : StaticTargetedEvent<Distance::Events.Stunt.StuntCollectibleSpawned.Data> { }
}
