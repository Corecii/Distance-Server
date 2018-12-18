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

        public bool HasSkipped = false;
        public double SkipThreshold = .7;

        public Dictionary<string, double> LeftAt = new Dictionary<string, double>();
        public Dictionary<string, DistanceLevel> PlayerVotes = new Dictionary<string, DistanceLevel>();
        public List<string> SkipVotes = new List<string>();

        public int NeededVotesToSkipLevel => (int)Math.Ceiling(Server.ValidPlayers.Count * SkipThreshold);

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
            });

            Server.OnLevelStartedEvent.Connect(() =>
            {
                SkipVotes.Clear();
                HasSkipped = false;
            });

            DistanceServerMain.GetEvent<Events.ClientToAllClients.ChatMessage>().Connect(ProcessChatMessage);

            autoServer = Manager.GetPlugin<BasicAutoServer.BasicAutoServer>();
            autoServer.OnAdvancingToNextLevel.Connect(OnAdvancingToNextLevel);

            Server.OnPlayerDisconnectedEvent.Connect(player =>
            {
                LeftAt[player.UnityPlayerGuid] = player.LeftAt;
            });
        }

        BasicAutoServer.BasicAutoServer autoServer;
        void OnAdvancingToNextLevel()
        {
            var validVotes = PlayerVotes.ToList();
            for (int i = 0; i < validVotes.Count; i++)
            {
                var vote = validVotes[i];
                if (Server.GetDistancePlayer(vote.Key) == null)
                {
                    validVotes.RemoveAt(i);
                    i--;
                    if (!LeftAt.ContainsKey(vote.Key) || DistanceServerMain.UnixTime - LeftAt[vote.Key] > 5 * 60)
                    {
                        PlayerVotes.Remove(vote.Key);
                        LeftAt.Remove(vote.Key);
                    }
                }
            }
            if (validVotes.Count == 0)
            {
                return;
            }
            var choice = validVotes[new Random().Next(validVotes.Count)];
            PlayerVotes.Remove(choice.Key);
            autoServer.SetNextLevel(choice.Value);
            Server.SayChatMessage(true, "Choosing a map at random from the voted-for tracks");
            var voteCount = 0;
            foreach (var vote in PlayerVotes.ToList())
            {
                if (vote.Value.RelativeLevelPath == choice.Value.RelativeLevelPath)
                {
                    voteCount++;
                    PlayerVotes.Remove(vote.Key);
                }
            }
            var nextLevelId = Server.CurrentLevelId + 1;
            LocalEventEmpty.EventConnection[] conns = new LocalEventEmpty.EventConnection[2];
            conns[0] = Server.OnModeStartedEvent.Connect(() =>
            {
                Server.SayChatMessage(true, $"Chosen level is [00FF00]{choice.Value.Name}[-], voted for by {Server.GetDistancePlayer(choice.Key).Name}" + (voteCount > 0 ? $" and {voteCount} others" : ""));
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

        void ProcessChatMessage(Distance::Events.ClientToAllClients.ChatMessage.Data data)
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
            match = Regex.Match(message, @"^/help$");
            if (match.Success)
            {
                Server.SayLocalChatMessage(player.UnityPlayer, "[00FFFF]/search /vote /skip[-]");
            }


            match = Regex.Match(message, @"^/skip$");
            if (match.Success && SkipThreshold < 100 && SkipThreshold != -1)
            {

                if (!SkipVotes.Contains(player.UnityPlayerGuid))
                {
                    SkipVotes.Add(player.UnityPlayerGuid);
                    Server.SayLocalChatMessage(player.UnityPlayer, $"Added your vote to skip the level {SkipVotes.Count + 1}/{NeededVotesToSkipLevel}");
                    CheckForSkip();
                }
                else
                {
                    SkipVotes.Remove(player.UnityPlayerGuid);
                    Server.SayLocalChatMessage(player.UnityPlayer, $"Removed your vote to skip the level {SkipVotes.Count + 1}/{NeededVotesToSkipLevel}");
                }
                return;
            }

            var isVote = true;
            match = match = Regex.Match(message, @"^/vote (.*)$");
            if (!match.Success)
            {
                match = Regex.Match(message, @"^/search (.*)$");
                isVote = false;
            }
            if (!match.Success)
            {
                if (Regex.Match(message, @"^/vote$").Success || Regex.Match(message, @"^/search$").Success)
                {
                    if (PlayerVotes.ContainsKey(player.UnityPlayerGuid))
                    {
                        Server.SayLocalChatMessage(player.UnityPlayer, $"Your current vote is for [00FF00]{PlayerVotes[player.UnityPlayerGuid].Name}[-]");
                    }
                    Server.SayLocalChatMessage(player.UnityPlayer, "[00FFFF]/search name[-] or [00FFFF]/search name by author[-] to search\n[00FFFF]/vote name[-] or [00FFFF]/vote name by author[-] to vote for a level\n[00FFFF]/vote clear[-] to clear your vote\n[00FFFF]/skip[-] to vote to skip the level");
                }
                return;
            }

            var levelSearchName = match.Groups[1].ToString();

            if (levelSearchName == "clear")
            {
                string levelName = "";
                if (PlayerVotes.ContainsKey(player.UnityPlayerGuid))
                {
                    levelName = $" for [00FF00]{PlayerVotes[player.UnityPlayerGuid].Name}[-]";
                }
                PlayerVotes.Remove(player.UnityPlayerGuid);
                Server.SayLocalChatMessage(player.UnityPlayer, $"Removed your vote" + levelName);
                return;
            }

            DistanceServerMainStarter.Instance.StartCoroutine(SearchForLevels(player, levelSearchName, isVote));
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
                Manager.GetPlugin<BasicAutoServer.BasicAutoServer>().AdvanceLevel();
                foreach (var player in Server.ValidPlayers)
                {
                    if (player.Car != null && !player.Car.Finished)
                    {
                        player.Car.BroadcastDNF();
                    }
                }
                Server.SayChatMessage(true, $"Votes to skip the level have passed {(int)(SkipThreshold*100)}%. Skipping the level in 10 seconds.");
            }
        }

        public System.Collections.IEnumerator SearchForLevels(DistancePlayer searcher, string searchText, bool isVote)
        {
            var autoServer = Manager.GetPlugin<BasicAutoServer.BasicAutoServer>();
            var byMatch = Regex.Match(searchText, @"by (.*)$");
            string onlyBy = null;
            if (byMatch.Success)
            {
                searchText = Regex.Replace(searchText, @"\s*by (.*)$", "");
                onlyBy = byMatch.Groups[1].ToString();
            }
            var search = new WorkshopSearch.DistanceSearchRetriever(new WorkshopSearch.DistanceSearchParameters()
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
                MaxSearch = 5*30,
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
            });
            yield return search.TaskCoroutine;
            if (search.HasError)
            {
                Server.SayLocalChatMessage(searcher.UnityPlayer, $"Error when searching for \"{searchText}\"");
                Log.Error($"Error when searching for \"{searchText}\": {search.Error}");
                yield break;
            }

            var items = search.Results;
            if (items.Count == 0)
            {
                Server.SayLocalChatMessage(searcher.UnityPlayer, $"No levels found for \"{searchText}\"" + (onlyBy != null ? $" by \"{onlyBy}\"" : ""));
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
                Server.SayLocalChatMessage(searcher.UnityPlayer, result);
                yield break;
            }
            else
            {
                PlayerVotes[searcher.UnityPlayerGuid] = items[0].DistanceLevelResult;
                Server.SayChatMessage(true, $"Set {searcher.Name}'s vote to [00FF00]{items[0].DistanceLevelResult.Name}[-] by {items[0].WorkshopItemResult.AuthorName}");
                yield break;
            }
        }
    }
}
