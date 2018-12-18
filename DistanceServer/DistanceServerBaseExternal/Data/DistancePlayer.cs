extern alias Distance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class DistancePlayer : IExternalData
{
    public DistanceServer Server;

    public string UnityPlayerGuid = "";
    public PlayerState State = PlayerState.Initializing;
    public DistanceLevel Level;
    public int LevelId;
    public bool ReceivedInfo = false;
    public int Index = -1;
    public string Name = "";


    public double JoinedAt = 0;
    public double ValidatedAt = 0;
    public double LeftAt = 0;

    public List<object> ExternalData = new List<object>();
    public T GetExternalData<T>()
    {
        return (T)ExternalData.Find(val => val is T);
    }
    public void AddExternalData(object val)
    {
        ExternalData.Add(val);
    }

    public LocalEventEmpty OnValidatedEvent = new LocalEventEmpty();
    public LocalEventEmpty OnValidatedPreReplicationEvent = new LocalEventEmpty();
    public LocalEventEmpty OnDisconnectedEvent = new LocalEventEmpty();

    public LocalEvent<DistanceCar> OnCarAddedEvent = new LocalEvent<DistanceCar>();
    public LocalEvent<DistanceCar> OnCarRemovedEvent = new LocalEvent<DistanceCar>();

    public bool Ready = false;

    DistanceCar car = null;
    public DistanceCar Car
    {
        get
        {
            return car;
        }

        set
        {
            if (car == value)
            {
                return;
            }
            if (car != null)
            {
                car.RemoveNetworker();
            }
            var lastCar = car;
            car = value;
            if (lastCar != null)
            {

                OnCarRemovedEvent.Fire(lastCar);
            }
            if (car != null)
            {
                OnCarAddedEvent.Fire(car);
            }
        }
    }

    public LevelCompatibilityInfo LevelCompatibilityInfo = new LevelCompatibilityInfo(-1, "", false);
    public Distance::LevelCompatabilityStatus LevelCompatability
    {
        get
        {
            if (LevelCompatibilityInfo.LevelCompatibilityId != Server.CurrentLevelId)
            {
                return Distance::LevelCompatabilityStatus.Unverified;
            }
            /*
            if (LevelCompatibilityInfo.LevelVersion != Server.CurrentLevel.LevelVersion)
            {
                return LevelCompatabilityStatus.WrongVersion;
            }
            */
            if (!LevelCompatibilityInfo.HasLevel)
            {
                return Distance::LevelCompatabilityStatus.MissingLevel;
            }
            return Distance::LevelCompatabilityStatus.LevelCompatable;
        }
    }

    public DistancePlayer(DistanceServer server, string guid)
    {
        Server = server;
        UnityPlayerGuid = guid;
    }

    public bool Valid
    {
        get
        {
            return HasUnityPlayer && ReceivedInfo;
        }
    }
    public bool HasUnityPlayer
    {
        get
        {
            return UnityEngine.Network.connections.Any((player) =>
            {
                return player.guid == UnityPlayerGuid;
            });
        }
    }
    public bool unityPlayerSet = false;
    public UnityEngine.NetworkPlayer unityPlayer;
    public void SetUnityPlayer(UnityEngine.NetworkPlayer player)
    {
        unityPlayer = player;
        unityPlayerSet = true;
    }
    public void ClearUnityPlayer()
    {
        unityPlayerSet = false;
    }
    public UnityEngine.NetworkPlayer UnityPlayer
    {
        get
        {
            if (unityPlayerSet) {
                return unityPlayer;
            }
            return UnityEngine.Network.connections.First((player) =>
            {
                return player.guid == UnityPlayerGuid;
            });
        }
    }
    public Distance::Events.ServerToClient.AddClient.Data GetAddClientData(bool displayMessage) {
        return new Distance::Events.ServerToClient.AddClient.Data()
        {
            clientName_ = Name,
            player_ = UnityPlayer,
            displayMessage_ = displayMessage,
            ready_ = Ready,
            status_ = LevelCompatability
        };
    }

    // Token: 0x02000999 RID: 2457
    public enum PlayerState
    {
        // Token: 0x040038F2 RID: 14578
        Initializing,
        // Token: 0x040038F3 RID: 14579
        Initialized,
        // Token: 0x040038F4 RID: 14580
        LoadingLobbyScene,
        // Token: 0x040038F5 RID: 14581
        LoadedLobbyScene,
        // Token: 0x040038F6 RID: 14582
        SubmittedLobbyInfo,
        // New
        WaitingForCompatibilityStatus,
        // Token: 0x040038F7 RID: 14583
        LoadingGameModeScene,
        // Token: 0x040038F8 RID: 14584
        LoadedGameModeScene,
        // Token: 0x040038F9 RID: 14585
        SubmittedGameModeInfo,
        // Token: 0x040038FA RID: 14586
        StartedMode,
        // Token: 0x040038FB RID: 14587
        CantLoadLevelSoInLobby
    }
}
