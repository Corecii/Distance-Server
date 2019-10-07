﻿extern alias Distance;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using WorkshopSearch;


namespace BasicAutoServer
{
    public class BasicAutoServer : DistanceServerPlugin
    {

        public override string DisplayName => "Basic Auto Server";
        public override string Author => "Corecii; Discord: Corecii#3019";
        public override int Priority => -5;
        public override SemanticVersion ServerVersion => new SemanticVersion("0.1.3");

        public enum Stage
        {
            Starting,
            Started,
            Timeout,
            AllFinished,
        }
        
        public int currentLevelIndex = 0;
        public Stage ServerStage = 0;

        double allFinishedAt = 0;

        double countdownTime = -1.0;

        public List<DistanceLevel> Playlist;

        public double LevelEndTime = -1;

        public HashSet<string> StartingPlayerGuids = new HashSet<string>();

        double lastMasterServerRegistration = 0.0;
        bool masterServerDeregistered = false;

        int currentTip = 0;

        ///

        public List<DistanceLevel> OverridePlaylist = new List<DistanceLevel>();

        public delegate bool FilterWorkshopSearchDelegate(List<DistanceSearchResultItem> results);
        List<FilterWorkshopSearchDelegate> Filters;
        public void AddWorkshopFilter(FilterWorkshopSearchDelegate filter)
        {
            Filters.Add(filter);
        }
        public bool FilterWorkshopLevels(List<DistanceSearchResultItem> results)
        {
            var cont = true;
            foreach (var filter in Filters)
            {
                cont = cont && filter(results);
            }
            return cont;
        }

        ///

        public delegate string GetLinkCode(DistancePlayer player);

        public GetLinkCode LinkCodeGetter = null;

        ///

        public delegate string GetTimeoutMessage(string timeLeft);
        public delegate string GetStartingPlayersFinishedMessage();

        public GetTimeoutMessage TimeoutMessageGetter = time => $"Server has been on this level for {time}. Advancing to the next level in 60 seconds.";
        public GetStartingPlayersFinishedMessage StartingPlayersFinishedMessageGetter = () => $"All initial players finished. Advancing to the next level in 60 seconds.";

        ///

        string MasterServerGameModeOverride = null;
        string ServerName = "Auto Server";
        string PrivateServerPassword = null;
        int MaxPlayers = 24;
        int Port = 45671;
        bool ReportToMasterServer = true;
        double MasterServerReRegisterFrequency = 5 * 60.0;

        bool LoadWorkshopLevels = false;
        public bool AdvanceWhenStartingPlayersFinish = true;
        public double IdleTimeout = 60;
        public double LevelTimeout = 7 * 60;
        public string WelcomeMessage = null;
        public List<string> TipMessages = new List<string>();

        public void ReadSettings()
        {
            var filePath = new System.IO.FileInfo(Manager.ServerDirectory.FullName + "/BasicAutoServer.json");
            if (!filePath.Exists)
            {
                Log.Info("No BasicAutoServer.json, using default settings");
                return;
            }
            try
            {
                var txt = System.IO.File.ReadAllText(filePath.FullName);
                var reader = new JsonFx.Json.JsonReader();
                var dictionary = (Dictionary<string, object>)reader.Read(txt);
                TryGetValue(dictionary, "MasterServerGameModeOverride", ref MasterServerGameModeOverride);
                TryGetValue(dictionary, "ServerName", ref ServerName);
                TryGetValue(dictionary, "MaxPlayers", ref MaxPlayers);
                TryGetValue(dictionary, "Port", ref Port);
                TryGetValue(dictionary, "PrivateServerPassword", ref PrivateServerPassword);
                TryGetValue(dictionary, "ReportToMasterServer", ref ReportToMasterServer);
                TryGetValue(dictionary, "MasterServerReRegisterFrequency", ref MasterServerReRegisterFrequency);
                TryGetValue(dictionary, "LoadWorkshopLevels", ref LoadWorkshopLevels);
                TryGetValue(dictionary, "AdvanceWhenStartingPlayersFinish", ref AdvanceWhenStartingPlayersFinish);
                TryGetValue(dictionary, "IdleTimeout", ref IdleTimeout);
                TryGetValue(dictionary, "WelcomeMessage", ref WelcomeMessage);
                TryGetValue(dictionary, "LevelTimeout", ref LevelTimeout);
                var tipsBase = new object[0];
                TryGetValue(dictionary, "TipMessages", ref tipsBase);
                foreach (object tipBase in tipsBase)
                {
                    TipMessages.Add((string)tipBase);
                }

                Log.Info("Loaded settings from BasicAutoServer.json");
            }
            catch (Exception e)
            {
                Log.Error($"Couldn't read BasicAutoServer.json. Is your json malformed?\n{e}");

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

        class LastMoveTimeData
        {
            public double LastMoveTime = 0;
            public LastMoveTimeData(double at)
            {
                LastMoveTime = at;
            }
        }

        public override void Start()
        {
            Log.Info("Basic Auto Server started!");
            Playlist = OfficialPlaylist;
            Filters = new List<FilterWorkshopSearchDelegate>();
            ReadSettings();

            Server.MasterServerGameModeOverride = MasterServerGameModeOverride;
            Server.ServerName = ServerName;
            Server.MaxPlayers = MaxPlayers;
            Server.Port = Port;
            Server.ReportToMasterServer = ReportToMasterServer;
            lastMasterServerRegistration = DistanceServerMain.UnixTime;
            if (PrivateServerPassword != null)
            {
                UnityEngine.Network.incomingPassword = PrivateServerPassword;
            }

            if (IdleTimeout > 0)
            {
                Server.OnPlayerConnectedEvent.Connect(player =>
                {
                    player.Countdown = countdownTime;
                    player.OnCarAddedEvent.Connect(car =>
                    {
                        car.AddExternalData(new LastMoveTimeData(DistanceServerMain.UnixTime));
                    });
                });
            }

            if (WelcomeMessage != null)
            {
                Server.OnPlayerValidatedEvent.Connect(player =>
                {
                    var message = WelcomeMessage;
                    if (message.Contains("$linkcode") && LinkCodeGetter != null)
                    {
                        message = message.Replace("$linkcode", LinkCodeGetter(player));
                    }
                    message = message.Replace("$player", player.Name);
                    if (!Server.HasModeStarted || player.Car != null)
                    {
                        Server.SayLocalChatMessage(player.UnityPlayer, message);
                    }
                    else
                    {
                        LocalEvent<DistanceCar>.EventConnection connection = null;
                        connection = player.OnCarAddedEvent.Connect(car =>
                        {
                            Server.SayLocalChatMessage(player.UnityPlayer, message);
                            connection.Disconnect();
                        });
                    }
                });
            }

            Server.OnLevelStartedEvent.Connect(() =>
            {
                LevelEndTime = -1.0;
                countdownTime = -1.0;
            });

            Server.OnModeStartedEvent.Connect(() =>
            {
                StartingPlayerGuids.Clear();
                foreach (var player in Server.ValidPlayers)
                {
                    if (player.Car != null)
                    {
                        player.Car.GetExternalData<LastMoveTimeData>().LastMoveTime = DistanceServerMain.UnixTime;
                        SetStartingPlayer(player.UnityPlayerGuid, true);
                    }
                }
                if (LevelEndTime == -1.0)
                {
                    LevelEndTime = Server.ModeStartTime + LevelTimeout;
                }
                if (ServerStage == Stage.Starting)
                {
                    ServerStage = Stage.Started;
                }
            });

            OnAdvancingToNextLevel.Connect(() =>
            {
                if (TipMessages.Count > 0)
                {
                    var tip = TipMessages[currentTip];
                    currentTip = (currentTip + 1) % TipMessages.Count;
                    Server.SayChatMessage(true, tip);
                }
            });
            
            DistanceServerMain.GetEvent<Events.Instanced.Finished>().Connect((instance, data) =>
            {
                Log.WriteLine($"{((DistanceCar)instance).Player.Name} finished");
                if (ServerStage != Stage.Starting && ServerStage != Stage.Started && ServerStage != Stage.Timeout)
                {
                    return;
                }
                AttemptToAdvanceLevel();
            });

            if (LoadWorkshopLevels)
            {
                DistanceServerMainStarter.Instance.StartCoroutine(DoLoadWorkshopLevels());
            }
            else
            {
                StartAutoServer();
            }
        }

        void StartAutoServer()
        {

            Server.CurrentLevel = Playlist[0];
            StartLevel();

            Server.OnUpdateEvent.Connect(Update);
            Server.OnPlayerDisconnectedEvent.Connect(player =>
            {
                if (Server.ValidPlayers.Count == 0)
                {
                    Server.SayChatMessage(true, "All players have left. Advancing level.");
                    AdvanceLevel();
                }
                else
                {
                    AttemptToAdvanceLevel();
                }
            });
        }

        System.Collections.IEnumerator DoLoadWorkshopLevels()
        {
            // Start players on campaign maps while the workshop maps load
            StartAutoServer();
            NextLevel();
            Server.SayChatMessage(true, "Generating playlist from Steam Workshop...");
            Server.OnLevelStartInitiatedEvent.Connect(() =>
            {
                if (currentLevelIndex == Playlist.Count - 1)
                {
                    // Update the playlist on the last level
                    DistanceServerMainStarter.Instance.StartCoroutine(UpdatePlaylist());
                }
            });
            // Load maps, but don't wait on them to finish -- it might error!
            DistanceServerMainStarter.Instance.StartCoroutine(UpdatePlaylist());
            yield break;
        }

        void AttemptToAdvanceLevel()
        {
            if (Server.StartingLevel || !Server.HasModeStarted)
            {
                Log.Debug($"Mode not started, not advancing normally.");
                return;
            }
            if (Server.ValidPlayers.Count == 0)
            {
                Log.Debug($"No players, not advancing normally.");
                return;
            }
            var canAdvance = true;
            foreach (var player in Server.ValidPlayers)
            {
                if ((player.Car != null && !player.Car.Finished) || DistanceServerMain.UnixTime - player.RestartTime < 30)
                {
                    canAdvance = false;
                    break;
                }
            }
            if (canAdvance)
            {
                Log.Debug($"Advancing because all players with cars have finished.");
                Server.SayChatMessage(true, "All players finished. Advancing to the next level in 10 seconds.");
                AdvanceLevel();
            }
            else if (AdvanceWhenStartingPlayersFinish)
            {
                if (ServerStage == Stage.Started && GetUnfinishedStartingPlayersCount() == 0)
                {
                    LevelEndTime = DistanceServerMain.NetworkTime + 60.0;
                    ServerStage = Stage.Timeout;
                    setCountdownTime(Server.ModeTime + 60.0);
                    Server.SayChatMessage(true, StartingPlayersFinishedMessageGetter());
                }
            }
        }

        public void AdvanceLevel()
        {
            ServerStage = Stage.AllFinished;
            allFinishedAt = (ServerStage == Stage.Starting ? DistanceServerMain.NetworkTime - 10 : DistanceServerMain.NetworkTime);
        }

        public void SetNextLevel(DistanceLevel level)
        {
            OverridePlaylist.Insert(0, level);
        }

        public LocalEventEmpty OnAdvancingToNextLevel = new LocalEventEmpty();
        public void NextLevel()
        {
            OnAdvancingToNextLevel.Fire();
            if (OverridePlaylist.Count > 0)
            {
                Server.CurrentLevel = OverridePlaylist[0];
                OverridePlaylist.RemoveAt(0);
            }
            else
            {
                currentLevelIndex++;
                if (currentLevelIndex >= Playlist.Count)
                {
                    currentLevelIndex = 0;
                }
                Server.CurrentLevel = Playlist[currentLevelIndex];
            }
            StartLevel();
        }

        public void StartLevel()
        {
            ServerStage = Stage.Starting;
            Server.StartLevel();
        }

        public bool IsStartingPlayer(string guid)
        {
            return StartingPlayerGuids.Contains(guid);
        }

        public void SetStartingPlayer(string guid, bool isStartingPlayer)
        {
            if (isStartingPlayer)
            {
                StartingPlayerGuids.Add(guid);
            }
            else
            {
                StartingPlayerGuids.Remove(guid);
            }
        }

        public int GetUnfinishedStartingPlayersCount()
        {
            var count = 0;
            foreach (var guid in StartingPlayerGuids)
            {
                var player = Server.GetDistancePlayer(guid);
                if (player != null && player.Car != null && !player.Car.Finished)
                {
                    count++;
                }
            }
            return count;
        }

        public string GenerateLevelTimeoutText(double timeoutBase = -1.0)
        {
            if (timeoutBase == -1.0)
            {
                timeoutBase = LevelTimeout;
            }
            int timeout = (int)timeoutBase;
            if (timeout % 60 == 0)
            {
                return $"{timeout / 60} minutes";
            }
            return $"{timeout / 60}:{timeout % 60}";
        }

        public bool ExtendTimeout(double time)
        {
            if (LevelEndTime == -1.0)
            {
                return false;
            }
            LevelEndTime += time;
            if (ServerStage == Stage.Timeout && UnityEngine.Network.time < LevelEndTime - 60.0)
            {
                ServerStage = Stage.Started;
                setCountdownTime(-1.0);
            }
            return true;
        }

        void setCountdownTime(double time)
        {
            countdownTime = time;
            foreach (var player in Server.DistancePlayers.Values)
            {
                player.UpdateCountdown(time);
            }
        }

        public void Update()
        {
            if (ServerStage == Stage.Started && UnityEngine.Network.time >= LevelEndTime - 60.0)
            {
                ServerStage = Stage.Timeout;
                var timeLeft = LevelEndTime - UnityEngine.Network.time;
                setCountdownTime(Server.ModeTime + timeLeft);
                Server.SayChatMessage(true, TimeoutMessageGetter(GenerateLevelTimeoutText(LevelTimeout - 60.0)));
            }
            else if (ServerStage == Stage.Started && IdleTimeout > 0)
            {
                foreach (var player in Server.ValidPlayers)
                {
                    if (player.Car != null && player.Car.CarDirectives != null && !player.Car.Finished)
                    {
                        var lastMoveData = player.Car.GetExternalData<LastMoveTimeData>();
                        if (!player.Car.CarDirectives.IsZero())
                        {
                            lastMoveData.LastMoveTime = DistanceServerMain.UnixTime;
                        }
                        else if (DistanceServerMain.UnixTime - lastMoveData.LastMoveTime >= IdleTimeout)
                        {
                            lastMoveData.LastMoveTime = DistanceServerMain.UnixTime;
                            player.Car.BroadcastDNF();
                            Server.SayChatMessage(true, $"{player.Name} has been set to spectate mode for being idle");
                        }
                    }
                }
            }
            else if (ServerStage == Stage.Timeout && UnityEngine.Network.time >= LevelEndTime)
            {
                NextLevel();
            }
            else if (ServerStage == Stage.AllFinished && UnityEngine.Network.time - allFinishedAt >= 10)
            {
                NextLevel();
            }
            if (ReportToMasterServer)
            {
                if (!masterServerDeregistered)
                {
                    if (DistanceServerMain.UnixTime - lastMasterServerRegistration > MasterServerReRegisterFrequency)
                    {
                        Log.Debug("Deregistering from master server...");
                        masterServerDeregistered = true;
                        Server.ReportToMasterServer = false;
                    }
                }
                else if (DistanceServerMain.UnixTime - lastMasterServerRegistration > MasterServerReRegisterFrequency + 10.0)
                {
                    Log.Debug("Re-reregistering to master server...");
                    Server.ReportToMasterServer = true;
                    masterServerDeregistered = false;
                    lastMasterServerRegistration = DistanceServerMain.UnixTime;
                }
            }
        }

        System.Collections.IEnumerator UpdatePlaylist()
        {
            HashSet<string> foundLevels = new HashSet<string>();
            List<DistanceSearchRetriever> levelRetrievers = new List<DistanceSearchRetriever>();
            var filePath = new System.IO.FileInfo(Manager.ServerDirectory.FullName + "/BasicAutoServer.json");
            if (filePath.Exists)
            {
                try
                {
                    var txt = System.IO.File.ReadAllText(filePath.FullName);
                    var reader = new JsonFx.Json.JsonReader();
                    var dictionary = (Dictionary<string, object>)reader.Read(txt);
                    if (dictionary.ContainsKey("Workshop"))
                    {
                        Log.Info("Using workshop level info stored in BasicAutoServer.json");
                        var levelSettings = (object[])dictionary["Workshop"];
                        foreach (var settingsObject in levelSettings)
                        {
                            var settings = (Dictionary<string, object>)settingsObject;
                            var search = (string)settings["Search"];
                            var sort = (WorkshopSearchParameters.SortType)Enum.Parse(typeof(WorkshopSearchParameters.SortType), (string)settings["Sort"]);
                            var days = (int)settings["Days"];
                            var tagsBase = (object[])settings["Tags"];
                            var tagsList = new List<string>() { };
                            foreach (object tagBase in tagsBase)
                            {
                                tagsList.Add((string)tagBase);
                            }
                            var count = (int)settings["Count"];
                            var searchParams = WorkshopSearchParameters.GameFiles(
                                searchText: search,
                                appId: Workshop.DistanceAppId,
                                sort: sort,
                                days: days,
                                requiredTags: tagsList.ToArray(),
                                numPerPage: 30
                            );
                            var retriever = new DistanceSearchRetriever(new DistanceSearchParameters()
                            {
                                Search = searchParams,
                                DistanceLevelFilter = (levels) =>
                                {
                                    levels.RemoveAll(level =>
                                    {
                                        return foundLevels.Contains(level.DistanceLevelResult.RelativeLevelPath);
                                    });
                                    foreach (var level in levels)
                                    {
                                        foundLevels.Add(level.DistanceLevelResult.RelativeLevelPath);
                                    }
                                    var res = FilterWorkshopLevels(levels);
                                    return res;
                                },
                                MaxSearch = count
                            }, false);
                            levelRetrievers.Add(retriever);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Error retrieving workshop level settings:\n{e}");
                }
            }
            if (levelRetrievers == null || levelRetrievers.Count == 0)
            {
                Log.Error("No workshop levels defined. Using default: official levels");
                Playlist = OfficialPlaylist;
                yield break;
            }

            var levelListIndex = 0;
            foreach (var retriever in levelRetrievers)
            {
                retriever.StartCoroutine();
                yield return retriever.TaskCoroutine;
                if (retriever.HasError)
                {
                    Log.Error($"Error retrieving levels: {retriever.Error}");
                }
                foreach(var levelInfo in retriever.Results)
                {
                    levelInfo.DistanceLevelResult.AddExternalData(new ListIndexExternalData(levelListIndex));
                }
                levelListIndex++;
            }

            Log.Info($"Level Retrievers: {levelRetrievers.Count}");

            var levelResults = levelRetrievers.ConvertAll(retriever => retriever.Results.ConvertAll(result => result.DistanceLevelResult));
            var results = Combine(levelResults.ToArray());

            var listStr = $"Levels ({results.Count}):";
            foreach (var level in results)
            {
                listStr += $"\n{level.GetExternalData<ListIndexExternalData>().listIndex} {level.Name}";
            }
            Log.Info(listStr);

            if (results.Count == 0)
            {
                Log.Error("Workshop search returned nothing, using default: official levels");
                Playlist = OfficialPlaylist;
                yield break;
            }

            Playlist = results;
            currentLevelIndex = -1;
        }

        public class ListIndexExternalData
        {
            public int listIndex = 0;
            public ListIndexExternalData(int index)
            {
                listIndex = index;
            }
        }

        List<T> Combine<T>(List<T>[] lists)
        {
            var result = new List<T>();
            var frequency = new double[lists.Length];
            var counter = new double[lists.Length];
            var index = new int[lists.Length];
            var max = 0;
            foreach (var list in lists)
            {
                if (list.Count > max)
                {
                    max = list.Count;
                }
            }
            for (var i = 0; i < lists.Length; i++)
            {
                frequency[i] = (double)max / lists[i].Count;
            }
            var added = 1;
            while (added > 0)
            {
                added = 0;
                for (var i = 0; i < lists.Length; i++)
                {
                    var list = lists[i];
                    if (index[i] < list.Count)
                    {
                        added++;
                        counter[i]++;
                        while (counter[i] >= frequency[i] && index[i] < list.Count)
                        {
                            counter[i] -= frequency[i];
                            var listIndex = index[i];
                            result.Add(list[listIndex]);
                            index[i]++;
                        }
                    }
                }
            }
            return result;
        }

        List<DistanceLevel> OfficialPlaylist = new List<DistanceLevel>()
        {
            new DistanceLevel()
            {
                Name = "Cataclysm",
                RelativeLevelPath = "OfficialLevels/Cataclysm.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
            new DistanceLevel()
            {
                Name = "Diversion",
                RelativeLevelPath = "OfficialLevels/Diversion.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
            new DistanceLevel()
            {
                Name = "Euphoria",
                RelativeLevelPath = "OfficialLevels/Euphoria.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
            new DistanceLevel()
            {
                Name = "Entanglement",
                RelativeLevelPath = "OfficialLevels/Entanglement.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
            new DistanceLevel()
            {
                Name = "Automation",
                RelativeLevelPath = "OfficialLevels/Automation.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
            new DistanceLevel()
            {
                Name = "Abyss",
                RelativeLevelPath = "OfficialLevels/Abyss.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
            new DistanceLevel()
            {
                Name = "Embers",
                RelativeLevelPath = "OfficialLevels/Embers.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
            new DistanceLevel()
            {
                Name = "Isolation",
                RelativeLevelPath = "OfficialLevels/Isolation.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
            new DistanceLevel()
            {
                Name = "Repulsion",
                RelativeLevelPath = "OfficialLevels/Repulsion.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
            new DistanceLevel()
            {
                Name = "Compression",
                RelativeLevelPath = "OfficialLevels/Compression.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
            new DistanceLevel()
            {
                Name = "Research",
                RelativeLevelPath = "OfficialLevels/Research.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
            new DistanceLevel()
            {
                Name = "Contagion",
                RelativeLevelPath = "OfficialLevels/Contagion.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
            new DistanceLevel()
            {
                Name = "Overload",
                RelativeLevelPath = "OfficialLevels/Overload.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
            new DistanceLevel()
            {
                Name = "Ascension",
                RelativeLevelPath = "OfficialLevels/Ascension.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
            new DistanceLevel()
            {
                Name = "Forgotten Utopia",
                RelativeLevelPath = "OfficialLevels/Forgotten Utopia.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
            new DistanceLevel()
            {
                Name = "A Deeper Void",
                RelativeLevelPath = "OfficialLevels/A Deeper Void.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
            new DistanceLevel()
            {
                Name = "Eye of the Storm",
                RelativeLevelPath = "OfficialLevels/Eye of the Storm.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
            new DistanceLevel()
            {
                Name = "The Sentinel Still Watches",
                RelativeLevelPath = "OfficialLevels/The Sentinel Still Watches.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
            new DistanceLevel()
            {
                Name = "Shadow of the Beast",
                RelativeLevelPath = "OfficialLevels/Shadow of the Beast.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
            new DistanceLevel()
            {
                Name = "Pulse of a Violent Heart",
                RelativeLevelPath = "OfficialLevels/Pulse of a Violent Heart.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
            new DistanceLevel()
            {
                Name = "It Was Supposed To Be Perfect",
                RelativeLevelPath = "OfficialLevels/It Was Supposed To Be Perfect.bytes",
                WorkshopFileId = "",
                GameMode = "Sprint",
            },
        };
    }
}
