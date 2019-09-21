using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class DistanceChatEventData
{
    public string Message;
    public DistanceChat[] Chats;
    public string SenderGuid;

    public DistanceChatEventData(string message, DistanceChat[] chats, string senderGuid)
    {
        Message = message;
        Chats = chats;
        SenderGuid = senderGuid;
    }
}
