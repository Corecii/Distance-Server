extern alias Distance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicAutoServer
{
    public class Sprint : GameMode
    {
        public double LevelEndTime = -1.0;

        public bool TimeoutStarted = false;

        public Sprint(BasicAutoServer plugin) : base(plugin) { }

        public override void Start()
        {
            Connections.Add(
                Plugin.Server.OnUpdateEvent.Connect(Update),
                Plugin.Server.OnCheckIfModeCanStartEvent.Connect(OnCheckIfModeCanStart),
                Plugin.Server.OnNeedToSyncLateClientToGameMode.Connect(OnNeedToSyncLateClientToGameMode),
                Plugin.Server.OnModeStartedEvent.Connect(OnModeStarted),
                Plugin.OnCheckIfLevelCanAdvanceEvent.Connect(OnCheckIfLevelCanAdvance)
            );
        }

        public void OnModeStarted()
        {
            LevelEndTime = Plugin.Server.ModeStartTime + Plugin.LevelTimeout;
        }

        public void OnNeedToSyncLateClientToGameMode(DistancePlayer player)
        {
            DistanceServerMain.GetEvent<Events.ServerToClient.SyncMode>().Fire(
                player.UnityPlayer,
                new Distance::Events.GameMode.SyncMode.Data(Plugin.ServerStage == BasicAutoServer.Stage.AllFinished)
            );
        }

        public void OnCheckIfModeCanStart(Cancellable canceller)
        {
            canceller.CancelIf(
                Plugin.Server.ValidPlayers.FindAll((player) => !player.Stuck && player.HasLoadedLevel(false)).Count < 1
            );
        }

        public void OnCheckIfLevelCanAdvance(Cancellable canceller)
        {
            if (canceller.IsCancelled) return;

            var canAdvance = Plugin.AdvanceWhenAllPlayersFinish;
            foreach (var player in Plugin.Server.ValidPlayers)
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
                Plugin.Server.SayChat(DistanceChat.Server("AutoServer:Advancing:Finished", "All players finished. Advancing to the next level in 10 seconds."));
                return;
            }

            canceller.Cancel();

            if (Plugin.AdvanceWhenStartingPlayersFinish)
            {
                if (!TimeoutStarted && Plugin.GetUnfinishedStartingPlayersCount() == 0)
                {
                    LevelEndTime = DistanceServerMain.NetworkTime + 60.0;
                    TimeoutStarted = true;
                    Plugin.SetCountdownTime(Plugin.Server.ModeTime + 60.0);
                    Plugin.Server.SayChat(DistanceChat.Server("AutoServer:Warning:InitialFinished", Plugin.StartingPlayersFinishedMessageGetter()));
                }
            }
        }

        public void Update()
        {
            if (Plugin.ServerStage == BasicAutoServer.Stage.Started && !TimeoutStarted && UnityEngine.Network.time >= LevelEndTime - 60.0)
            {
                TimeoutStarted = true;
                var timeLeft = LevelEndTime - UnityEngine.Network.time;
                Plugin.SetCountdownTime(Plugin.Server.ModeTime + timeLeft);
                Plugin.Server.SayChat(DistanceChat.Server("AutoServer:Warning:Timeout", Plugin.TimeoutMessageGetter(Plugin.GenerateLevelTimeoutText(Plugin.LevelTimeout - 60.0))));
            }
            if (Plugin.ServerStage == BasicAutoServer.Stage.Started && UnityEngine.Network.time >= LevelEndTime)
            {
                Plugin.FinishAllPlayersAndAdvanceLevel();
            }
        }

        public bool ExtendTimeout(double time)
        {
            if (LevelEndTime == -1.0)
            {
                return false;
            }
            LevelEndTime += time;
            if (TimeoutStarted && UnityEngine.Network.time < LevelEndTime - 60.0)
            {
                TimeoutStarted = false;
                Plugin.SetCountdownTime(-1.0);
            }
            return true;
        }


    }
}
