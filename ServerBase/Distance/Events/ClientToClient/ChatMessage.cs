extern alias Distance;

using System;

namespace Events.ClientToAllClients
{
	public class ChatMessage : BroadcastAllEvent<Distance::Events.ClientToAllClients.ChatMessage.Data> {}
}
