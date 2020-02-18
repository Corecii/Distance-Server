extern alias Distance;

using System;
using UnityEngine;

namespace Events.ClientToAllClients
{
	public class HitTagStuntCollectible : BroadcastAllEvent<Distance::Events.Stunt.HitTagStuntCollectible.Data> { }
}
