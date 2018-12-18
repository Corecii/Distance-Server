using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public struct DistanceChat
{
    public double Timestamp;
    public string Chat;

    public double NetworkTime
    {
        get
        {
            return DistanceServerMain.UnixTimeToNetworkTime(Timestamp);
        }
    }

    public DistanceChat(double timestamp, string chat)
    {
        Timestamp = timestamp;
        Chat = chat;
    }

    public DistanceChat(string chat)
    {
        Timestamp = DistanceServerMain.UnixTime;
        Chat = chat;
    }
}