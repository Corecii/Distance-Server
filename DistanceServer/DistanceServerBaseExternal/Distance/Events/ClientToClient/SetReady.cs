extern alias Distance;

using System;
using UnityEngine;

namespace Events.ClientToAllClients
{
	public class SetReady : BroadcastAllEvent<Distance::Events.ClientToAllClients.SetReady.Data> { }
}
