extern alias Distance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Events;

public class DistanceServer
{
    string serverName = "Server";
    public string ServerName
    {
        get
        {
            return serverName;
        }
        set
        {
            if (serverName != value)
            {
                serverName = value;
                DoReportToMasterServer();
            }
        }
    }

    int maxPlayers = 1;
    public int MaxPlayers
    {
        get
        {
            return maxPlayers;
        }
        set
        {
            maxPlayers = value;
            Network.maxConnections = maxPlayers;
        }
    }
    public int Port = 45671;

    bool reportToMasterServer = true;
    public bool ReportToMasterServer
    {
        get
        {
            return reportToMasterServer;
        }
        set
        {
            if (reportToMasterServer == value)
            {
                return;
            }
            reportToMasterServer = value;
            if (Network.isServer)
            {
                if (reportToMasterServer)
                {
                    DoReportToMasterServer();
                }
                else
                {
                    MasterServer.UnregisterHost();
                }
            }
        }
    }
    string masterServerGameModeOverride = null;
    public string MasterServerGameModeOverride
    {
        get
        {
            return masterServerGameModeOverride;
        }
        set
        {
            if (masterServerGameModeOverride != value)
            {
                masterServerGameModeOverride = value;
                DoReportToMasterServer();
            }
        }
    }

    int distanceVersion = 6703;
    public int DistanceVersion
    {
        get
        {
            return distanceVersion;
        }
        set
        {
            if (distanceVersion != value)
            {
                distanceVersion = value;
                DoReportToMasterServer();
            }
        }
    }

    double modeTimeOffset = 0;
    public double ModeTimeOffset
    {
        get
        {
            return modeTimeOffset;
        }
        set
        {
            modeTimeOffset = value;
            foreach (var player in DistancePlayers.Values)
            {
                if (player.Car != null)
                {
                    player.Car.RigidbodyStateTransceiver.Clear();
                    player.Car.CarStateTransceiver.Clear();
                }
            } 
        }
    }
    public double ModeTime = 0;

    public DistanceLevel currentLevel = new DistanceLevel()
    {
        Name = "Test Server",
        RelativeLevelPath = "OfficialLevels/broken symmetry.bytes",
        WorkshopFileId = "",
        GameMode = "Sprint"
    };
    public bool IsCustomPlaylist = false;
    public bool HideLevelName = false;

    public int CurrentLevelId { get; private set; } = 0;
    public DistanceLevel CurrentLevel
    {
        get { return currentLevel; }
        set {
            CurrentLevelId++;
            currentLevel = value;
            if (MasterServerGameModeOverride == null)
            {
                DoReportToMasterServer();
            }
        }
    }

    public bool IsInLobby = true;

    public bool HasModeStarted = false;
    public double ModeStartTime = -1.0;

    public List<DistanceChat> ChatLog = new List<DistanceChat>();
    public void AddChatMessage(string message)
    {
        foreach (string line in message.Split(new char[] { '\n', '\r'}, StringSplitOptions.RemoveEmptyEntries))
        {
            ChatLog.Add(new DistanceChat(line));
            if (ChatLog.Count > 64)
            {
                ChatLog.RemoveAt(0);
            }
        }
    }
    public void SayChatMessage(bool addMessage, string message)
    {
        if (addMessage)
        {
            AddChatMessage(message);
        }
        if (Network.isServer)
        {
            DistanceServerMain.GetEvent<Events.ClientToAllClients.ChatMessage>().Fire(
                RPCMode.Others,
                new Distance::Events.ClientToAllClients.ChatMessage.Data(message)
            );
        }
    }
    public void SayLocalChatMessage(NetworkPlayer player, string message)
    {
        if (Network.isServer)
        {
            DistanceServerMain.GetEvent<Events.ClientToAllClients.ChatMessage>().Fire(
                player,
                new Distance::Events.ClientToAllClients.ChatMessage.Data(message)
            );
        }
    }

    public Dictionary<string, DistancePlayer> DistancePlayers = new Dictionary<string, DistancePlayer>();
    public List<DistancePlayer> ValidPlayers = new List<DistancePlayer>();

    public DistancePlayer GetDistancePlayer(NetworkPlayer player)
    {
        return DistancePlayers.ContainsKey(player.guid) ? DistancePlayers[player.guid] : null;
    }

    public DistancePlayer GetDistancePlayer(string guid)
    {
        return DistancePlayers.ContainsKey(guid) ? DistancePlayers[guid] : null;
    }

    public DistancePlayer GetDistancePlayer(int clientId)
    {
        return ValidPlayers.Count > clientId ? ValidPlayers[clientId] : null;
    }
    
    public LocalEvent<DistancePlayer> OnPlayerValidatedEvent = new LocalEvent<DistancePlayer>();
    public LocalEvent<DistancePlayer> OnPlayerValidatedPreReplicationEvent = new LocalEvent<DistancePlayer>();

    ///

    public void Init()
    {
        DistanceServerMain.GetEvent<Events.ClientToServer.SubmitPlayerInfo>().Connect(data =>
        {
            var player = GetDistancePlayer(data.sender_);
            player.Name = data.playerName_;
            player.ReceivedInfo = true;
            player.Index = ValidPlayers.Count;
            player.ValidatedAt = DistanceServerMain.UnixTime;
            ValidPlayers.Add(player);
            player.OnValidatedPreReplicationEvent.Fire();
            OnPlayerValidatedPreReplicationEvent.Fire(player);
            DistanceServerMain.GetEvent<Events.ServerToClient.AddClient>().Fire(
                RPCMode.Others,
                player.GetAddClientData(true)
            );
            player.OnValidatedEvent.Fire();
            OnPlayerValidatedEvent.Fire(player);
        });
        DistanceServerMain.GetEvent<Events.ClientToServer.CompletedRequest>().Connect(data =>
        {
            var distancePlayer = GetDistancePlayer(data.networkPlayer_);
            if (data.request_ == Distance::ServerRequest.SubmitClientInfo)  // TODO: check if request was actually completed for security
            {
                distancePlayer.State = DistancePlayer.PlayerState.Initialized;
                if (IsInLobby)
                {
                    SendPlayerToLobby(data.networkPlayer_);
                }
                else
                {
                    SendPlayerToLevel(data.networkPlayer_);
                }
            }
            else if (data.request_ == Distance::ServerRequest.LoadLobbyScene)
            {
                distancePlayer.State = DistancePlayer.PlayerState.LoadedLobbyScene;
                SendLobbyServerInfo(data.networkPlayer_);
                SendLevelInfo(data.networkPlayer_);
                RequestLevelCompatibilityInfo(data.networkPlayer_);
                RequestSubmitLobbyInfo(data.networkPlayer_);
                if (!IsInLobby)
                {
                    distancePlayer.State = DistancePlayer.PlayerState.CantLoadLevelSoInLobby;
                }
            }
            else if (data.request_ == Distance::ServerRequest.SubmitLobbyInfo)
            {
                distancePlayer.State = DistancePlayer.PlayerState.SubmittedLobbyInfo;
            }
            else if (data.request_ == Distance::ServerRequest.LoadGameModeScene)
            {
                distancePlayer.State = DistancePlayer.PlayerState.LoadedGameModeScene;
                if (distancePlayer.LevelId != CurrentLevelId)
                {
                    SendPlayerToLevel(distancePlayer.UnityPlayer);
                    return;
                }
                // fire CreatePlayer for sender for all existing cars (see WelcomeClientToGameMode.Data)
                foreach (var player in ValidPlayers)
                {
                    if (player.Car != null)
                    {
                        DistanceServerMain.GetEvent<Events.ServerToClient.CreatePlayer>().Fire(
                            data.networkPlayer_,
                            new Distance::Events.ServerToClient.CreatePlayer.Data(player.Car.GetPlayerInitializeData(), player.Car.GetPlayerLateInitializeData(), player.Index)
                        );
                        if (!player.Car.Finished && player.Car.Alive)
                        {
                            var gameObject = player.Car.Networker;
                            var transform = gameObject.transform;
                            DistanceServerMain.GetEvent<Events.ServerToClient.CreateExistingCar>().Fire(
                                data.networkPlayer_,
                                new Distance::Events.ServerToClient.CreateExistingCar.Data(transform.position, transform.rotation, player.Car.WingsOpen, player.Index)
                            );
                        }
                    }
                }
                if (HasModeStarted)
                {
                    // TODO: sync game mode things if already started -- different for every game mode (see SyncLateClientToGameMode.Data and SyncMode.Data)
                    // NOTE: this should be handled entirely by the game mode programming, not by the base server programming
                }
                RequestSubmitGameModeInfo(data.networkPlayer_);
            }
            else if (data.request_ == Distance::ServerRequest.SubmitGameModeInfo)
            {
                distancePlayer.State = DistancePlayer.PlayerState.SubmittedGameModeInfo;
                // If mode has not started, try to start it (check if all players have submitted info/loaded)
                // If mode has started, set state to StartedMode and fire StartClientLate
                // (see ServerLogic.OnEventCompletedRequest)
                if (StartingMode)
                {
                    AttemptToStartMode();
                }
                else
                {
                    distancePlayer.State = DistancePlayer.PlayerState.StartedMode;
                    DistanceServerMain.GetEvent<Events.ServerToClient.StartMode>().Fire(
                        data.networkPlayer_,
                        new Distance::Events.ServerToClient.StartMode.Data(ModeStartTime, true)
                    );
                }
            }
        });
        DistanceServerMain.GetEvent<Events.ClientToAllClients.ChatMessage>().Connect(data =>
        {
            AddChatMessage(data.message_);
        });
        DistanceServerMain.GetEvent<Events.ClientToAllClients.SetReady>().Connect(data =>
        {
            GetDistancePlayer(data.player_).Ready = data.ready_;
        });
        DistanceServerMain.GetEvent<Events.ClientToServer.SubmitLevelCompatabilityInfo>().Connect(data =>
        {
            var distancePlayer = GetDistancePlayer(data.player_);
            distancePlayer.LevelCompatibilityInfo = new LevelCompatibilityInfo(data);
            Log.Debug($"Level compatibility versions test: {data.levelVersion_} : {CurrentLevel.LevelVersion}");
            // TODO: write proper level compat check code (the current computed version is incorrect, so version checking is ignored)
            DistanceServerMain.GetEvent<Events.ServerToClient.UpdatePlayerLevelCompatibilityStatus>().Fire(
                data.player_,
                new Distance::Events.ServerToClient.UpdatePlayerLevelCompatibilityStatus.Data(data.player_, distancePlayer.LevelCompatability)
            );
            if (distancePlayer.State == DistancePlayer.PlayerState.WaitingForCompatibilityStatus)
            {
                SendPlayerToLevel(distancePlayer.UnityPlayer);
            }
        });
        DistanceServerMain.GetEvent<Events.ClientToServer.SubmitPlayerData>().Connect(data =>
        {
            var distancePlayer = GetDistancePlayer(data.clientPlayerIndex_);
            distancePlayer.Car = new DistanceCar(distancePlayer, data.data_);
            distancePlayer.Car.MakeNetworker();
            DistanceServerMain.GetEvent<Events.ServerToClient.CreatePlayer>().Fire(
                RPCMode.Others,
                new Distance::Events.ServerToClient.CreatePlayer.Data(distancePlayer.Car.GetPlayerInitializeData(), distancePlayer.Car.GetPlayerLateInitializeData(), distancePlayer.Index)
            );
        });
    }

    public LocalEventEmpty OnUpdateEvent = new LocalEventEmpty();
    public void Update()
    {
        ModeTime = Network.time + ModeTimeOffset;
        PrivateUtilities.setPrivateProperty(typeof(Distance::Timex), null, "ModeTime_", ModeTime);
        OnUpdateEvent.Fire();
        if (StartingLevel)
        {
            AttemptToStartLevel();
        }
    }

    public LocalEventEmpty OnServerInitializedEvent = new LocalEventEmpty();
    public void OnServerInitialized()
    {
        Log.WriteLine("Started server");
        DoReportToMasterServer();
        OnServerInitializedEvent.Fire();
    }

    public void RequestSubmitLobbyInfo(NetworkPlayer player)
    {
        DistanceServerMain.GetEvent<Events.ServerToClient.Request>().Fire(
            player,
            new Distance::Events.ServerToClient.Request.Data(Distance::ServerRequest.SubmitLobbyInfo)
        );
    }

    public void RequestSubmitGameModeInfo(NetworkPlayer player)
    {
        DistanceServerMain.GetEvent<Events.ServerToClient.Request>().Fire(
            player,
            new Distance::Events.ServerToClient.Request.Data(Distance::ServerRequest.SubmitGameModeInfo)
        );
    }

    public bool UpdateLevelCompatabilityStatus(DistancePlayer player)
    {
        if (player.LevelCompatibilityInfo.LevelCompatibilityId != CurrentLevelId || player.LevelCompatability == Distance::LevelCompatabilityStatus.Unverified)
        {
            RequestLevelCompatibilityInfo(player.UnityPlayer);
            return false;
        }
        return true;
    }

    public void RequestLevelCompatibilityInfo(NetworkPlayer player)
    {
        DistanceServerMain.GetEvent<Events.ServerToClient.UpdatePlayerLevelCompatibilityStatus>().Fire(
            player,
            new Distance::Events.ServerToClient.UpdatePlayerLevelCompatibilityStatus.Data(player, Distance::LevelCompatabilityStatus.Unverified)
        );
        DistanceServerMain.GetEvent<Events.ServerToClient.RequestLevelCompatabilityInfo>().Fire(
            player,
            new Distance::Events.ServerToClient.RequestLevelCompatabilityInfo.Data()
            {
                gameMode_ = CurrentLevel.GameMode,
                levelCompatInfoID_ = CurrentLevelId,
                levelName_ = CurrentLevel.Name,
                relativeLevelPath_ = CurrentLevel.RelativeLevelPath
            }
        );
    }

    public void SendLobbyServerInfo(NetworkPlayer player)
    {
        DistanceServerMain.GetEvent<Events.ServerToClient.SetServerName>().Fire(
            player,
            new Distance::Events.ServerToClient.SetServerName.Data(ServerName)
        );
        DistanceServerMain.GetEvent<Events.ServerToClient.SetMaxPlayers>().Fire(
            player,
            new Distance::Events.ServerToClient.SetMaxPlayers.Data(MaxPlayers)
        );
    }

    public void SendLevelInfo(NetworkPlayer player)
    {
        DistanceServerMain.GetEvent<Events.ServerToClient.SetLevelName>().Fire(
            player,
            CurrentLevel.GetLevelNameData(HideLevelName, IsCustomPlaylist)
        );
        DistanceServerMain.GetEvent<Events.ServerToClient.SetGameMode>().Fire(
            player,
            CurrentLevel.GetGameModeData()
        );
    }

    public void SendPlayerToLobby(NetworkPlayer player)
    {
        var distancePlayer = GetDistancePlayer(player);
        distancePlayer.State = DistancePlayer.PlayerState.LoadingLobbyScene;
        DistanceServerMain.GetEvent<Events.ServerToClient.Request>().Fire(
            player,
            new Distance::Events.ServerToClient.Request.Data(Distance::ServerRequest.LoadLobbyScene)
        );
    }

    public void SendAllPlayersToLobby()
    {
        foreach (var player in ValidPlayers)
        {
            SendPlayerToLobby(player.UnityPlayer);
        }
    }

    public LocalEventEmpty OnLobbyStartedEvent = new LocalEventEmpty();
    public void StartLobby()
    {
        ModeTimeOffset = 0;
        IsInLobby = true;
        foreach (var player in ValidPlayers)
        {
            player.Car = null;
        }
        SendAllPlayersToLobby();
        OnLobbyStartedEvent.Fire();
    }

    public void SendPlayerToLevel(NetworkPlayer player)
    {

        SendLevelInfo(player);
        var distancePlayer = GetDistancePlayer(player);
        if (!UpdateLevelCompatabilityStatus(distancePlayer))
        {
            distancePlayer.State = DistancePlayer.PlayerState.WaitingForCompatibilityStatus;
            return;
        }
        distancePlayer.State = DistancePlayer.PlayerState.LoadingGameModeScene;
        distancePlayer.Level = CurrentLevel;
        distancePlayer.LevelId = CurrentLevelId;
        DistanceServerMain.GetEvent<Events.ServerToClient.Request>().Fire(
            player,
            new Distance::Events.ServerToClient.Request.Data(Distance::ServerRequest.LoadGameModeScene)
        );
    }

    public void SendAllPlayersToLevel()
    {
        foreach (var player in ValidPlayers)
        {
            SendPlayerToLevel(player.UnityPlayer);
        }
    }

    public LocalEventEmpty OnLevelStartInitiatedEvent = new LocalEventEmpty();
    public LocalEventEmpty OnLevelStartedEvent = new LocalEventEmpty();

    public bool StartingLevel = false;
    public double StartingLevelTime = -1.0;
    public double StartingLevelTimeout = 30.0;
    public bool StartLevel()
    {
        StartingLevel = true;
        StartingLevelTime = Network.time;
        OnLevelStartInitiatedEvent.Fire();
        return AttemptToStartLevel();
    }

    public bool AttemptToStartLevel()
    {
        if (StartingLevelTime == -1.0 || Network.time - StartingLevelTime < StartingLevelTimeout)
        {
            foreach (var player in ValidPlayers)
            {
                if (player.State == DistancePlayer.PlayerState.LoadingGameModeScene)
                {
                    return false;
                }
            }
        }
        StartingLevel = false;
        IsInLobby = false;
        HasModeStarted = false;
        foreach (var player in ValidPlayers)
        {
            player.Car = null;
        }
        StartingMode = true;
        SendAllPlayersToLevel();
        OnLevelStartedEvent.Fire();
        if (ValidPlayers.Count == 0)
        {
            AttemptToStartMode();
        }
        return true;
    }

    public bool StartingMode = false;
    public LocalEventEmpty OnModeStartedEvent = new LocalEventEmpty();
    public void AttemptToStartMode()
    {
        foreach (var player in ValidPlayers)
        {
            if (player.State != DistancePlayer.PlayerState.SubmittedGameModeInfo && player.State != DistancePlayer.PlayerState.CantLoadLevelSoInLobby)
            {
                return;
            }
        }
        foreach (var player in ValidPlayers)
        {
            player.State = DistancePlayer.PlayerState.StartedMode;
        }
        ModeStartTime = Network.time + 6.0;  // TODO: figure out proper time values
        ModeTimeOffset = -ModeStartTime;
        HasModeStarted = true;
        StartingMode = false;
        foreach (var player in ValidPlayers)
        {
            DistanceServerMain.GetEvent<Events.ServerToClient.StartMode>().Fire(
                player.UnityPlayer,
                new Distance::Events.ServerToClient.StartMode.Data(ModeStartTime, false)
            );
        }
        OnModeStartedEvent.Fire();
    }

    public LocalEvent<DistancePlayer> OnPlayerConnectedEvent = new LocalEvent<DistancePlayer>();
    public void OnPlayerConnected(NetworkPlayer player)
    {
        var distancePlayer = new DistancePlayer(this, player.guid);
        DistancePlayers[player.guid] = distancePlayer;

        foreach (var existingPlayer in ValidPlayers)
        {
            DistanceServerMain.GetEvent<Events.ServerToClient.AddClient>().Fire(
                player,
                existingPlayer.GetAddClientData(false)
            );
        }

        string chatString = "";
        foreach (var line in ChatLog)
        {
            chatString += "\n" + line.Chat;
        }
        if (chatString.Length > 0)
        {
            chatString = chatString.Substring(1);
        }
        DistanceServerMain.GetEvent<Events.ServerToClient.SetServerChat>().Fire(
            player,
            new Distance::Events.ServerToClient.SetServerChat.Data(chatString)
        );

        SendLevelInfo(player);

        DistanceServerMain.GetEvent<Events.ServerToClient.Request>().Fire(
            player,
            new Distance::Events.ServerToClient.Request.Data(Distance::ServerRequest.SubmitClientInfo)
        );

        distancePlayer.JoinedAt = DistanceServerMain.UnixTime;
        OnPlayerConnectedEvent.Fire(distancePlayer);
    }

    public LocalEvent<DistancePlayer> OnPlayerDisconnectedEvent = new LocalEvent<DistancePlayer>();
    public void OnPlayerDisconnected(NetworkPlayer player)
    {
        var distancePlayer = GetDistancePlayer(player);
        distancePlayer.unityPlayer = player;
        if (distancePlayer.ReceivedInfo) {
            ValidPlayers.Remove(distancePlayer);
            for (int i = distancePlayer.Index; i < ValidPlayers.Count; i++)
            {
                ValidPlayers[i].Index = i;
            }
        }
        DistancePlayers.Remove(player.guid);
        distancePlayer.Car = null;
        DistanceServerMain.GetEvent<Events.ServerToClient.RemovePlayerFromClientList>().Fire(
            RPCMode.Others,
            new Distance::Events.ServerToClient.RemovePlayerFromClientList.Data(player, distancePlayer.Index, Distance::DisconnectionType.Quit)
        );

        if (StartingMode)
        {
            AttemptToStartMode();
        }

        distancePlayer.LeftAt = DistanceServerMain.UnixTime;
        distancePlayer.OnDisconnectedEvent.Fire();
        OnPlayerDisconnectedEvent.Fire(distancePlayer);
    }

    public LocalEventEmpty OnDestroyEvent = new LocalEventEmpty();
    public void OnDestroy()
    {
        MasterServer.UnregisterHost();
    }

    public void DoReportToMasterServer()
    {
        if (!Network.isServer || !ReportToMasterServer)
        {
            return;
        }
        var gameMode = MasterServerGameModeOverride;
        if (gameMode == null)
        {
            gameMode = CurrentLevel.GameMode;
        }
        MasterServer.RegisterHost("Distance", this.ServerName, DistanceVersion.ToString("D" + 6) + gameMode);
    }
}
