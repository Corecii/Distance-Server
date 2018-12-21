extern alias Distance;

using System;

namespace Events.ServerToClient
{
		public class TaggedPlayer : StaticTargetedEvent<Distance::Events.ReverseTag.TaggedPlayer.Data> { }
}
