extern alias Distance;

using System;

namespace Events.ServerToClient
{
		public class GameModeFinished : StaticTargetedEvent<Distance::Events.GameMode.Finished.Data> { }
}
