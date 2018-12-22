using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace HttpConsole
{
    class RequestInfo
    {
        internal Entry.Command Method;
        internal string Args;
        internal string Response;
        internal HttpListenerContext Context;
        internal RequestInfo(HttpListenerContext context, Entry.Command method, string args)
        {
            Context = context;
            Method = method;
            Args = args;
        }
    }

    public class Entry : DistanceServerPlugin
    {
        public override string Author { get; } = "Corecii; Discord: Corecii#3019";
        public override string DisplayName { get; } = "Http Console";
        public override int Priority { get; } = 0;
        public override SemanticVersion ServerVersion => new SemanticVersion("0.1.3");

        internal delegate string Command(string input);
        Dictionary<string, Command> commands = new Dictionary<string, Command>();

        int Port = 45681;
        string LogFileLocation = null;
        bool PublicMode = false;

        ThreadWorker<RequestInfo> worker;

        public void ReadSettings()
        {
            var filePath = new System.IO.FileInfo(Manager.ServerDirectory.FullName + "/HttpConsole.json");
            if (!filePath.Exists)
            {
                Log.Info("No HttpConsole.json, using default settings");
                return;
            }
            try
            {
                var txt = System.IO.File.ReadAllText(filePath.FullName);
                var reader = new JsonFx.Json.JsonReader();
                var dictionary = (Dictionary<string, object>)reader.Read(txt);
                TryGetValue(dictionary, "Port", ref Port);
                TryGetValue(dictionary, "LogFileLocation", ref LogFileLocation);
                TryGetValue(dictionary, "PublicMode", ref PublicMode);
                Log.Info("Loaded settings from HttpConsole.json");
            }
            catch (Exception e)
            {
                Log.Error($"Couldn't read HttpConsole.json. Is your json malformed?\n{e}");

            }
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

        public override void Start()
        {
            ReadSettings();
            Log.Info($"Starting HTTP Console server on port {Port}");

            worker = new ThreadWorker<RequestInfo>();
            worker.QueueResponses = false;

            Server.OnUpdateEvent.Connect(() =>
            {
                worker.Respond(info =>
                {
                    info.Request.Response = info.Request.Method(info.Request.Args);
                    return info.Request;
                });
            });

            var listener = new HttpListener();
            Server.OnDestroyEvent.Connect(() =>
            {
                listener.Abort();
            });
            listener.Prefixes.Add($"http://*:{Port}/");
            listener.Start();
            listener.BeginGetContext(listenerCallback, listener);

            Log.Debug($"Started HTTP Console server on port {Port}");

            commands = new Dictionary<string, Command>()
            {
                { "players", input =>
                {
                    input = input.ToLower();
                    var output = "";
                    output += $"{Server.DistancePlayers.Count}/{Server.MaxPlayers} Online ({Server.ValidPlayers.Count} Valid)\n";
                    var players = Server.DistancePlayers.Values.ToList();
                    players.Sort((a, b) =>
                    {
                        return a.Index - b.Index;
                    });
                    foreach (var player in players)
                    {
                        output += $"{player.UnityPlayerGuid} {player.Index} {player.Name} {player.State}\n";
                        if (input == "detailed")
                        {
                            output += $"\t{(player.Ready ? "Ready" : "NoRdy")} : On level {player.LevelId}\n";
                            output += $"\t{player.LevelCompatability} {player.LevelCompatibilityInfo.LevelCompatibilityId} {player.LevelCompatibilityInfo.HasLevel} {player.LevelCompatibilityInfo.LevelVersion}\n";
                            output += $"\t{(player.Car.Finished ? player.Car.FinishType.ToString() : "NotFinished")} {player.Car.FinishData}\n";
                            output += $"\t{player.Car.PlayerViewId} {player.Car.CarViewId1} {player.Car.CarViewId2}\n";
                            output += $"\t{player.Car.CarName} {(player.Car.Alive ? "Alive" : "Dead")}\n";
                            output += $"\t{(player.Car.WingsOpen ? "Flying" : "Driving")}\n";
                        }
                    }
                    return output;
                } },
                { "level", input =>
                {
                    var output = "";
                    output += $"{Server.CurrentLevelId}\n{Server.CurrentLevel.Name}\n{Server.CurrentLevel.RelativeLevelPath}\n{Server.CurrentLevel.LevelVersion}\n{Server.CurrentLevel.WorkshopFileId}\n{Server.CurrentLevel.GameMode}";
                    return output;
                } },
                { "info", input =>
                {
                    var output = "";
                    output += $"{Server.ServerName}\n{Server.DistancePlayers.Count}/{Server.MaxPlayers} ({Server.ValidPlayers.Count} Valid)\n";
                    output += $"{(Server.IsInLobby ? "Lobby" : Server.CurrentLevel.GameMode)}\n";
                    output += $"Port {Server.Port}\n";
                    output += $"{(Server.ReportToMasterServer ? "Reporting to Master Server" : "Not Reporting to Master Server")}\n";
                    output += $"{(Server.StartingLevel ? "Changing levels" : "On level")}";
                    return output;
                } },
                { "chat", input =>
                {
                    var output = "";
                    if (input != "" && !PublicMode)
                    {
                        Server.SayChatMessage(true, input);
                    }
                    foreach (var chat in Server.ChatLog)
                    {
                        output += $"{chat.Chat}\n";
                    }
                    return output;
                } },
                { "log", input =>
                {
                    if (PublicMode)
                    {
                        return "Log not available in public mode";
                    }
                    string logLocation = LogFileLocation == null ? logPath : LogFileLocation;
                    var output = "";
                    if (logLocation == null)
                    {
                        return "Cannot get log file location";
                    }
                    try
                    {
                        FileStream fs = new FileStream(logLocation, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        StreamReader sr = new StreamReader(fs);
                        output = sr.ReadToEnd();
                    }
                    catch (Exception e)
                    {
                        output = $"Error reading log file at {logLocation}:\n{e}";
                    }
                    return output;
                } },
                { "summary", input =>
                {
                    var output = "";
                    output += $"{Server.ServerName}\n{Server.DistancePlayers.Count}/{Server.MaxPlayers} ({Server.ValidPlayers.Count} Valid)\n";
                    output += $"{(Server.IsInLobby ? "Lobby" : Server.CurrentLevel.GameMode)}\n";
                    output += $"Port {Server.Port}\n";
                    output += $"{(Server.ReportToMasterServer ? "Reporting to Master Server" : "Not Reporting to Master Server")}\n";
                    output += $"{(Server.StartingLevel ? "Changing levels" : "On level")}\n\n";

                    output += $"Level:\n{Server.CurrentLevelId}\n{Server.CurrentLevel.Name}\n{Server.CurrentLevel.RelativeLevelPath}\n{Server.CurrentLevel.LevelVersion}\n{Server.CurrentLevel.WorkshopFileId}\n{Server.CurrentLevel.GameMode}\n\n";

                    output += $"Players:\n{Server.DistancePlayers.Count}/{Server.MaxPlayers} Online ({Server.ValidPlayers.Count} Valid)\n";
                    var players = Server.DistancePlayers.Values.ToList();
                    players.Sort((a, b) =>
                    {
                        return a.Index - b.Index;
                    });
                    foreach (var player in players)
                    {
                        output += $"{player.UnityPlayerGuid} {player.Index} {player.Name} {player.State}\n";
                        if (input == "detailed")
                        {
                            output += $"\t{(player.Ready ? "Ready" : "NoRdy")} : On level {player.LevelId}\n";
                            output += $"\t{player.LevelCompatability} {player.LevelCompatibilityInfo.LevelCompatibilityId} {player.LevelCompatibilityInfo.HasLevel} {player.LevelCompatibilityInfo.LevelVersion}\n";
                            output += $"\t{(player.Car.Finished ? player.Car.FinishType.ToString() : "NotFinished")} {player.Car.FinishData}\n";
                            output += $"\t{player.Car.PlayerViewId} {player.Car.CarViewId1} {player.Car.CarViewId2}\n";
                            output += $"\t{player.Car.CarName} {(player.Car.Alive ? "Alive" : "Dead")}\n";
                            output += $"\t{(player.Car.WingsOpen ? "Flying" : "Driving")}\n";
                        }
                    }

                    output += "\nChat:\n";
                    foreach (var chat in Server.ChatLog)
                    {
                        output += $"{chat.Chat}\n";
                    }
                    return output;
                } },
            };
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

            response.ContentType = "text/plain";

            try
            {

                if (request.HttpMethod.ToUpper() == "POST" && request.HasEntityBody)
                {
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        requestBody = reader.ReadToEnd();
                    }
                }

                string commandString = requestBody;
                if (commandString == null)
                {
                    commandString = Uri.UnescapeDataString(request.Url.PathAndQuery).Substring(1);
                }
                var regexResult = Regex.Match(commandString, @"^(\S*)\s*(.*)$");
                var command = regexResult.Groups.Count > 1 ? regexResult.Groups[1].ToString() : "";
                var args = regexResult.Groups.Count > 2 ? regexResult.Groups[2].ToString() : "";
                
                Command method;
                
                if (!commands.TryGetValue(command, out method))
                {
                    responseString += $"Unknown command `{command}`";
                    sendResponse(response, responseString);
                }
                else
                {
                    var task = worker.AddTask(new RequestInfo(context, method, args));
                    task.WaitForResponse();
                    if (task.State == ThreadTask<RequestInfo>.ThreadTaskState.Error)
                    {
                        responseString += $"Error when invoking command:\n{task.Error}";
                    }
                    else
                    {
                        responseString += task.Response.Response;
                    }
                }
            }
            catch (Exception e)
            {
                responseString += $"Error when processing command:\n{e}";
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
