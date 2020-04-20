extern alias Distance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicAutoServer
{
    public class ReverseTagData
    {
        public double SecondsTagged = 0;

        public int MillisTagged => (int)(SecondsTagged*1000);

        public void AddSecondsTagged(double seconds, double maxTime)
        {
            if (SecondsTagged + seconds < 0)
            {
                SecondsTagged = 0;
            }
            else if (SecondsTagged + seconds > maxTime)
            {
                SecondsTagged = maxTime;
            }
            else
            {
                SecondsTagged += seconds;
            }
        }
    }

    public class ReverseTag : GameMode
    {
        public double TagBubbleLockedUntil = -1.0;

        public DistancePlayer TaggedPlayer = null;
        public DistancePlayer Leader = null;
        public double TimeOfLastTag = 0.0;

        public double WinTime;
        public double MaxModeTime;

        public bool IsInSinglePlayerMode = false;

        public bool HasShown30SecondWarning = false;
        public bool HasShown10SecondWarning = false;

        public bool Finished = false;
        
        public ReverseTag(BasicAutoServer plugin, double winTime, double maxModeTime) : base(plugin)
        {
            WinTime = winTime;
            MaxModeTime = maxModeTime;
        }

        public override void Start()
        {
            Connections.Add(
                DistanceServerMain.GetEvent<Events.Instanced.CarRespawn>().Connect(OnCarRespawn),
                DistanceServerMain.GetEvent<Events.ClientToServer.HitTagBubble>().Connect(OnHitTagBubble),
                Plugin.Server.OnNeedToSyncLateClientToGameMode.Connect(OnNeedToSyncLateClientToGameMode),
                Plugin.Server.OnCheckIfModeCanStartEvent.Connect(OnCheckIfModeCanStart),
                Plugin.OnCheckIfLevelCanAdvanceEvent.Connect(OnCheckIfModeCanAdvance),
                Plugin.Server.OnUpdateEvent.Connect(Update),
                Plugin.Server.OnPlayerConnectedEvent.Connect(OnPlayerConnected)
            );

            foreach (var player in Plugin.Server.DistancePlayers.Values)
            {
                player.AddExternalData(new ReverseTagData());
            }
        }

        public override void Destroy()
        {
            base.Destroy();
            
            foreach (var player in Plugin.Server.DistancePlayers.Values)
            {
                player.RemoveExternalData<ReverseTagData>();
            }
        }

        public void OnPlayerConnected(DistancePlayer player) {
            player.AddExternalData(new ReverseTagData());
        }

        public void OnCheckIfModeCanAdvance(Cancellable canceller)
        {
            canceller.Cancel();
        }

        public void OnCarRespawn(object instance, Distance::Events.Player.CarRespawn.Data data)
        {
            var car = (DistanceCar)instance;
            var player = car.Player;

            if (player != TaggedPlayer)
            {
                return;
            }

            TagRandomLastPlacePlayer();
        }

        public void OnHitTagBubble(Distance::Events.ReverseTag.HitTagBubble.Data data, UnityEngine.NetworkMessageInfo info)
        {
            Log.Debug("OnHitTagBubble!");
            if (IsTagBubbleLocked())
            {
                Log.Debug("Bubble locked!");
                return;
            }

            var playerToTag = GetModePlayers().Find((player) => player.UnityPlayer == info.sender);
                // GetModePlayers().Find((player) => player.Index == data.index_);
            if (playerToTag == null)
            {
                Log.Debug($"Player {data.index_} : {info.sender.guid} does not exist!");
                return;
            }

            Log.Debug("Tagging!");

            TagPlayer(playerToTag);
        }

        public void OnNeedToSyncLateClientToGameMode(DistancePlayer player)
        {
            DistanceServerMain.GetEvent<Events.ServerToClient.SyncMode>().Fire(
                player.UnityPlayer,
                new Distance::Events.GameMode.SyncMode.Data(Finished)
            );
            if (TaggedPlayer != null)
            {
                DistanceServerMain.GetEvent<Events.ServerToClient.TaggedPlayer>().Fire(
                    player.UnityPlayer,
                    new Distance::Events.ReverseTag.TaggedPlayer.Data(TaggedPlayer.Index, 0.0, TimeOfLastTag)
                );
            }
        }

        public void OnCheckIfModeCanStart(Cancellable canceller)
        {
            canceller.CancelIf(
                Plugin.Server.ValidPlayers.FindAll((player) => !player.Stuck && player.HasLoadedLevel(false)).Count == 0
            );
        }

        public bool IsTagBubbleLocked()
        {
            return Plugin.Server.ModeTime <= TagBubbleLockedUntil;
        }

        public void LockTagBubbleFor(double seconds)
        {
            TagBubbleLockedUntil = Plugin.Server.ModeTime + seconds;
        }

        public System.Collections.IEnumerator GiveTagBubbleSoon(DistancePlayer fromPlayer, DistancePlayer player)
        {
            yield return new UnityEngine.WaitForSeconds(1);
            if (IsPlayerInMode(player) && TaggedPlayer == fromPlayer)
            {
                fromPlayer.GetExternalData<ReverseTagData>().SecondsTagged = 0.0;
                TagPlayer(player);
            }
            fromPlayer.SayLocalChat(DistanceChat.Server("ReverseTag:SinglePlayerTransition", "Transitioning out of single-player: your timer has been reset and your tag bubble taken!"));
        }

        public void Update()
        {
            if (Finished) return;
            if (Plugin.ServerStage != BasicAutoServer.Stage.Started) return;

            var playersInMode = GetModePlayers();

            if (playersInMode.Count <= 1)
            {
                IsInSinglePlayerMode = true;
                MaxModeTime += UnityEngine.Time.deltaTime;
            }
            else if (IsInSinglePlayerMode)
            {
                IsInSinglePlayerMode = false;
                // When transitioning out of single-player mode, give the bubble to the new player to reset the timer:
                if (TaggedPlayer != null)
                {
                    var nonTagged = playersInMode.Find((player) => TaggedPlayer != player);
                    if (nonTagged != null)
                    {
                        TaggedPlayer.GetExternalData<ReverseTagData>().SecondsTagged = 0.0;
                        DistanceServerMainStarter.Instance.StartCoroutine(GiveTagBubbleSoon(TaggedPlayer, nonTagged));
                    }
                }
            }

            if (TaggedPlayer != null && !IsPlayerInMode(TaggedPlayer))
            {
                TagRandomLastPlacePlayer();
                if (!IsPlayerInMode(TaggedPlayer))
                {
                    TagPlayer(null);
                }
            }

            if (TaggedPlayer != null && !IsInSinglePlayerMode)
            {
                TaggedPlayer.GetExternalData<ReverseTagData>().AddSecondsTagged(UnityEngine.Time.deltaTime, WinTime);
            }

            var leader = GetFirstPlacePlayer();
            if (leader != Leader)
            {
                if (leader != null)
                {
                    var colorHex = Distance::ColorEx.ColorToHexNGUI(Distance::ColorEx.PlayerRainbowColor(leader.Index));
                    var nameColored = $"[{colorHex}]{leader.Name}[-]";
                    var chat = DistanceChat.Server("Vanilla:TakenTheLead", $"{nameColored} has taken the lead!");
                    chat.ChatType = DistanceChat.ChatTypeEnum.ServerVanilla;
                    Plugin.Server.SayChat(chat);
                }
                Leader = leader;
            }

            var leaderSecondsTagged = Leader == null ? 0.0 : Leader.GetExternalData<ReverseTagData>().SecondsTagged;
            if (leaderSecondsTagged >= WinTime || Plugin.Server.ModeTime >= MaxModeTime)
            {
                Finished = true;

                if (Leader != null)
                {
                    var networkFinishedData = new Distance::Events.ReverseTag.Finished.Data(Leader.Index, leaderSecondsTagged);
                    DistanceServerMain.GetEvent<Events.ServerToClient.ReverseTagFinished>().Fire(UnityEngine.RPCMode.Others, networkFinishedData);
                }

                Log.Debug($"Advancing level because win condition met: {leaderSecondsTagged} >= {WinTime} || {Plugin.Server.ModeTime} > {MaxModeTime}");
                Plugin.AdvanceLevel();
            }

            if (Finished) return;

            AdvanceLevelIfOnlyFinishedPlayers();

            var secondsLeft = Math.Max(0.0, Math.Min(WinTime - leaderSecondsTagged, MaxModeTime - Plugin.Server.ModeTime));

            if (secondsLeft <= 30 && !HasShown30SecondWarning)
            {
                HasShown30SecondWarning = true;
                var colorHex = Distance::ColorEx.ColorToHexNGUI(Distance::Colors.orangeRed);
                var chat = DistanceChat.Server("Vanilla:TimeWarning", $"[{colorHex}]30 seconds left![-]");
                chat.ChatType = DistanceChat.ChatTypeEnum.ServerVanilla;
                Plugin.Server.SayChat(chat);
            }
            else if (secondsLeft <= 10 && !HasShown10SecondWarning)
            {
                HasShown10SecondWarning = true;
                var colorHex = Distance::ColorEx.ColorToHexNGUI(Distance::Colors.orangeRed);
                var chat = DistanceChat.Server("Vanilla:TimeWarning", $"[{colorHex}]10 seconds left![-]");
                chat.ChatType = DistanceChat.ChatTypeEnum.ServerVanilla;
                Plugin.Server.SayChat(chat);
            }
        }

        public bool AdvanceLevelIfOnlyFinishedPlayers()
        {
            var modePlayers = GetModePlayers();
            var finishedPlayers = Plugin.Server.ValidPlayers.Where((player) =>
                player.Valid
                && !player.IsLoading()
                && player.LevelId == Plugin.Server.CurrentLevelId
                && player.Car != null
                && player.Car.Finished
                && player.GetExternalData<ReverseTagData>() != null);

            if (modePlayers.Count == 0 && finishedPlayers.Any())
            {
                Plugin.AdvanceLevel();
                return true;
            }
            return false;
        }

        public void TagPlayer(DistancePlayer player)
        {
            if (TaggedPlayer == player)
            {
                Log.Debug("Player same");
                return;
            }
            if (player == null)
            {
                Log.Debug("Player null");
                TaggedPlayer = null;
                return;
            }
            
            var lastTagData = TaggedPlayer?.GetExternalData<ReverseTagData>();
            double lastSecondsTagged = lastTagData == null ? 0.0 : lastTagData.SecondsTagged;

            TimeOfLastTag = Plugin.Server.ModeTime;
            var networkTagData = new Distance::Events.ReverseTag.TaggedPlayer.Data(player.Index, lastSecondsTagged, TimeOfLastTag);

            DistanceServerMain.GetEvent<Events.ServerToClient.TaggedPlayer>().Fire(UnityEngine.RPCMode.Others, networkTagData);

            TaggedPlayer = player;

            LockTagBubbleFor(5);
        }

        public void TagRandomLastPlacePlayer()
        {
            var players = GetLastPlacePlayers(TaggedPlayer);
            if (players.Count == 0) return;

            var player = players[UnityEngine.Random.Range(0, players.Count)];
            TagPlayer(player);
        }

        public bool IsPlayerInMode(DistancePlayer player)
        {
            return player.Valid
                && !player.IsLoading()
                && player.LevelId == Plugin.Server.CurrentLevelId
                && player.Car != null
                && !player.Car.Finished
                && player.GetExternalData<ReverseTagData>() != null;
        }

        public List<DistancePlayer> GetModePlayers()
        {
            return Plugin.Server.ValidPlayers.Where(player => IsPlayerInMode(player)).ToList();
        }

        public DistancePlayer GetFirstPlacePlayer()
        {
            var players = GetModePlayers().Where(player => player.GetExternalData<ReverseTagData>().SecondsTagged > 0.0).ToList();
            players.Sort((a, b) => a.GetExternalData<ReverseTagData>().MillisTagged - b.GetExternalData<ReverseTagData>().MillisTagged);

            return players.Count == 0 ? null : players.Last();
        }

        public List<DistancePlayer> GetLastPlacePlayers(DistancePlayer exclude = null)
        {
            var players = GetModePlayers();

            players.RemoveAll(player => player == exclude);
            players.Sort((a, b) => a.GetExternalData<ReverseTagData>().MillisTagged - b.GetExternalData<ReverseTagData>().MillisTagged);

            var minMillisTagged = -1;
            return players.TakeWhile((player) =>
            {
                if (minMillisTagged == -1)
                {
                    minMillisTagged = player.GetExternalData<ReverseTagData>().MillisTagged;
                }
                return player.GetExternalData<ReverseTagData>().MillisTagged == minMillisTagged;
            }).ToList();
        }
    }
}
