extern alias Distance;

using System;

namespace Events.ServerToClient
{
		public class SyncMode : StaticTargetedEvent<Distance::Events.GameMode.SyncMode.Data> { }
}
