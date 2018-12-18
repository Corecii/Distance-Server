extern alias Distance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class GameModeBridge : Distance::GameMode
{

    public GameModeBridge()
    {
        // This class is just a stub so that Distance's CarStateTransceiver has a valid bridge to isGo_
        // isGo_ needs to be true for Distance's CarStateTransceiver to do its work.
        // make isGo_ true:
        isStarted_ = true;
        PrivateUtilities.setPrivateProperty(typeof(Distance::Timex), null, "PhysicsFrameCount_", 0);
    }

    public override Distance::GameModeID GameModeID_ => throw new NotImplementedException();

    public override string Name_ => throw new NotImplementedException();

    public override Distance::GameModeSortValue SortValue_ => throw new NotImplementedException();

    public override string Description_ => throw new NotImplementedException();

    public override int PlayerCountMax_ => throw new NotImplementedException();

    public override int PlayerCountMin_ => throw new NotImplementedException();

    public override Distance::CheckModeRequirements CheckModeRequirements_ => throw new NotImplementedException();

    public override Distance::ComponentID ID_ => throw new NotImplementedException();

    public override void GetCurrentTrickyTextStandings(StringBuilder text, int index)
    {
        throw new NotImplementedException();
    }

    public override double GetDisplayTime(int playerIndex)
    {
        throw new NotImplementedException();
    }

    protected override Distance::ModeFinishInfoBase CreateFinfoFrom(Distance::ModePlayerInfoBase modeInfo, Distance::FinishType finishType, int finishData)
    {
        throw new NotImplementedException();
    }

    protected override Distance::ModePlayerInfoBase CreateNewModePlayerInfo()
    {
        throw new NotImplementedException();
    }
}
