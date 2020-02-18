extern alias Distance;

using System;
using UnityEngine;

namespace Events.ServerToClient
{
		public class StuntBubbleStarted : StaticTargetedEvent<Distance::Events.Stunt.StuntBubbleStarted.Data> { }
}
