extern alias Distance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChatFilterPlugin
{
    public class ChatFilterPlugin : DistanceServerPlugin
    {
        public override string Author => "Corecii; Discord: Corecii#3019";
        public override string DisplayName => "Chat Filter Plugin";
        public override int Priority => -4;
        public override SemanticVersion ServerVersion => new SemanticVersion("0.1.3");
        
        public List<string> FilteredWords = new List<string>();
        public List<double> PunishLevels = new List<double>();

        public Dictionary<string, double> TempMuted = new Dictionary<string, double>(); // <playerGuid, expireUnixTime>
        public Dictionary<string, int> PlayerLevels = new Dictionary<string, int>(); // <playerGuid, level>

        public double LastPersistentStateWrite = 0;
        public bool WritingState = false;

        StringBuilder LogBuilder = new StringBuilder();
        double LastLogWrite = 0;
        bool WritingLog = false;

        public void WriteLogIfNecessary()
        {
            if (DistanceServerMain.UnixTime - LastLogWrite > 60)
            {
                WriteLog();
            }
        }

        public void WriteLog()
        {
            if (WritingLog)
            {
                return;
            }
            WritingLog = true;
            var preLastLogWrite = LastLogWrite;
            LastLogWrite = DistanceServerMain.UnixTime;
            try
            {
                var filePath = new System.IO.FileInfo(Manager.ServerDirectory.FullName + "/ChatFilterLog.log");
                System.IO.File.AppendAllText(filePath.FullName, LogBuilder.ToString());
                LogBuilder.Clear();
            }
            catch (Exception e)
            {
                LastLogWrite = preLastLogWrite;
                Log.Error($"Couldn't write ChatFilterLog.log: {e}");
            }
            WritingLog = false;
        }

        public void LogMessage(string str)
        {
            LogBuilder.Append($"[{DateTime.UtcNow.ToString("o")}] {str}\n");
            Log.Info(str);
        }

        public void WritePersistentStateIfNecessary()
        {
            if (DistanceServerMain.UnixTime - LastPersistentStateWrite > 60)
            {
                WritePeristentState();
            }
        }

        public void WritePeristentState()
        {
            if (WritingState)
            {
                return;
            }
            WritingState = true;
            var preLastPersistentStateWrite = LastPersistentStateWrite;
            LastPersistentStateWrite = DistanceServerMain.UnixTime;
            var filePath = new System.IO.FileInfo(Manager.ServerDirectory.FullName + "/ChatFilterState.json");
            try
            {
                var writer = new JsonFx.Json.JsonWriter();
                var text = writer.Write(new { TempMuted, PlayerLevels });
                System.IO.File.WriteAllText(filePath.FullName, text);
            }
            catch (Exception e)
            {
                LastPersistentStateWrite = preLastPersistentStateWrite;
                Log.Error($"Couldn't write ChatFilterState.json: {e}");
            }
            WritingState = false;
        }

        public void ReadPersistentState()
        {
            var filePath = new System.IO.FileInfo(Manager.ServerDirectory.FullName + "/ChatFilterState.json");
            if (!filePath.Exists)
            {
                Log.Info("No ChatFilterState.json, using default settings");
                return;
            }
            try
            {
                var txt = System.IO.File.ReadAllText(filePath.FullName);
                var reader = new JsonFx.Json.JsonReader();
                var dictionary = (Dictionary<string, object>)reader.Read(txt);
                var dictBase = new Dictionary<System.String, System.Object>();
                Log.Debug($"TempMuted: {dictionary["TempMuted"].GetType()}; PlayerLevels: {dictionary["PlayerLevels"].GetType()}");
                TryGetValue(dictionary, "TempMuted", ref dictBase);
                foreach (var pairBase in dictBase)
                {
                    TempMuted[(string)pairBase.Key] = (double)pairBase.Value;
                }
                Log.Info($"Read {dictBase.Count} values from TempMuted");
                var dictBase2 = new Dictionary<System.String, System.Object>();
                TryGetValue(dictionary, "PlayerLevels", ref dictBase2);
                foreach (var pairBase in dictBase2)
                {
                    PlayerLevels[(string)pairBase.Key] = (int)pairBase.Value;
                }
                Log.Info($"Read {dictBase2.Count} values from PlayerLevels");
                Log.Info("Loaded settings from ChatFilterState.json");
            }
            catch (Exception e)
            {
                Log.Error($"Couldn't read ChatFilterState.json. Is your json malformed?\n{e}");
            }
        }

        public void ReadSettings()
        {
            var filePath = new System.IO.FileInfo(Manager.ServerDirectory.FullName + "/ChatFilter.json");
            if (!filePath.Exists)
            {
                Log.Info("No ChatFilter.json, using default settings");
                return;
            }
            try
            {
                var txt = System.IO.File.ReadAllText(filePath.FullName);
                var reader = new JsonFx.Json.JsonReader();
                var dictionary = (Dictionary<string, object>)reader.Read(txt);
                var listBase = new object[0];
                TryGetValue(dictionary, "FilteredWords", ref listBase);
                foreach (object valBase in listBase)
                {
                    Log.Debug($"Reading FilteredWord {valBase}");
                    var val = (string)valBase;
                    FilteredWords.Add(val);
                }
                var listBase2 = new System.Double[0];
                TryGetValue(dictionary, "PunishLevels", ref listBase2);
                foreach (object valBase in listBase2)
                {
                    Log.Debug($"Reading PunishLevel {valBase}");
                    var val = (double)valBase;
                    PunishLevels.Add(val);
                }
                Log.Info("Loaded settings from ChatFilter.json");
            }
            catch (Exception e)
            {
                Log.Error($"Couldn't read ChatFilter.json. Is your json malformed?\n{e}");
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
            Log.Info("ChatFilter Started!");
            ReadSettings();
            ReadPersistentState();

            var votePlugin = Manager.GetPlugin<VoteCommands.VoteCommands>();
            if (votePlugin != null)
            {
                foreach (var pair in TempMuted)
                {
                    votePlugin.Mute(pair.Key, pair.Value);
                }
            }

            Server.OnDestroyEvent.Connect(() =>
            {
                WritePeristentState(); // NOTE/TODO: this can fail if it's already being written
                WriteLog(); // also applies here
            });

            Server.OnChatMessageEvent.Connect(ProcessChatMessage);
        }

        void ProcessChatMessage(DistanceChat data)
        {
            if (data.SenderGuid == "server")
            {
                return;
            }
            var player = Server.GetDistancePlayer(data.SenderGuid);
            if (player == null)
            {
                return;
            }

            var isMuted = false;
            if (TempMuted.ContainsKey(data.SenderGuid))
            {
                var mutedUntil = TempMuted[data.SenderGuid];
                if (mutedUntil > DistanceServerMain.UnixTime)
                {
                    isMuted = true;
                }
                else
                {
                    TempMuted.Remove(data.SenderGuid);
                }
            }

            if (!isMuted)
            {
                var playerMatch = Regex.Match(data.Message, @"^\[[0-9A-F]{6}\](.+)\[FFFFFF\]: (.*)$");
                var message = playerMatch.Groups[2].ToString();
                var safeMessage = Regex.Replace(ChatFilter.CharReplacement.ConvertTextToBase(message), @"[^a-zA-Z0-9 ]", "");

                var isMessageBad = false;
                foreach (var word in FilteredWords)
                {
                    if (Regex.IsMatch(message, word, RegexOptions.IgnoreCase) || Regex.IsMatch(safeMessage, word, RegexOptions.IgnoreCase))
                    {
                        isMessageBad = true;
                        LogMessage($"Player {player.UnityPlayerGuid} ({player.Name}) said a filtered word: {word} FROM {message} ({safeMessage})");
                        break;
                    }
                }
                if (!isMessageBad)
                {
                    return;
                }



                int currentLevel = -1;
                PlayerLevels.TryGetValue(player.UnityPlayerGuid, out currentLevel);
                currentLevel = Math.Min(PunishLevels.Count - 2, currentLevel);
                Log.Debug($"PunishLevels: {PunishLevels.Count}");
                if (PunishLevels.Count == 0)
                {
                    LogMessage($"  Cannot punish because the next punishment level ({currentLevel + 1}) does not exist. (max {PunishLevels.Count - 1})");
                    WriteLogIfNecessary();
                }
                else
                {
                    currentLevel = currentLevel + 1;
                    PlayerLevels[player.UnityPlayerGuid] = currentLevel;
                    var punishment = PunishLevels[currentLevel];
                    TempMuted[player.UnityPlayerGuid] = DistanceServerMain.UnixTime + Math.Abs(punishment);
                    var votePlugin = Manager.GetPlugin<VoteCommands.VoteCommands>();
                    if (votePlugin != null)
                    {
                        votePlugin.Mute(player.UnityPlayerGuid, TempMuted[player.UnityPlayerGuid]);
                    }
                    WritePersistentStateIfNecessary();
                    LogMessage($"  Muting (shadow: {(punishment < 0 ? "yes" : "no")}) for {Math.Abs(punishment)} seconds (level {currentLevel})");
                    WriteLogIfNecessary();
                    if (punishment > 0)
                    {
                        player.SayLocalChat(DistanceChat.Server("ChatFilter:Muted", $"[FF0000]Be nice![-] You are muted for {Math.Abs(punishment)} seconds."));
                    }
                }
            }

            var isShadowMuted = false;
            int level = 0;
            PlayerLevels.TryGetValue(player.UnityPlayerGuid, out level);
            isShadowMuted = level < PunishLevels.Count && PunishLevels[level] < 0;
            if (isShadowMuted)
            {
                Server.DeleteChatMessage(data);
                foreach (var otherPlayer in Server.ValidPlayers)
                {
                    if (otherPlayer != player)
                    {
                        otherPlayer.DeleteChatMessage(data, true);
                    }
                }
            }
            else
            {
                Server.DeleteChatMessage(data, true);
            }
            data.Blocked = true;
        }
    }
}
