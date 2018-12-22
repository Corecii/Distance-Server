using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkshopSearch.Plugin
{
    public class Entry : DistanceServerPlugin
    {
        public static Entry Instance;

        public override string Author { get; } = "Corecii; Discord: Corecii#3019";
        public override string DisplayName { get; } = "WorkshopSearch API Utility";
        public override int Priority { get; } = -9;
        public override SemanticVersion ServerVersion => new SemanticVersion("0.1.3");

        public Entry()
        {
            Instance = this;
            Log.WriteLine("Set Instance");
        }

        public override void Start()
        {

        }
    }
}
