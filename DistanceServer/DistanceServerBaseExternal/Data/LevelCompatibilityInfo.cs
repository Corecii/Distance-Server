extern alias Distance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public struct LevelCompatibilityInfo
{
    public int LevelCompatibilityId;
    public bool HasLevel;
    public string LevelVersion;

    public LevelCompatibilityInfo(int id, string version, bool has)
    {
        LevelCompatibilityId = id;
        LevelVersion = version;
        HasLevel = has;
    }

    public LevelCompatibilityInfo(Distance::Events.ClientToServer.SubmitLevelCompatabilityInfo.Data data)
    {
        LevelCompatibilityId = data.levelCompatInfoID_;
        LevelVersion = data.levelVersion_;
        HasLevel = data.hasLevel_;
    }
}
