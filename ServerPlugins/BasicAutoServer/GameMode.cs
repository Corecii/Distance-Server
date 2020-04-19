extern alias Distance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicAutoServer
{
    public abstract class GameMode
    {
        public BasicAutoServer Plugin;
        protected EventCleaner Connections = new EventCleaner();

        public GameMode(BasicAutoServer plugin)
        {
            Plugin = plugin;
        }

        public abstract void Start();

        public virtual void Destroy()
        {
            Connections.Clean();
        }
    }
}