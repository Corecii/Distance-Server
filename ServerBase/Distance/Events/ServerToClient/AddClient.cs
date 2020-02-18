extern alias Distance;

using System;
using UnityEngine;

namespace Events.ServerToClient
{
		public class AddClient : StaticTargetedEvent<Distance::Events.ServerToClient.AddClient.Data> { }
}
