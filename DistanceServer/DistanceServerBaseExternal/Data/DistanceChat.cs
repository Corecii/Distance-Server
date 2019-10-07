using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class DistanceChat
{
    public double Timestamp;
    public string Chat;
    public string SenderGuid;
    public string ChatGuid;

    public double NetworkTime
    {
        get
        {
            return DistanceServerMain.UnixTimeToNetworkTime(Timestamp);
        }
    }

    public DistanceChat(double timestamp, string chat, string senderGuid="server")
    {
        Timestamp = timestamp;
        Chat = chat;
        SenderGuid = senderGuid;
        ChatGuid = Guid.NewGuid().ToString();
    }

    public DistanceChat(string chat, string senderGuid="server")
    {
        Timestamp = DistanceServerMain.UnixTime;
        Chat = chat;
        SenderGuid = senderGuid;
        ChatGuid = Guid.NewGuid().ToString();
    }
}