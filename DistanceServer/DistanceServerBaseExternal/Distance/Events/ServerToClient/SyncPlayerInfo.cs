extern alias Distance;

using System;

namespace Events.ServerToClient
{
		public class SyncPlayerInfo : StaticTargetedEvent<Distance::Events.ModePlayerInfo.SyncPlayerInfo.Data> { }
}
