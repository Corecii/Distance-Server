extern alias Distance;

using System;

namespace Events.ServerToClient
{
		public class Request : StaticTargetedEvent<Distance::Events.ServerToClient.Request.Data> { }
}
