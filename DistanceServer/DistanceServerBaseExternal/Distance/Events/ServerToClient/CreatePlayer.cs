extern alias Distance;

using System;

namespace Events.ServerToClient
{
		public class CreatePlayer : StaticTargetedEvent<Distance::Events.ServerToClient.CreatePlayer.Data> { }
}
