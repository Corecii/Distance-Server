extern alias Distance;

using System;

namespace Events.Instanced
{
        public class Jump : EmptyInstancedTransceivedEvent<Jump.Data>
    {
        public class Data
        {
            public Data() { }
        }
    }
}
