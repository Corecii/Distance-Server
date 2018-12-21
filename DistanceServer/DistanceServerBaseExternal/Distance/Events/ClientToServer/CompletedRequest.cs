extern alias Distance;

using System;
using UnityEngine;

namespace Events.ClientToServer
{
        public class CompletedRequest : ClientToServerEvent<Distance::Events.ClientToServer.CompletedRequest.Data> { }
}
