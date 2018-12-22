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
    public bool Stuck = false;
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
            /* Proper version calculation is not implemented yet
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
    
    public enum PlayerState
    {
        Initializing,
        Initialized,
        LoadingLobbyScene,
        LoadedLobbyScene,
        SubmittedLobbyInfo,
        WaitingForCompatibilityStatus,
        LoadingGameModeScene,
        LoadedGameModeScene,
        SubmittedGameModeInfo,
        StartedMode,
        CantLoadLevelSoInLobby
    }
}
