using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public abstract class DistanceServerPlugin
{
    public abstract string Author { get; }
    public abstract string DisplayName { get; }
    public virtual int Priority { get; } = 0;
    public abstract SemanticVersion ServerVersion { get; }

    public DistanceServerMain Manager;
    public DistanceServer Server;

    public abstract void Start();
}
