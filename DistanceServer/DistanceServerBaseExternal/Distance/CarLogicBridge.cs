extern alias Distance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class CarLogicBridge : Distance::CarLogic
{
    public CarLogicBridge()
    {
        // This class is just a stub to give Distance's CarStateTransceiver a valid bridge to a CarDirectives
        // CarDirectives is created by default for any CarLogic.
    }
}