using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


public class DistanceChat
{
    static string TagMatcher = @"(?:\[(?:[0-9A-F]{6}|\/?b|\/?i|\/?u|\/?s|\/?c|\-|\/?sub|\/?sup|\/?url|url=[^\]]*)\])?";
    static Dictionary<string, string> ActionMessages = new Dictionary<string, string>()
    {
        { " was terminated by the laser grid", "Death:KillGrid"},
        {" reset", "Death:Reset"},
        {" was wrecked after getting split", "Death:SplitWreck"},
        {" got wrecked", "Death:Impact"},
        {" exploded from overheating", "Death:Overheat"},
        {" got wrecked?", "Death:Unity"},
        {" was kicked due to not having this level", "WorkshopSelfKick"},
        // {" grabbed the x\\d+ multiplier!", "Stunt:Multiplier"}, // checked w/ regex
        {" finished", "Sprint:Finished"},
    };


    public double Timestamp;
    public string Message;
    public string[] Lines;
    public string ChatGuid;
    public string SenderGuid = "Unknown";
    public ChatTypeEnum ChatType = ChatTypeEnum.Unknown;
    public string ChatDescription = "Unknown";
    public bool Blocked = false;

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
        Message = chat;
        ChatGuid = Guid.NewGuid().ToString();
        Lines = chat.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
    }

    public DistanceChat(string chat)
    {
        Timestamp = DistanceServerMain.UnixTime;
        Message = chat;
        ChatGuid = Guid.NewGuid().ToString();
        Lines = chat.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
    }

    public static DistanceChat Server(string description, string chat)
    {
        return new DistanceChat(chat)
        {
            SenderGuid = "Server",
            ChatType = ChatTypeEnum.ServerCustom,
            ChatDescription = description,
        };
    }

    public static Tuple<ChatTypeEnum, string> DeduceChatType(string message, string playerName)
    {
        // matchName allows for mods to color and style names
        var matchName = TagMatcher + Regex.Replace(playerName, ".", match => Regex.Escape(match.ToString()) + TagMatcher);
        
        if (Regex.IsMatch(message, @"^\[[0-9A-F]{6}\](" + matchName + @")\[FFFFFF\]: (.*)$"))
        {
            return new Tuple<ChatTypeEnum, string>(ChatTypeEnum.PlayerChatMessage, "Distance");
        }

        var actionMessageMatch = Regex.Match(message, @"^\[c\]\[[0-9A-F]{6}\]" + matchName + @"(.*)\[\-\]\[\/c\]$");
        if (actionMessageMatch.Success)
        {
            var actionMessage = actionMessageMatch.Groups[1].ToString();
            if (Regex.IsMatch(actionMessage, " grabbed the x\\d+ multiplier!")) {
                return new Tuple<ChatTypeEnum, string>(ChatTypeEnum.PlayerAction, "Stunt:Multiplier");
            }
            else
            {
                string actionDescription = null;
                ActionMessages.TryGetValue(actionMessage, out actionDescription);
                if (actionDescription != null)
                {
                    return new Tuple<ChatTypeEnum, string>(ChatTypeEnum.PlayerAction, actionDescription);
                }
            }
        }

        return new Tuple<ChatTypeEnum, string>(ChatTypeEnum.Unknown, "Unknown");
    }

    public enum ChatTypeEnum
    {

        Unknown,
        PlayerCustom,
        ServerCustom,
        ServerVanilla,
        PlayerAction,
        PlayerChatMessage,
    }
}