extern alias Distance;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace HttpInfoServer
{

    class CarJsonData
    {
        public float[][] CarColors;
        public string CarName;
        public int Points;
        public bool Finished;
        public int FinishData;
        public Distance::FinishType FinishType;
        public bool Spectator;
        public bool Alive;
        public bool WingsOpen;
        public float[] Position;
        public float[] Rotation;
        public float[] Velocity;
        public float[] AngularVelocity;
    }
    class PlayerJsonData
    {
        public string UnityPlayerGuid;
        public DistancePlayer.PlayerState State;
        public bool Stuck;
        public int LevelId;
        public bool ReceivedInfo;
        public int Index;
        public string Name;
        public double JoinedAt;
        public double ValidatedAt;
        public bool Ready;
        public CarJsonData Car;
        public LevelCompatibilityInfo LevelCompatibilityInfo;
        public string LevelCompatibility;
        public bool Valid;
        public string IpAddress;
        public int Port;
    }

    class AutoServerJsonData
    {
        public double IdleTimeout;
        public double LevelTimeout;
        public bool AdvanceWhenStartingPlayersFinish;
        public string WelcomeMessage;
        public double LevelEndTime;
        public string[] StartingPlayerGuids;
    }

    class VotingJsonData
    {
        public double SkipThreshold;
        public bool HasSkipped;
        public double ExtendThreshold;
        public double ExtendTime;
        public Dictionary<string, double> LeftAt;
        public Dictionary<string, DistanceLevel> PlayerVotes;
        public Dictionary<string, int> AgainstVotes;
        public string[] SkipVotes;
        public string[] ExtendVotes;
    }

    class LevelJsonData
    {
        public int Index;
        public string Name;
        public string RelativeLevelPath;
        public string WorkshopFileId;
        public string GameMode;
        public string Difficulty;
    }

    class ChatJsonData
    {
        public string Sender;
        public string Guid;
        public double Timestamp;
        public string Chat;
        public string Type;
        public string Description;
    }

    class RequestInfo
    {
        internal string Body;
        internal string Response;
        internal bool IsPrivateMode;
        internal string SessionId;
        internal string UnityPlayerGUID;
        internal DistancePlayer DistancePlayer;
        internal Entry Plugin;
        internal HttpListenerContext Context;
        internal RequestInfo(HttpListenerContext context, string body, Entry plugin)
        {
            Context = context;
            Body = body;
            Plugin = plugin;
            IsPrivateMode = TestPrivateMode();

            SessionId = GetSessionId();
            if (SessionId == null)
            {
                SessionId = Guid.NewGuid().ToString();
                Context.Response.Cookies.Add(new Cookie("DistanceSession", SessionId));
            }
            Plugin.Expiry[SessionId] = DistanceServerMain.UnixTime + 10 * 60;

            UnityPlayerGUID = GetUnityPlayerGUID();
            if (UnityPlayerGUID != null)
            {
                DistancePlayer = Plugin.Server.GetDistancePlayer(UnityPlayerGUID);
            }
        }

        bool TestPrivateMode()
        {
            if (!Plugin.PublicMode)
            {
                return true;
            }
            if (Plugin.PrivateModeIps.Contains(Context.Request.RemoteEndPoint.Address.ToString()))
            {
                return true;
            }
            var headers = Context.Request.Headers;
            for (int i = 0; i < headers.Count; i++)
            {
                var key = headers.GetKey(i);
                Log.Debug($"Header key: '{key}' Header value: '{headers.Get(i)}'");
                if (key == "Authorization")
                {
                    var value = headers.Get(i);
                    if (value != null && value.Length > 7 && value.Substring(0, 7) == "Bearer " && Plugin.PrivateModeTokens.Contains(value.Substring(7)))
                    {
                        return true;
                    }
                }
                headers.Add(key, Context.Request.Headers[key]);
            }
            return false;
        }

        string GetSessionId()
        {
            var cookies = Context.Request.Cookies;
            for (int i = 0; i < cookies.Count; i++)
            {
                var cookie = cookies[i];
                if (cookie.Name == "DistanceSession")
                {
                    return cookie.Value;
                }
            }
            return null;
        }

        string GetUnityPlayerGUID()
        {
            if (Plugin.Links.ContainsKey(SessionId))
            {
                return Plugin.Links[SessionId];
            }
            return null;
        }

        internal void Respond()
        {
            Log.DebugLine("HTTP INFO RESPOND", 0);
            var location = Context.Request.Url.PathAndQuery.ToLower().Trim();
            Log.DebugLine("HTTP INFO RESPOND", 1);
            if (location.Length >= 9 && location.Substring(0, 9) == "/playlist")
            {
                RespondPlaylist();
            }
            else if (location == "/summary")
            {
                Log.DebugLine("HTTP INFO RESPOND", 2);
                RespondSummary();
                Log.DebugLine("HTTP INFO RESPOND", 3);
            }
            else if (location == "/links")
            {
                RespondLinks();
            }
            else if (location == "/link")
            {
                RespondLink();
            }
            else if (location == "/link-reverse")
            {
                RespondLinkReverse();
            }
            else if (location == "/vote")
            {
                RespondVote();
            }
            else if (location == "/chat")
            {
                RespondChat();
            }
            else if (location == "/serverchat")
            {
                RespondServerChat();
            }
            else
            {
                Context.Response.StatusCode = 404;
                Context.Response.StatusDescription = "Not Found";
                Response = (new JsonFx.Json.JsonWriter()).Write(new
                {
                    ErrorCode = 404,
                });
            }
        }
        internal void RespondServerChat()
        {
            var writer = new JsonFx.Json.JsonWriter();
            if (!IsPrivateMode)
            {
                Context.Response.StatusCode = 401;
                Context.Response.StatusDescription = "Unauthorized";
                Response = writer.Write(new
                {
                    ErrorCode = 401,
                });
                return;
            }
            var reader = new JsonFx.Json.JsonReader();
            var data = (Dictionary<string, object>)reader.Read(Body);
            string message = (string)data["Message"];
            object sender = "server";
            data.TryGetValue("Sender", out sender);

            Plugin.Server.SayChat(new DistanceChat(message)
            {
                SenderGuid = (string)sender,
                ChatType = DistanceChat.ChatTypeEnum.ServerCustom,
                ChatDescription = "HttpServer:ServerChat"
            });

            Response = writer.Write(new
            {
                Success = true,
            });
        }
        internal void RespondChat()
        {
            var writer = new JsonFx.Json.JsonWriter();
            if (!IsPrivateMode || DistancePlayer == null)
            {
                Context.Response.StatusCode = 401;
                Context.Response.StatusDescription = "Unauthorized";
                Response = writer.Write(new
                {
                    ErrorCode = 401,
                });
                return;
            }
            var reader = new JsonFx.Json.JsonReader();
            var data = (Dictionary<string, object>)reader.Read(Body);
            string message = (string)data["Message"];

            var chatColor = "[" + Distance::ColorEx.ColorToHexNGUI(Distance::ColorEx.PlayerRainbowColor(DistancePlayer.Index)) + "]";

            Plugin.Server.SayChat(new DistanceChat(chatColor + DistancePlayer.Name + "[FFFFFF]: " + message + "[-]")
            {
                SenderGuid = DistancePlayer.UnityPlayerGuid,
                ChatType = DistanceChat.ChatTypeEnum.PlayerChatMessage,
                ChatDescription = "HttpServer:PlayerChat"
            });
            
            Response = writer.Write(new
            {
                Success = true,
            });
        }
        internal void RespondVote()
        {
            var writer = new JsonFx.Json.JsonWriter();
            if (!IsPrivateMode || DistancePlayer == null)
            {
                Context.Response.StatusCode = 401;
                Context.Response.StatusDescription = "Unauthorized";
                Response = writer.Write(new
                {
                    ErrorCode = 401,
                });
                return;
            }

            Response = writer.Write(new
            {
                Success = false,
                Reason = "Not implemented",
            });
        }
        internal void RespondLinks()
        {
            var writer = new JsonFx.Json.JsonWriter();
            if (!IsPrivateMode)
            {
                Context.Response.StatusCode = 401;
                Context.Response.StatusDescription = "Unauthorized";
                Response = writer.Write(new
                {
                    ErrorCode = 401,
                });
                return;
            }
            Response = writer.Write(new
            {
                CodesForward = Plugin.CodesForward,
                CodesReverse = Plugin.CodesReverse,
                Links = Plugin.Links,
            });
        }
        internal void RespondLink()
        {
            var writer = new JsonFx.Json.JsonWriter();
            if (!IsPrivateMode)
            {
                Context.Response.StatusCode = 401;
                Context.Response.StatusDescription = "Unauthorized";
                Response = writer.Write(new
                {
                    ErrorCode = 401,
                });
                return;
            }
            var reader = new JsonFx.Json.JsonReader();
            var data = (Dictionary<string, object>)reader.Read(Body);
            string guid = (string)data["Guid"];
            
            Plugin.Links[SessionId] = guid;
            Response = writer.Write(new
            {
                Success = true,
            });

            if (data.ContainsKey("SendIpWarning") && (bool)data["SendIpWarning"])
            {
                var player = Plugin.Manager.Server.GetDistancePlayer(guid);
                if (player != null)
                {
                    Plugin.Server.SayLocalChat(player.UnityPlayer, DistanceChat.Server("HttpServer:Link", "Your game session has been automatically linked with a web session. You can now vote and chat from the website.\nIf this wasn't you, type [00FFFF]/unlink[-]"));
                }
            }
        }
        internal void RespondLinkReverse()
        {
            var writer = new JsonFx.Json.JsonWriter();
            if (!IsPrivateMode)
            {
                Context.Response.StatusCode = 401;
                Context.Response.StatusDescription = "Unauthorized";
                Response = writer.Write(new
                {
                    ErrorCode = 401,
                });
                return;
            }

            foreach(var pair in Plugin.CodesReverse)
            {
                if (pair.Value == SessionId)
                {
                    Response = writer.Write(new
                    {
                        Code = pair.Key,
                    });
                    return;
                }
            }

            var code = Entry.GenerateCode();
            Plugin.CodesReverse[code] = SessionId;

            Response = writer.Write(new
            {
                Code = code,
            });
        }
        internal void RespondPlaylist()
        {
            var server = DistanceServerMain.Instance.Server;
            var writer = new JsonFx.Json.JsonWriter();

            var autoServer = DistanceServerMain.Instance.GetPlugin<BasicAutoServer.BasicAutoServer>();
            if (autoServer == null)
            {
                Response = writer.Write(new
                {
                    CurrentLevelIndex = 0,
                    Playlist = new
                    {
                        Total = 0,
                        Start = 0,
                        Count = 0,
                        Levels = new string[] { },
                    },
                });
                return;
            }

            var query = new Dictionary<string, string>();
            foreach (var key in Context.Request.QueryString.AllKeys)
            {
                query.Add(key, Context.Request.QueryString[key]);
            }

            int start = autoServer.currentLevelIndex;
            int count = 10;
            string startStr = "";
            string countStr = "";
            query.TryGetValue("Start", out startStr);
            query.TryGetValue("Count", out countStr);
            int.TryParse(startStr, out start);
            int.TryParse(countStr, out count);

            var levels = new List<LevelJsonData>();
            for (int i = start; i < start + count; i++)
            {
                if (i >= autoServer.Playlist.Count)
                {
                    break;
                }
                var level = autoServer.Playlist[i];
                var jsonLevel = new LevelJsonData();
                jsonLevel.Index = i;
                jsonLevel.Name = level.Name;
                jsonLevel.RelativeLevelPath = level.RelativeLevelPath;
                jsonLevel.WorkshopFileId = level.WorkshopFileId;
                jsonLevel.GameMode = level.GameMode;
                levels.Add(jsonLevel);
            }

            Response = writer.Write(new
            {
                CurrentLevelIndex = autoServer.currentLevelIndex,
                Playlist = new
                {
                    Total = autoServer.Playlist.Count,
                    Start = start,
                    Count = count,
                    Levels = levels,
                },
            });
        }
        internal VotingJsonData RespondSummaryVote()
        {
            VotingJsonData votingJsonData = null;
            Log.DebugLine("HTTP INFO VOTE", 0);
            var voteCommands = DistanceServerMain.Instance.GetPlugin<VoteCommands.VoteCommands>();
            Log.DebugLine("HTTP INFO VOTE", 1);
            if (voteCommands != null)
            {
                votingJsonData = new VotingJsonData();
                votingJsonData.SkipThreshold = voteCommands.SkipThreshold;
                votingJsonData.HasSkipped = voteCommands.HasSkipped;
                votingJsonData.ExtendThreshold = voteCommands.ExtendThreshold;
                votingJsonData.ExtendTime = voteCommands.ExtendTime;
                votingJsonData.LeftAt = voteCommands.LeftAt;
                votingJsonData.PlayerVotes = voteCommands.PlayerVotes;
                votingJsonData.SkipVotes = voteCommands.SkipVotes.ToArray();
                votingJsonData.ExtendVotes = voteCommands.ExtendVotes.ToArray();
                var against = new Dictionary<string, int>();
                foreach (var pair in voteCommands.AgainstVotes)
                {
                    against[pair.Key] = pair.Value.Count;
                }
                votingJsonData.AgainstVotes = against;
            }
            return votingJsonData;
        }
        internal void RespondSummary()
        {
            Log.DebugLine("HTTP INFO SUMMARY", 0);
            var server = DistanceServerMain.Instance.Server;
            var writer = new JsonFx.Json.JsonWriter();
            
            var players = new List<PlayerJsonData>(server.DistancePlayers.Count);
            foreach (var player in server.DistancePlayers.Values)
            {
                var jsonPlayer = new PlayerJsonData();
                jsonPlayer.UnityPlayerGuid = player.UnityPlayerGuid;
                jsonPlayer.State = player.State;
                jsonPlayer.Stuck = player.Stuck;
                jsonPlayer.LevelId = player.LevelId;
                jsonPlayer.ReceivedInfo = player.ReceivedInfo;
                jsonPlayer.Index = player.Index;
                jsonPlayer.Name = player.Name;
                jsonPlayer.JoinedAt = player.JoinedAt;
                jsonPlayer.ValidatedAt = player.ValidatedAt;
                jsonPlayer.Ready = player.Ready;
                jsonPlayer.LevelCompatibilityInfo = player.LevelCompatibilityInfo;
                jsonPlayer.LevelCompatibility = player.LevelCompatability.ToString();
                jsonPlayer.Valid = player.Valid;
                if (IsPrivateMode)
                {
                    jsonPlayer.IpAddress = player.UnityPlayer.ipAddress;
                    jsonPlayer.Port = player.UnityPlayer.port;
                }
                else
                {
                    jsonPlayer.IpAddress = "Hidden";
                    jsonPlayer.Port = -1;
                }
                if (player.Car != null)
                {
                    var car = player.Car;
                    var jsonCar = new CarJsonData();
                    jsonCar.CarColors = new float[][] {
                        new float[] {car.CarColors.primary_.r, car.CarColors.primary_.g, car.CarColors.primary_.b, car.CarColors.primary_.a},
                        new float[] {car.CarColors.secondary_.r, car.CarColors.secondary_.g, car.CarColors.secondary_.b, car.CarColors.secondary_.a},
                        new float[] {car.CarColors.glow_.r, car.CarColors.glow_.g, car.CarColors.glow_.b, car.CarColors.glow_.a},
                        new float[] {car.CarColors.sparkle_.r, car.CarColors.sparkle_.g, car.CarColors.sparkle_.b, car.CarColors.sparkle_.a},
                    };
                    jsonCar.CarName = car.CarName;
                    jsonCar.Points = car.Points;
                    jsonCar.Finished = car.Finished;
                    jsonCar.FinishData = car.FinishData;
                    jsonCar.FinishType = car.FinishType;
                    jsonCar.Spectator = car.Spectator;
                    jsonCar.Alive = car.Alive;
                    jsonCar.WingsOpen = car.WingsOpen;
                    jsonCar.Position = new float[] {car.Rigidbody.position.x, car.Rigidbody.position.y, car.Rigidbody.position.z};
                    jsonCar.Rotation = new float[] {car.Rigidbody.rotation.w, car.Rigidbody.rotation.x, car.Rigidbody.rotation.y, car.Rigidbody.rotation.z};
                    jsonCar.Velocity = new float[] {car.Rigidbody.velocity.x, car.Rigidbody.velocity.y, car.Rigidbody.velocity.z};
                    jsonCar.AngularVelocity = new float[] {car.Rigidbody.angularVelocity.x, car.Rigidbody.angularVelocity.y, car.Rigidbody.angularVelocity.z};
                    jsonPlayer.Car = jsonCar;
                }
                players.Add(jsonPlayer);
            }

            AutoServerJsonData autoServerJson = null;
            var autoServer = DistanceServerMain.Instance.GetPlugin<BasicAutoServer.BasicAutoServer>();
            if (autoServer != null)
            {
                autoServerJson = new AutoServerJsonData();
                autoServerJson.IdleTimeout = autoServer.IdleTimeout;
                autoServerJson.LevelTimeout = autoServer.LevelTimeout;
                autoServerJson.WelcomeMessage = autoServer.WelcomeMessage;
                autoServerJson.AdvanceWhenStartingPlayersFinish = autoServer.AdvanceWhenStartingPlayersFinish;
                autoServerJson.LevelEndTime = DistanceServerMain.NetworkTimeToUnixTime(autoServer.LevelEndTime);
                autoServerJson.StartingPlayerGuids = autoServer.StartingPlayerGuids.ToArray();
            }

            Log.DebugLine("HTTP INFO SUMMARY", 1);
            VotingJsonData votingJsonData = null;
            try
            {
                votingJsonData = RespondSummaryVote();
            }
            catch (Exception e) { } // TODO
            Log.DebugLine("HTTP INFO SUMMARY", 2);

            var chatLog = new List<ChatJsonData>();
            foreach (var chat in server.ChatLog)
            {
                var chatJson = new ChatJsonData();
                chatJson.Timestamp = chat.Timestamp;
                chatJson.Chat = chat.Message;
                chatJson.Sender = chat.SenderGuid;
                chatJson.Guid = chat.ChatGuid;
                chatJson.Type = chat.ChatType.ToString();
                chatJson.Description = chat.ChatDescription;
                chatLog.Add(chatJson);
            }
            
            Response = writer.Write(new
            {
                Server = new
                {
                    CurrentLevelId = server.CurrentLevelId,
                    MaxPlayers = server.MaxPlayers,
                    Port = server.Port,
                    ReportToMasterServer = server.ReportToMasterServer,
                    MasterServerGameModeOverride = server.MasterServerGameModeOverride,
                    DistanceVersion = server.DistanceVersion,
                    IsInLobby = server.IsInLobby,
                    HasModeStarted = server.HasModeStarted,
                    ModeStartTime = DistanceServerMain.NetworkTimeToUnixTime(server.ModeStartTime),
                },
                Level = new
                {
                    Name = server.CurrentLevel.Name,
                    RelativeLevelPath = server.CurrentLevel.RelativeLevelPath,
                    WorkshopFileId = server.CurrentLevel.WorkshopFileId,
                    GameMode = server.CurrentLevel.GameMode,
                },
                ChatLog = chatLog,
                Players = players,
                AutoServer = autoServerJson,
                VoteCommands = votingJsonData,
            });
        }
    }

    public class Entry : DistanceServerPlugin
    {
        public override string Author { get; } = "Corecii; Discord: Corecii#3019";
        public override string DisplayName { get; } = "Http Info Server";
        public override int Priority { get; } = 0;
        public override SemanticVersion ServerVersion => new SemanticVersion("0.1.3");

        internal delegate string Command(string input);
        Dictionary<string, Command> commands = new Dictionary<string, Command>();

        int Port = 45683;
        int PortHttps = 45684;
        internal string HelpTextWebsite = null;
        internal bool PublicMode = true;
        internal string[] PrivateModeIps = new string[0];
        internal string[] PrivateModeTokens = new string[0];

        //a forward link involves inputting a 6-digit code on the website to link the web session to the matching guid
        //a reverse link involves inputting a 6-digit code in the game to link the game guid to the matching web session

        internal Dictionary<string, string> CodesForward = new Dictionary<string, string>(); // <6-digit link code, UnityPlayer GUID>
        internal Dictionary<string, string> CodesReverse = new Dictionary<string, string>(); // <6-digit link code, Session token>
        internal Dictionary<string, string> Links = new Dictionary<string, string>(); // <Session token, UnityPlayer GUID>

        internal Dictionary<string, double> Expiry = new Dictionary<string, double>(); // <Session OR GUID, unix time of code or link expiry>

        ThreadWorker<RequestInfo> worker;

        public void ReadSettings()
        {
            var filePath = new System.IO.FileInfo(Manager.ServerDirectory.FullName + "/HttpInfoServer.json");
            if (!filePath.Exists)
            {
                Log.Info("No HttpInfoServer.json, using default settings");
                return;
            }
            try
            {
                var txt = System.IO.File.ReadAllText(filePath.FullName);
                var reader = new JsonFx.Json.JsonReader();
                var dictionary = (Dictionary<string, object>)reader.Read(txt);
                TryGetValue(dictionary, "Port", ref Port);
                TryGetValue(dictionary, "PortHttps", ref PortHttps);
                TryGetValue(dictionary, "HelpTextWebsite", ref HelpTextWebsite);
                TryGetValue(dictionary, "PublicMode", ref PublicMode);
                TryGetValue(dictionary, "PrivateModeIps", ref PrivateModeIps);
                TryGetValue(dictionary, "PrivateModeTokens", ref PrivateModeTokens);
                Log.Info("Loaded settings from HttpInfoServer.json");
            }
            catch (Exception e)
            {
                Log.Error($"Couldn't read HttpInfoServer.json. Is your json malformed?\n{e}");

            }
            Log.Info($"{PrivateModeIps.Length} PrivateModeIps; {PrivateModeTokens.Length} PrivateModeTokens;");
        }

        bool TryGetValue<T>(Dictionary<string, object> dict, string name, ref T value)
        {
            try
            {
                value = (T)dict[name];
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        static System.Random rng = new System.Random();
        internal static string GenerateCode()
        {
            var code = "";
            for (int i = 0; i < 3; i++)
            {
                code += rng.Next(0, 10).ToString();
            }
            for (int i = 0; i < 3; i++)
            {
                code += (char)(65 + rng.Next(0, 26));
            }
            return code;
        }

        internal bool IsExpired(string value, double time = -1)
        {
            if (!Expiry.ContainsKey(value))
            {
                return false;
            }
            if (time == -1)
            {
                time = DistanceServerMain.UnixTime;
            }
            return Expiry[value] <= time;
        }

        public string GetOrGenerateCode(string guid)
        {
            foreach (var pair in CodesForward)
            {
                if (pair.Value == guid)
                {
                    return pair.Key;
                }
            }
            var code = GenerateCode();
            CodesForward[code] = guid;
            return code;
        }

        public override void Start()
        {
            ReadSettings();
            Log.Info($"Starting HTTP Info Server on port {Port}");

            worker = new ThreadWorker<RequestInfo>();
            worker.QueueResponses = false;

            Server.OnUpdateEvent.Connect(() =>
            {
                worker.Respond(info =>
                {
                    info.Request.Respond();
                    return info.Request;
                });
            });

            var listener = new HttpListener();
            Server.OnDestroyEvent.Connect(() =>
            {
                listener.Abort();
            });
            listener.Prefixes.Add($"http://*:{Port}/");
            if (PortHttps >= 0 && PortHttps != Port)
            {
                listener.Prefixes.Add($"https://*:{PortHttps}/");
            }
            listener.Start();
            listener.BeginGetContext(listenerCallback, listener);

            Log.Debug($"Started HTTP(S) Info Server on port {Port}");

            DistanceServerMain.GetEvent<Events.ClientToAllClients.ChatMessage>().Connect((data, info) =>
            {
                var playerMatch = Regex.Match(data.message_, @"^\[[0-9A-F]{6}\](.*?)\[FFFFFF\]: (.*)$");
                if (!playerMatch.Success)
                {
                    return;
                }
                var playerName = Regex.Replace(playerMatch.Groups[1].ToString(), @"\[.*\]", "").ToLower();
                var player = Server.ValidPlayers.Find(distPlayer => distPlayer.Name.ToLower() == Regex.Replace(playerMatch.Groups[1].ToString(), @"\[.*\]", "").ToLower());
                if (player == null)
                {
                    return;
                }
                var message = playerMatch.Groups[2].ToString();

                Match match;
                match = Regex.Match(message, @"^/unlink$");
                if (match.Success)
                {
                    var keysToRemove = new List<string>();
                    foreach (var pair in Links)
                    {
                        if (pair.Value == player.UnityPlayerGuid)
                        {
                            keysToRemove.Add(pair.Key);
                        }
                    }
                    foreach (var key in keysToRemove)
                    {
                        Links.Remove(key);
                    }
                    Server.SayLocalChat(player.UnityPlayer, DistanceChat.Server("HttpServer:Link", $"Your game session has been unlinked from {keysToRemove.Count} web session{(keysToRemove.Count == 1 ? "s" : "")}"));
                    return;
                }

                match = Regex.Match(message, @"^/link (\w{6})$");
                if (match.Success)
                {
                    var code = match.Groups[1].ToString().ToUpper();
                    if (!CodesReverse.ContainsKey(code))
                    {
                        var add = "";
                        if (HelpTextWebsite != null)
                        {
                            add = $"\nVisit {HelpTextWebsite.Replace("$linkcode", GetOrGenerateCode(player.UnityPlayerGuid))} to view and vote online.";
                        }
                        Server.SayLocalChat(player.UnityPlayer, DistanceChat.Server("HttpServer:Link", "Invalid link code!"+add));
                    }
                    Links[CodesReverse[code]] = player.UnityPlayerGuid;
                    CodesReverse.Remove(code);
                    Server.SayLocalChat(player.UnityPlayer, DistanceChat.Server("HttpServer:Link", $"Your game session has been linked to a web session!\nType [00FFFF]/unlink[-] to undo this."));
                    return;
                }

                match = Regex.Match(message, @"^/link");
                if (match.Success)
                {
                    if (HelpTextWebsite != null)
                    {
                        Server.SayLocalChat(player.UnityPlayer, DistanceChat.Server("HttpServer:Link", $"Visit {HelpTextWebsite.Replace("$linkcode", GetOrGenerateCode(player.UnityPlayerGuid))} to view and vote online."));
                    }
                    return;
                }
            });

            Manager.Server.OnPlayerValidatedEvent.Connect(player =>
            {
                GetOrGenerateCode(player.UnityPlayerGuid);
            });

            var autoServer = Manager.GetPlugin<BasicAutoServer.BasicAutoServer>();
            if (autoServer != null)
            {
                Log.Debug("Set LinkCodeGetter in BasicAutoServer");
                autoServer.LinkCodeGetter = player =>
                {
                    return GetOrGenerateCode(player.UnityPlayerGuid);
                };
            }

            Manager.Server.OnPlayerDisconnectedEvent.Connect(player =>
            {
                Expiry[player.UnityPlayerGuid] = DistanceServerMain.UnixTime + 5*60;
                string keyToRemove = null;
                foreach (var pair in CodesForward)
                {
                    if (pair.Value == player.UnityPlayerGuid)
                    {
                        keyToRemove = pair.Key;
                        break;
                    }
                }
                if (keyToRemove != null)
                {
                    CodesForward.Remove(keyToRemove);
                }
            });

            double lastUpdate = 0;
            Manager.Server.OnUpdateEvent.Connect(() =>
            {
                var now = DistanceServerMain.UnixTime;
                if (now - lastUpdate >= 60)
                {
                    lastUpdate = now;
                    var KeysToRemove = new List<string>();
                    foreach (var pair in Links)
                    {
                        if (IsExpired(pair.Key, now) || IsExpired(pair.Value, now))
                        {
                            KeysToRemove.Add(pair.Key);
                        }
                    }
                    foreach (var key in KeysToRemove)
                    {
                        Links.Remove(key);
                    }
                    KeysToRemove.Clear();
                    foreach (var pair in CodesForward)
                    {
                        if (IsExpired(pair.Value, now))
                        {
                            KeysToRemove.Add(pair.Key);
                        }
                    }
                    foreach (var key in KeysToRemove)
                    {
                        CodesForward.Remove(key);
                    }
                    KeysToRemove.Clear();
                    foreach (var pair in CodesReverse)
                    {
                        if (IsExpired(pair.Value, now))
                        {
                            KeysToRemove.Add(pair.Key);
                        }
                    }
                    foreach (var key in KeysToRemove)
                    {
                        CodesReverse.Remove(key);
                    }
                }
            });

            Log.Debug($"Started handling code linking");
        }

        void listenerCallback(IAsyncResult httpResult)
        {
            var listener = (HttpListener)httpResult.AsyncState;
            var context = listener.EndGetContext(httpResult);
            var request = context.Request;
            var response = context.Response;

            listener.BeginGetContext(listenerCallback, listener);

            string requestBody = null;
            string responseString = "";

            response.ContentType = "application/json";

            try
            {

                if (request.HttpMethod.ToUpper() == "POST" && request.HasEntityBody)
                {
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        requestBody = reader.ReadToEnd();
                    }
                }

                var task = worker.AddTask(new RequestInfo(context, requestBody, this));
                task.WaitForResponse();
                if (task.State == ThreadTask<RequestInfo>.ThreadTaskState.Error)
                {
                    responseString += $"Error when invoking:\n{task.Error}";
                }
                else
                {
                    responseString += task.Response.Response;
                }
            }
            catch (Exception e)
            {
                responseString += $"Error when processing:\n{e}";
            }
            sendResponse(response, responseString);
        }

        void sendResponse(HttpListenerResponse response, string responseString)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }

        static string logPath
        {
            get
            {
                var args = System.Environment.GetCommandLineArgs();
                string location = null;
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-logFile")
                    {
                        location = args[i + 1];
                    }
                }
                if (location == null)
                {
                    if (Application.platform == RuntimePlatform.WindowsEditor)
                    {
                        location = Environment.GetEnvironmentVariable("AppData") + @"\..\Local\Unity\Editor\Editor.log";
                    }
                    else if (Application.platform == RuntimePlatform.WindowsPlayer)
                    {
                        location = Environment.GetEnvironmentVariable("AppData") + @"\..\LocalLow" + Application.companyName + @"\" + Application.productName + @"\output_log.txt";
                    }
                    else if (Application.platform == RuntimePlatform.LinuxPlayer)
                    {
                        location = "~/.config/unity3d/" + Application.companyName + "/" + Application.productName + "/Player.log";
                    }
                    else if (Application.platform == RuntimePlatform.OSXPlayer)
                    {
                        location = "~/Library/Logs/Unity/Player.log";
                    }
                }
                return location;
            }
        }
    }
}
