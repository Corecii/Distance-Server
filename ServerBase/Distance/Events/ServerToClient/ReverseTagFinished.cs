extern alias Distance;

using System;

namespace Events.ServerToClient
{
		public class ReverseTagFinished : StaticTargetedEvent<Distance::Events.ReverseTag.Finished.Data> { }
}
