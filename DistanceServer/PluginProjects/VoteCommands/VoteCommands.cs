extern alias Distance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VoteCommands
{
    public class VoteCommands : DistanceServerPlugin
    {
        public override string Author => "Corecii; Discord: Corecii#3019";
        public override string DisplayName => "Voting Commands Plugin";
        public override int Priority => -4;
        public override SemanticVersion ServerVersion => new SemanticVersion("0.1.3");

        public List<string> RequiredTags = new List<string>();
        public HashSet<string> SoftBlocklistLevelIds = new HashSet<string>();
        public HashSet<string> HardBlocklistLevelIds = new HashSet<string>();
        public HashSet<string> SoftBlocklistDifficulties = new HashSet<string>();
        public double SkipThreshold = .7;
        public double ExtendThreshold = .7;
        public double ExtendTime = 3 * 60;
        public double SoftBlocklistThreshold = .7;
        public int RecentMapsSoftBlocklistRoundCount = 5;
        public double VoteNotSafetyTime = 20.0;

        public bool HasSkipped = false;
        public Dictionary<string, int> RecentMaps = new Dictionary<string, int>(); // <relativeLevelPath, expireLevelIndex>

        public Dictionary<string, double> TempMuted = new Dictionary<string, double>(); // <playerGuid, expireUnixTime>

        public Dictionary<string, DistanceLevel> PlayerVotes = new Dictionary<string, DistanceLevel>(); // <playerGuid, level>
        public Dictionary<string, double> PlayerVoteTimes = new Dictionary<string, double>(); // <playerGuid, voteUnixTime>
        public Dictionary<string, HashSet<string>> AgainstVotes = new Dictionary<string, HashSet<string>>(); // <relativeLevelPath, Set<playerGuid>>
        public List<string> SkipVotes = new List<string>(); // <playerGuid>
        public List<string> ExtendVotes = new List<string>(); // <playerGuid>
        public int DelayedExtensions = 0;

        public Dictionary<string, double> LeftAt = new Dictionary<string, double>(); // <playerGuid, expireUnixTime>

        public class FilterLevelRealtimeEventData {
            public bool SoftBlocklist = false;
            public bool HardBlocklist = false;
            public DistanceLevel Level;
            public string Reason = "";
            public FilterLevelRealtimeEventData(DistanceLevel level)
            {
                Level = level;
            }
            public void SoftBlock(string reason)
            {
                if (SoftBlocklist || HardBlocklist)
                {
                    return;
                }
                SoftBlocklist = true;
                Reason = reason;
            }
            public void HardBlock(string reason)
            {
                if (HardBlocklist)
                {
                    return;
                }
                HardBlocklist = true;
                Reason = reason;
            }
        }
        public LocalEvent<FilterLevelRealtimeEventData> OnFilterLevelRealtime = new LocalEvent<FilterLevelRealtimeEventData>();

        public int NeededVotesToSkipLevel => (int)Math.Ceiling(Server.ValidPlayers.Count * SkipThreshold);
        public int NeededVotesToExtendLevel => (int)Math.Ceiling(Server.ValidPlayers.Count * ExtendThreshold);
        public int NeededVotesToOverrideSoftBlocklist => (int)Math.Ceiling(Server.ValidPlayers.Count * SoftBlocklistThreshold);

        public void ReadSettings()
        {
            var filePath = new System.IO.FileInfo(Manager.ServerDirectory.FullName + "/VoteCommands.json");
            if (!filePath.Exists)
            {
                Log.Info("No VoteCommands.json, using default settings");
                return;
            }
            try
            {
                var txt = System.IO.File.ReadAllText(filePath.FullName);
                var reader = new JsonFx.Json.JsonReader();
                var dictionary = (Dictionary<string, object>)reader.Read(txt);
                TryGetValue(dictionary, "SkipThreshold", ref SkipThreshold);
                TryGetValue(dictionary, "ExtendThreshold", ref ExtendThreshold);
                TryGetValue(dictionary, "ExtendTime", ref ExtendTime);
                TryGetValue(dictionary, "VoteNotSafetyTime", ref VoteNotSafetyTime);
                TryGetValue(dictionary, "SoftBlocklistThreshold", ref SoftBlocklistThreshold);
                TryGetValue(dictionary, "RecentMapsSoftBlocklistRoundCount", ref RecentMapsSoftBlocklistRoundCount);
                var listBase = new object[0];
                TryGetValue(dictionary, "RequiredTags", ref listBase);
                foreach (object valBase in listBase)
                {
                    RequiredTags.Add((string)valBase);
                }
                listBase = new object[0];
                TryGetValue(dictionary, "SoftBlocklist", ref listBase);
                foreach (object valBase in listBase)
                {
                    SoftBlocklistLevelIds.Add((string)valBase);
                }
                listBase = new object[0];
                TryGetValue(dictionary, "HardBlocklist", ref listBase);
                foreach (object valBase in listBase)
                {
                    HardBlocklistLevelIds.Add((string)valBase);
                }
                TryGetValue(dictionary, "SoftBlocklistDifficulties", ref listBase);
                foreach (object valBase in listBase)
                {
                    SoftBlocklistDifficulties.Add((string)valBase);
                }
                Log.Info("Loaded settings from VoteCommands.json");
            }
            catch (Exception e)
            {
                Log.Error($"Couldn't read VoteCommands.json. Is your json malformed?\n{e}");

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

        public string GetExtendTimeText()
        {
            int timeout = (int)ExtendTime;
            if (timeout % 60 == 0)
            {
                return $"{timeout / 60} minutes";
            }
            return $"{timeout / 60}:{timeout % 60}";
        }

        public override void Start()
        {
            Log.Info("VoteCommands Started!");
            ReadSettings();

            Server.OnPlayerDisconnectedEvent.Connect(player =>
            {
                if (SkipVotes.Contains(player.UnityPlayerGuid))
                {
                    SkipVotes.Remove(player.UnityPlayerGuid);
                }
                CheckForSkip();
                if (ExtendVotes.Contains(player.UnityPlayerGuid))
                {
                    ExtendVotes.Remove(player.UnityPlayerGuid);
                }
                CheckForExtend();
            });

            Server.OnLevelStartedEvent.Connect(() =>
            {
                ExtendVotes.Clear();
                DelayedExtensions = 0;
                SkipVotes.Clear();
                HasSkipped = false;
                if (RecentMapsSoftBlocklistRoundCount > 0)
                {
                    RecentMaps.Add(Server.CurrentLevel.RelativeLevelPath, Server.CurrentLevelId + RecentMapsSoftBlocklistRoundCount);
                }
            });

            Server.OnModeStartedEvent.Connect(1, () =>
            {
                if (DelayedExtensions != 0)
                {
                    autoServer.ExtendTimeout(ExtendTime * DelayedExtensions);
                }
            });

            Server.OnChatMessageEvent.Connect(ProcessChatMessage);

            autoServer = Manager.GetPlugin<BasicAutoServer.BasicAutoServer>();
            autoServer.OnAdvancingToNextLevel.Connect(OnAdvancingToNextLevel);

            autoServer.TimeoutMessageGetter = time => $"Server has been on this level for {time}. Use [00FFFF]/extend[-] to extend this level.";
            autoServer.StartingPlayersFinishedMessageGetter = () => $"All initial players finished. Use [00FFFF]/extend[-] to extend this level.";

            Server.OnPlayerDisconnectedEvent.Connect(player =>
            {
                LeftAt[player.UnityPlayerGuid] = player.LeftAt;
            });

            OnFilterLevelRealtime.Connect(data =>
            {
                if (SoftBlocklistLevelIds.Contains(data.Level.WorkshopFileId))
                {
                    data.SoftBlock("this level is on the soft blocklist");
                }
                else if (HardBlocklistLevelIds.Contains(data.Level.WorkshopFileId))
                {
                    data.HardBlock("this level is on the blocklist");
                }
                else if (RecentMaps.ContainsKey(data.Level.RelativeLevelPath))
                {
                    var expiryLevelId = RecentMaps[data.Level.RelativeLevelPath];
                    if (Server.CurrentLevelId <= expiryLevelId)
                    {
                        data.SoftBlock("this level was recently played");
                    }
                }
                else if (SoftBlocklistDifficulties.Contains(data.Level.Difficulty.ToString()))
                {
                    data.SoftBlock("this level's difficulty is on the soft blocklist");
                }
            });
        }

        public void Mute(string guid, double until)
        {
            if (TempMuted.ContainsKey(guid))
            {
                TempMuted[guid] = Math.Max(TempMuted[guid], until);
            }
            else
            {
                TempMuted[guid] = until;
            }
        }

        BasicAutoServer.BasicAutoServer autoServer;
        void OnAdvancingToNextLevel()
        {
            var levelLookup = new Dictionary<string, DistanceLevel>();
            var validVotes = new Dictionary<string, int>();
            foreach (var vote in PlayerVotes)
            {
                if (Server.GetDistancePlayer(vote.Key) == null)
                {
                    if (!LeftAt.ContainsKey(vote.Key) || DistanceServerMain.UnixTime - LeftAt[vote.Key] > 5 * 60)
                    {
                        PlayerVotes.Remove(vote.Key);
                        PlayerVoteTimes.Remove(vote.Key);
                        LeftAt.Remove(vote.Key);
                    }
                }
                else if (PlayerVoteTimes[vote.Key] <= DistanceServerMain.UnixTime - VoteNotSafetyTime)
                {
                    int count = 0;
                    validVotes.TryGetValue(vote.Value.RelativeLevelPath, out count);
                    validVotes[vote.Value.RelativeLevelPath] = count + 1;
                    if (!levelLookup.ContainsKey(vote.Value.RelativeLevelPath))
                    {
                        levelLookup[vote.Value.RelativeLevelPath] = vote.Value;
                    }
                }
            }

            foreach (var pair in RecentMaps.ToArray())
            {
                if (Server.CurrentLevelId > pair.Value)
                {
                    RecentMaps.Remove(pair.Key);
                }
            }

            var votesSum = 0;
            foreach (var vote in validVotes.ToArray())
            {
                var data = new FilterLevelRealtimeEventData(levelLookup[vote.Key]);
                OnFilterLevelRealtime.Fire(data);
                if (data.HardBlocklist || (data.SoftBlocklist && vote.Value < NeededVotesToOverrideSoftBlocklist))
                {
                    validVotes.Remove(vote.Key);
                }
                else
                {
                    var value = vote.Value;
                    if (AgainstVotes.ContainsKey(vote.Key))
                    {
                        var count = 0;
                        foreach (var guid in AgainstVotes[vote.Key])
                        {
                            if (Server.GetDistancePlayer(guid) != null)
                            {
                                count++;
                                value--;
                            }
                        }
                    }
                    if (value <= 0)
                    {
                        validVotes.Remove(vote.Key);
                    }
                    else
                    {
                        validVotes[vote.Key] = value;
                        votesSum += value;
                    }
                }
            }
            
            foreach (var pair in LeftAt.ToArray())
            {
                if (DistanceServerMain.UnixTime - pair.Value > 5 * 60)
                {
                    LeftAt.Remove(pair.Key);
                    foreach (var votePair in AgainstVotes.ToArray())
                    {
                        votePair.Value.Remove(pair.Key);
                        if (votePair.Value.Count == 0)
                        {
                            AgainstVotes.Remove(votePair.Key);
                        }
                    }
                }
            }

            if (validVotes.Count == 0)
            {
                return;
            }
            var choiceInt = new Random().Next(votesSum);
            var choiceSum = 0;
            DistanceLevel level = null;
            foreach (var pair in validVotes)
            {
                choiceSum += pair.Value;
                if (choiceInt < choiceSum)
                {
                    level = levelLookup[pair.Key];
                    break;
                }
            }
            if (level == null)
            {
                return;
            }
            autoServer.SetNextLevel(level);
            var voteCount = 0;
            string firstPlayer = null;
            foreach (var vote in PlayerVotes.ToArray())
            {
                if (vote.Value.RelativeLevelPath == level.RelativeLevelPath)
                {
                    voteCount++;
                    PlayerVotes.Remove(vote.Key);
                    PlayerVoteTimes.Remove(vote.Key);
                    if (firstPlayer == null)
                    {
                        firstPlayer = vote.Key;
                    }
                }
            }
            var nextLevelId = Server.CurrentLevelId + 1;
            LocalEventEmpty.EventConnection[] conns = new LocalEventEmpty.EventConnection[2];
            conns[0] = Server.OnModeStartedEvent.Connect(() =>
            {
                var chat = DistanceChat.Server("VoteCommands:ChosenLevel", $"Chosen level is [00FF00]{level.Name}[-], voted for by {Server.GetDistancePlayer(firstPlayer).Name}" + (voteCount > 1 ? $" and {voteCount - 1} others" : ""));
                Server.SayChat(chat);
                foreach (var conn in conns)
                {
                    conn.Disconnect();
                }
            });
            conns[1] = Server.OnLevelStartInitiatedEvent.Connect(() =>
            {
                if (Server.CurrentLevelId != nextLevelId)
                {
                    foreach (var conn in conns)
                    {
                        conn.Disconnect();
                    }
                }
            });
        }

        System.Collections.IEnumerator RestartPlayerAfter(DistancePlayer player, float time)
        {
            player.RestartTime = DistanceServerMain.UnixTime;
            autoServer.SetStartingPlayer(player.UnityPlayerGuid, false);
            player.Car.BroadcastDNF();
            yield return new UnityEngine.WaitForSeconds(time);
            player.Car = null; // if the car stays in the game, the player will get stuck on the loading screen!
            Server.SendPlayerToLevel(player.UnityPlayer);
        }

        void ProcessChatMessage(DistanceChat data)
        {
            Log.DebugLine("VC PC", 0);
            if (data.SenderGuid == "server")
            {
                Log.DebugLine("VC PC", 1);
                return;
            }
            Log.DebugLine("VC PC", 2);
            var player = Server.GetDistancePlayer(data.SenderGuid);
            if (player == null)
            {
                Log.DebugLine("VC PC", 3);
                return;
            }
            Log.DebugLine("VC PC", 4);

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

            var playerMatch = Regex.Match(data.Message, @"^\[[0-9A-F]{6}\](.+)\[FFFFFF\]: (.*)$");
            var message = playerMatch.Groups[2].ToString();

            Match match;
            match = Regex.Match(message, @"^/help$");
            if (match.Success)
            {
                Server.SayLocalChat(player.UnityPlayer, DistanceChat.Server("VoteCommands:Help:Simple","[00FFFF]/vote /skip /extend /restart /not /clear[-]"));
            }

            match = Regex.Match(message, @"^/skip$");
            if (match.Success && SkipThreshold < 100 && SkipThreshold != -1)
            {
                if (isMuted)
                {
                    Server.SayLocalChat(player.UnityPlayer, DistanceChat.Server("VoteCommands:Muted", "You are not allowed to vote while muted"));
                }
                else if (!SkipVotes.Contains(player.UnityPlayerGuid))
                {
                    SkipVotes.Add(player.UnityPlayerGuid);
                    Server.SayChat(DistanceChat.Server("VoteCommands:Feedback:SkipVoteAdded", $"Added your vote to skip the level {SkipVotes.Count}/{NeededVotesToSkipLevel}"));
                    CheckForSkip();
                }
                else
                {
                    SkipVotes.Remove(player.UnityPlayerGuid);
                    Server.SayChat(DistanceChat.Server("VoteCommands:Feedback:SkipVoteRemoved",$"Removed your vote to skip the level {SkipVotes.Count}/{NeededVotesToSkipLevel}"));
                }
                return;
            }

            match = Regex.Match(message, @"^/extend$");
            if (match.Success && SkipThreshold < 100 && SkipThreshold != -1)
            {

                if (isMuted)
                {
                    Server.SayLocalChat(player.UnityPlayer, DistanceChat.Server("VoteCommands:Muted", "You are not allowed to vote while muted"));
                }
                else if (!ExtendVotes.Contains(player.UnityPlayerGuid))
                {
                    ExtendVotes.Add(player.UnityPlayerGuid);
                    Server.SayChat(DistanceChat.Server("VoteCommands:Feedback:ExtendVoteRemoved", $"Added your vote to extend the level {ExtendVotes.Count}/{NeededVotesToExtendLevel}"));
                    CheckForExtend();
                }
                else
                {
                    ExtendVotes.Remove(player.UnityPlayerGuid);
                    Server.SayChat(DistanceChat.Server("VoteCommands:Feedback:ExtendVoteRemoved", $"Removed your vote to extend the level {ExtendVotes.Count}/{NeededVotesToExtendLevel}"));
                }
                return;
            }

            match = Regex.Match(message, @"^/restart$");
            if (match.Success)
            {
                var restartOkay = false;
                if (autoServer.ServerStage == BasicAutoServer.BasicAutoServer.Stage.Started)
                {
                    restartOkay = true;
                } else if (autoServer.ServerStage == BasicAutoServer.BasicAutoServer.Stage.Timeout && autoServer.LevelEndTime - DistanceServerMain.NetworkTime > 30)
                {
                    restartOkay = true;
                }
                if (player.State != DistancePlayer.PlayerState.StartedMode || player.Car == null)
                {
                    restartOkay = false;
                }
                if (Server.IsInLobby)
                {
                    restartOkay = false;
                }
                if (!restartOkay)
                {
                    Server.SayLocalChat(player.UnityPlayer, DistanceChat.Server("VoteCommands:NoRestart", $"You cannot restart right now"));
                    return;
                }
                Server.SayLocalChat(player.UnityPlayer, DistanceChat.Server("VoteCommands:Restart", $"Restarting the level, just for you..."));
                DistanceServerMainStarter.Instance.StartCoroutine(RestartPlayerAfter(player, 2));
                return;
            }

            var isVote = true;
            var isAgainst = false;
            string levelSearchName = null;
            match = Regex.Match(message, @"^/vote (.*)$");
            if (!match.Success)
            {
                match = Regex.Match(message, @"^/not (.*)$");
                isAgainst = true;
            }
            if (!match.Success)
            {
                match = Regex.Match(message, @"^/search (.*)$");
                isVote = false;
            }
            if (match.Success)
            {
                levelSearchName = match.Groups[1].ToString();
            }
            else
            {
                if (Regex.Match(message, @"^/vote$").Success || Regex.Match(message, @"^/search$").Success || Regex.Match(message, @"^/not$").Success)
                {
                    if (PlayerVotes.ContainsKey(player.UnityPlayerGuid))
                    {
                        Server.SayLocalChat(player.UnityPlayer, DistanceChat.Server("VoteCommands:Help:CurrentVote", $"Your current vote is for [00FF00]{PlayerVotes[player.UnityPlayerGuid].Name}[-]"));
                    }
                    Server.SayLocalChat(player.UnityPlayer, DistanceChat.Server("VoteCommands:Help:Detailed", "[00FFFF]/search name[-] to search\n[00FFFF]/clear[-] to clear your vote\n[00FFFF]/vote name[-] to vote for a level\n[00FFFF]/not name[-] to vote against a level\n[00FFFF]/skip[-] to vote to skip the level"));
                }
                else if (Regex.Match(message, @"^/clear$").Success)
                {
                    string levelName = "";
                    if (PlayerVotes.ContainsKey(player.UnityPlayerGuid))
                    {
                        levelName = $" for [00FF00]{PlayerVotes[player.UnityPlayerGuid].Name}[-]";
                    }
                    PlayerVotes.Remove(player.UnityPlayerGuid);
                    PlayerVoteTimes.Remove(player.UnityPlayerGuid);
                    Server.SayLocalChat(player.UnityPlayer, DistanceChat.Server("VoteCommands:Feedback:LevelVoteAdded", $"Removed your vote" + levelName));
                }
                return;
            }

            if (isMuted && isVote)
            {
                Server.SayLocalChat(player.UnityPlayer, DistanceChat.Server("VoteCommands:Muted", "You are not allowed to vote while muted"));
                return;
            }

            DistanceServerMainStarter.Instance.StartCoroutine(SearchForLevels(player, levelSearchName, isVote, isAgainst));
        }

        public void CheckForSkip()
        {
            if (HasSkipped || Server.ValidPlayers.Count == 0)
            {
                return;
            }
            if (SkipVotes.Count >= NeededVotesToSkipLevel)
            {
                HasSkipped = true;
                autoServer.AdvanceLevel();
                foreach (var player in Server.ValidPlayers)
                {
                    if (player.Car != null && !player.Car.Finished)
                    {
                        player.Car.BroadcastDNF();
                    }
                }
                Server.SayChat(DistanceChat.Server("VoteCommands:SkipSuccess", $"Votes to skip the level have passed {(int)(SkipThreshold*100)}%. Skipping the level in 10 seconds."));
            }
        }

        public void CheckForExtend()
        {
            if (Server.ValidPlayers.Count == 0)
            {
                return;
            }
            if (ExtendVotes.Count >= NeededVotesToExtendLevel)
            {
                var success = autoServer.ExtendTimeout(ExtendTime);
                if (success)
                {
                    Server.SayChat(DistanceChat.Server("VoteCommands:ExtendSuccess", $"Votes to extend the level have passed {(int)(ExtendThreshold * 100)}%. Extending the level by {GetExtendTimeText()}"));
                }
                else
                {
                    DelayedExtensions++;
                }
                ExtendVotes.Clear();
            }
        }

        public System.Collections.IEnumerator SearchForLevels(DistancePlayer searcher, string searchText, bool isVote, bool isAgainst)
        {
            var autoServer = Manager.GetPlugin<BasicAutoServer.BasicAutoServer>();
            var byMatch = Regex.Match(searchText, @"by (.*)$");
            string onlyBy = null;
            if (byMatch.Success)
            {
                searchText = Regex.Replace(searchText, @"\s*by (.*)$", "");
                onlyBy = byMatch.Groups[1].ToString();
            }
            var searches = new List<WorkshopSearch.DistanceSearchRetriever>();
            if (RequiredTags.Count == 0)
            {
                searches.Add(new WorkshopSearch.DistanceSearchRetriever(new WorkshopSearch.DistanceSearchParameters()
                {
                    Search = new WorkshopSearch.WorkshopSearchParameters()
                    {
                        AppId = WorkshopSearch.Workshop.DistanceAppId,
                        SearchText = searchText,
                        SearchType = WorkshopSearch.WorkshopSearchParameters.SearchTypeType.GameFiles,
                        Sort = WorkshopSearch.WorkshopSearchParameters.SortType.Relevance,
                        Days = -1,
                        NumPerPage = 30,
                        Page = 1,
                        RequiredTags = new string[] { "Sprint" },
                    },
                    MaxSearch = 5 * 30,
                    MaxResults = 3,
                    DistanceLevelFilter = (levels) =>
                    {
                        if (onlyBy != null)
                        {
                            levels.RemoveAll(level =>
                            {
                                return !level.WorkshopItemResult.AuthorName.ToLower().Contains(onlyBy.ToLower());
                            });
                        }
                        return autoServer.FilterWorkshopLevels(levels);
                    }
                }));
            }
            else
            {
                foreach (var tag in RequiredTags)
                {
                    searches.Add(new WorkshopSearch.DistanceSearchRetriever(new WorkshopSearch.DistanceSearchParameters()
                    {
                        Search = new WorkshopSearch.WorkshopSearchParameters()
                        {
                            AppId = WorkshopSearch.Workshop.DistanceAppId,
                            SearchText = searchText,
                            SearchType = WorkshopSearch.WorkshopSearchParameters.SearchTypeType.GameFiles,
                            Sort = WorkshopSearch.WorkshopSearchParameters.SortType.Relevance,
                            Days = -1,
                            NumPerPage = 30,
                            Page = 1,
                            RequiredTags = new string[] { "Sprint", tag },
                        },
                        MaxSearch = 5 * 30,
                        MaxResults = 3,
                        DistanceLevelFilter = (levels) =>
                        {
                            if (onlyBy != null)
                            {
                                levels.RemoveAll(level =>
                                {
                                    return !level.WorkshopItemResult.AuthorName.ToLower().Contains(onlyBy.ToLower());
                                });
                            }
                            return autoServer.FilterWorkshopLevels(levels);
                        }
                    }));
                }
            }

            var items = new List<WorkshopSearch.DistanceSearchResultItem>();
            foreach (var search in searches)
            {
                yield return search.TaskCoroutine;
                if (search.HasError)
                {
                    Server.SayLocalChat(searcher.UnityPlayer, DistanceChat.Server("VoteCommands:Feedback:SearchError", $"Error when searching for \"{searchText}\""));
                    Log.Error($"Error when searching for \"{searchText}\": {search.Error}");
                    yield break;
                }
                items.AddRange(search.Results);
            }
            
            if (items.Count == 0)
            {
                Server.SayLocalChat(searcher.UnityPlayer, DistanceChat.Server("VoteCommands:Feedback:SearchResult", $"No levels found for \"{searchText}\"" + (onlyBy != null ? $" by \"{onlyBy}\"" : "")));
                yield break;
            }
            if (!isVote)
            {
                var result = $"Levels for \"{searchText}\":";
                for (int i = 0; i < Math.Min(3, items.Count); i++)
                {
                    var item = items[i].WorkshopItemResult;
                    result += $"\n[00FF00]{item.ItemName}[-] by {item.AuthorName}" + (item.Rating == -1 ? "" : $" {item.Rating}/5");
                }
                Server.SayLocalChat(searcher.UnityPlayer, DistanceChat.Server("VoteCommands:Feedback:SearchResult", result));
                yield break;
            }
            else
            {
                var result = items[0].DistanceLevelResult;
                if (!isAgainst)
                {
                    var data = new FilterLevelRealtimeEventData(result);
                    OnFilterLevelRealtime.Fire(data);
                    if (data.HardBlocklist)
                    {
                        Server.SayChat(DistanceChat.Server("VoteCommands:Feedback:VoteHardBlocked", $"The level [00FF00]{result.Name}[-] by {items[0].WorkshopItemResult.AuthorName} is blocked because {data.Reason}."));
                        yield break;
                    }
                    PlayerVotes[searcher.UnityPlayerGuid] = result;
                    PlayerVoteTimes[searcher.UnityPlayerGuid] = DistanceServerMain.UnixTime;
                    Server.SayChat(DistanceChat.Server("VoteCommands:Feedback:VoteSuccess", $"Set {searcher.Name}'s vote to [00FF00]{result.Name}[-] by {items[0].WorkshopItemResult.AuthorName}"));
                    if (data.SoftBlocklist)
                    {
                        int count = PlayerVotes.Sum(pair =>
                        {
                            if (pair.Value.RelativeLevelPath != result.RelativeLevelPath || Server.GetDistancePlayer(pair.Key) == null)
                            {
                                return 0;
                            }
                            return 1;
                        });
                        int sub = 0;
                        if (AgainstVotes.ContainsKey(result.RelativeLevelPath))
                        {
                            sub = AgainstVotes[result.RelativeLevelPath].Sum(playerGuid => Server.GetDistancePlayer(playerGuid) != null ? 1 : 0);
                        }
                        count = count - sub;
                        int needed = NeededVotesToExtendLevel - count;
                        if (needed > 0)
                        {
                            Server.SayChat(DistanceChat.Server("VoteCommands:Feedback:VoteSoftBlocked", $"The level [00FF00]{result.Name}[-] is soft-blocked and needs {needed} more votes to be played because {data.Reason}."));
                        }
                        else
                        {
                            Server.SayChat(DistanceChat.Server("VoteCommands:Feedback:VoteSuccessSoftBlock", $"The level [00FF00]{result.Name}[-] is soft-blocked but has met its required vote count and can now be played."));
                        }
                    }
                }
                else
                {
                    var key = result.RelativeLevelPath;
                    if (!AgainstVotes.ContainsKey(key))
                    {
                        AgainstVotes[key] = new HashSet<string>();
                    }
                    if (AgainstVotes[key].Contains(searcher.UnityPlayerGuid))
                    {
                        AgainstVotes[key].Remove(searcher.UnityPlayerGuid);
                        if (AgainstVotes[key].Count == 0)
                        {
                            AgainstVotes.Remove(key);
                        }
                        Server.SayChat(DistanceChat.Server("VoteCommands:Feedback:AgainstVoteRemoved", $"Cleared {searcher.Name}'s vote against [00FF00]{result.Name}[-] by {items[0].WorkshopItemResult.AuthorName}"));
                    }
                    else
                    {
                        AgainstVotes[key].Add(searcher.UnityPlayerGuid);
                        Server.SayChat(DistanceChat.Server("VoteCommands:Feedback:AgainstVoteAdded", $"Set {searcher.Name}'s vote against [00FF00]{result.Name}[-] by {items[0].WorkshopItemResult.AuthorName}"));
                    }
                }
                yield break;
            }
        }
    }
}
