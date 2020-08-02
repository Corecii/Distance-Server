extern alias Distance;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#pragma warning disable 612,618  // disable warnings for obsolete things. we have to use those here.

public class DistanceServerMain : DistanceServerMainBase
{
    public static SemanticVersion ServerVersion = new SemanticVersion("0.2.0");
    public override int CompatibleStarterVersion => 1;

    public static NetworkView View;

    public static List<NetworkEvent> ClientToClientEvents = new List<NetworkEvent>
    {
        new Events.ClientToAllClients.HitTagStuntCollectible(),
        new Events.ClientToAllClients.ChatMessage(),
        new Events.ClientToAllClients.SetReady()
    };

    public static List<NetworkEvent> ClientToServerEvents = new List<NetworkEvent>
    {
        new Events.ClientToServer.HitTagBubble(),
        new Events.ClientToServer.SubmitPlayerData(),
        new Events.ClientToServer.CompletedRequest(),
        new Events.ClientToServer.SubmitLevelCompatabilityInfo(),
        new Events.ClientToServer.SubmitPlayerInfo()
    };

    public static List<NetworkEvent> ServerToClientEvents = new List<NetworkEvent>
    {
        new Events.ServerToClient.FinalCountdownCancel(),
        new Events.ServerToClient.ModeFinished(),
        new Events.ServerToClient.RemovePlayerFromClientList(),
        new Events.ServerToClient.UpdatePlayerLevelCompatibilityStatus(),
        new Events.ServerToClient.TimeWarning(),
        new Events.ServerToClient.InstantiatePrefab(),
        new Events.ServerToClient.InstantiatePrefabNoScale(),
        new Events.ServerToClient.SyncModeOptions(),
        new Events.ServerToClient.FinalCountdownActivate(),
        new Events.ServerToClient.SyncPlayerInfo(),
        new Events.ServerToClient.StuntBubbleStarted(),
        new Events.ServerToClient.StuntCollectibleSpawned(),
        new Events.ServerToClient.TaggedPlayer(),
        new Events.ServerToClient.ReverseTagFinished(),
        new Events.ServerToClient.SyncMode(),
        new Events.ServerToClient.GameModeFinished(),
        new Events.ServerToClient.SyncPlayerInfo(),
        new Events.ServerToClient.CreatePlayer(),
        new Events.ServerToClient.CreateExistingCar(),
        new Events.ServerToClient.SetLevelName(),
        new Events.ServerToClient.StartMode(),
        new Events.ServerToClient.RequestLevelCompatabilityInfo(),
        new Events.ServerToClient.Request(),
        new Events.ServerToClient.SetServerChat(),
        new Events.ServerToClient.SetGameMode(),
        new Events.ServerToClient.SetServerName(),
        new Events.ServerToClient.SetMaxPlayers(),
        new Events.ServerToClient.AddClient(),
    };
    public static List<NetworkEvent> InstancedEvents = new List<NetworkEvent> {
        new Events.Instanced.CarRespawn(),
        null,
        null,
        null,
        null,
        null,
        new Events.Instanced.Finished(),
        null,
        null,
        new Events.Instanced.Split(),
        null,
        new Events.Instanced.Death(),
        null,
        null,
        null,
        new Events.Instanced.Jump(),
        new Events.Instanced.WingsStateChange(),
        new Events.Instanced.TrickComplete(),
        new Events.Instanced.CheckpointHit(),
        new Events.Instanced.BrokeObject(),
        new Events.Instanced.ModeSpecial(),
        new Events.Instanced.Horn(),
        null,
        null,
        null,
        new Events.Instanced.PreTeleport(),
        new Events.Instanced.GravityToggled(),
        new Events.Instanced.Cooldown(),
        new Events.Instanced.WarpAnchorHit(),
        new Events.Instanced.DropperDroneStateChange(),
        new Events.Instanced.ShardClusterStateChange(),
        new Events.Instanced.ShardClusterFireShard(),
        null,
        null,
        null,
        null,
        null,
    };

    public static T GetEvent<T>() where T : NetworkEvent
    {
        foreach (var evt in ClientToClientEvents)
        {
            if (evt.GetType() == typeof(T))
            {
                return (T)evt;
            }
        }
        foreach (var evt in ClientToServerEvents)
        {
            if (evt.GetType() == typeof(T))
            {
                return (T)evt;
            }
        }
        foreach (var evt in ServerToClientEvents)
        {
            if (evt.GetType() == typeof(T))
            {
                return (T)evt;
            }
        }
        foreach (var evt in InstancedEvents)
        {
            if (evt != null && evt.GetType() == typeof(T))
            {
                return (T)evt;
            }
        }
        return null;
    }

    public static double UnixTime => (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
    public static double NetworkTime => Network.time;

    public static double NetworkTimeToUnixTime(double networkTime)
    {
        return networkTime + (UnixTime - NetworkTime);
    }

    public static double UnixTimeToNetworkTime(double unixTime)
    {
        return unixTime + (NetworkTime - UnixTime);
    }

    static Distance::BitStreamWriter bitStreamWriter = new Distance::BitStreamWriter();

    Distance::BitStreamReader bitStreamReader = new Distance::BitStreamReader(null);
    static Distance::BitStreamReader debugBitStreamReader = new Distance::BitStreamReader(null);

    public static void SendRPC(string rpcName, int eventIndex, UnityEngine.NetworkPlayer target, Distance::IBitSerializable data, NetworkView view)
    {
        bitStreamWriter.Clear();
        bitStreamWriter.Serialize(ref eventIndex);
        if (data != null)
        {
            data.Serialize(bitStreamWriter);
        }
        view.RPC(rpcName, target, new object[] { bitStreamWriter.ToBytes() });
    }

    public static void SendRPC(string rpcName, int eventIndex, UnityEngine.RPCMode target, Distance::IBitSerializable data, NetworkView view)
    {
        bitStreamWriter.Clear();
        bitStreamWriter.Serialize(ref eventIndex);
        if (data != null)
        {
            data.Serialize(bitStreamWriter);
        }
        view.RPC(rpcName, target, new object[] { bitStreamWriter.ToBytes() });
    }

    public static void SendRPC(string rpcName, int eventIndex, UnityEngine.NetworkPlayer target, Distance::IBitSerializable data)
    {
        SendRPC(rpcName, eventIndex, target, data, View);
    }

    public static void SendRPC(string rpcName, int eventIndex, UnityEngine.RPCMode target, Distance::IBitSerializable data)
    {
        SendRPC(rpcName, eventIndex, target, data, View);
    }

    public struct CommandLineArg
    {
        public string Name;
        public string[] Arguments;
        public CommandLineArg(string name, string[] arguments)
        {
            Name = name;
            Arguments = arguments;
        }
    }

    public CommandLineArg[] CommandLineArgs = new CommandLineArg[0];

    public System.IO.DirectoryInfo ServerDirectory;
    public System.IO.DirectoryInfo ExecutableDirectory;

    public DistanceServer Server = null;
    public static DistanceServerMain Instance;

    List<System.IO.DirectoryInfo> pluginDirs = new List<System.IO.DirectoryInfo>();
    List<System.IO.FileInfo> directPluginDirs = new List<System.IO.FileInfo>();

    public void ReadSettings()
    {
        var filePath = new System.IO.FileInfo(ServerDirectory.FullName + "/Server.json");
        if (!filePath.Exists)
        {
            Log.Info("No Server.json, using only commandline args");
            return;
        }
        try
        {
            var txt = System.IO.File.ReadAllText(filePath.FullName);
            var reader = new JsonFx.Json.JsonReader();
            var dictionary = (Dictionary<string, object>)reader.Read(txt);
            
            object[] pluginDirsList = new object[0];
            TryGetValue(dictionary, "PluginsDir", ref pluginDirsList);
            foreach (object dir in pluginDirsList)
            {
                if (dir is string)
                {
                    Log.Debug($"Adding plugin directory: {dir}");
                    pluginDirs.Add(new System.IO.DirectoryInfo((string)dir));
                }
                else
                {
                    Log.Info($"PluginsDir included value that was not a string: {dir}");
                }
            }

            object[] pluginsList = new object[0];
            TryGetValue(dictionary, "Plugins", ref pluginsList);
            foreach (object dir in pluginsList)
            {
                if (dir is string)
                {
                    Log.Debug($"Adding plugin: {dir}");
                    directPluginDirs.Add(new System.IO.FileInfo((string)dir));
                }
                else
                {
                    Log.Info($"Plugins included value that was not a string: {dir}");
                }
            }

            Log.Info("Loaded settings from Server.json");
        }
        catch (Exception e)
        {
            Log.Error($"Couldn't read Server.json. Is your json malformed?\n{e}");

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

    public override void Awake()
    {
        Debug.LogError("Using Error to force Unity log to show...");
        ExecutableDirectory = new System.IO.DirectoryInfo(UnityEngine.Application.dataPath).Parent;
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Debug.unityLogger.filterLogType = LogType.Log;
        ServerDirectory = ExecutableDirectory;
        var launchArgs = Environment.GetCommandLineArgs();
        if (launchArgs.Length > 0)
        {
            List<CommandLineArg> finalArgs = new List<CommandLineArg>();
            string name = "";
            List<string> args = new List<string>();
            for (int i = 0; i < launchArgs.Length; i++)
            {
                var lArg = launchArgs[i];
                if (lArg.Substring(0, 1) == "-")
                {
                    if (i != 0 || args.Count > 0)
                    {
                        finalArgs.Add(new CommandLineArg(name, args.ToArray()));
                    }
                    name = lArg;
                    args = new List<string>();
                }
                else
                {
                    args.Add(lArg);
                }
            }
            finalArgs.Add(new CommandLineArg(name, args.ToArray()));
            CommandLineArgs = finalArgs.ToArray();
        }

        Log.Info($"Target frame rate: {Application.targetFrameRate}");
        Application.targetFrameRate = 60;
        Log.Info($"New target frame rate: {Application.targetFrameRate}");

        foreach (var arg in CommandLineArgs)
        {
            if (arg.Name.ToLower() == "-serverdir" && arg.Arguments.Length > 0)
            {
                ServerDirectory = new System.IO.DirectoryInfo(arg.Arguments[0]);
                if (!ServerDirectory.Exists)
                {
                    Log.Error($"Server directory ({ServerDirectory}) does not exist! Not running server.");
                    return;
                }
            }
        }

        Log.Info($"Executable directory: {ExecutableDirectory}");
        Log.Info($"Server directory: {ServerDirectory}");

        ReadSettings();

        Instance = this;
        MasterServer.ipAddress = "54.213.90.85";
        MasterServer.port = 23466;
        Network.natFacilitatorIP = "54.213.90.85";
        Network.natFacilitatorPort = 50005;

        View = gameObject.GetComponent<NetworkView>();
        View.viewID = new NetworkViewID();

        Server = new DistanceServer();
        Server.Init();

        LoadPlugins();
        
        Log.WriteLine($"Starting server version {Server.DistanceVersion} on port {Server.Port} (UseNat: {Server.UseNat})");

        Network.InitializeServer(Server.MaxPlayers, Server.Port, Server.UseNat);
	}
    
    public List<DistanceServerPlugin> Plugins = new List<DistanceServerPlugin>();
    
    public T GetPlugin<T>() where T : DistanceServerPlugin
    {
        return (T)Plugins.Find(plugin => plugin is T);
    }

    void LoadPlugins()
    {
        var loadDefaultPlugins = true;
        var loadServerPlugins = true;
        var useLoadFrom = false;
        foreach (var arg in CommandLineArgs)
        {
            if (arg.Name.ToLower() == "-nodefaultplugins")
            {
                loadDefaultPlugins = false;
            }
            else if (arg.Name.ToLower() == "-noserverplugins")
            {
                loadServerPlugins = false;
            }
            else if (arg.Name.ToLower() == "-loadfrom")
            {
                useLoadFrom = true;
            }
        }

        if (loadDefaultPlugins)
        {
            pluginDirs.Add(new System.IO.DirectoryInfo(ExecutableDirectory.FullName + "/Plugins"));
        }

        if (loadServerPlugins && ServerDirectory != ExecutableDirectory)
        {
            pluginDirs.Add(new System.IO.DirectoryInfo(ServerDirectory.FullName + "/Plugins"));
        }

        foreach (var arg in CommandLineArgs)
        {
            if (arg.Name.ToLower() == "-pluginsdir")
            {
                foreach (var pluginDir in arg.Arguments)
                {
                    pluginDirs.Add(new System.IO.DirectoryInfo(pluginDir));
                }
            }
            else if (arg.Name.ToLower() == "-plugin")
            {
                foreach (var directPluginDir in arg.Arguments)
                {
                    directPluginDirs.Add(new System.IO.FileInfo(directPluginDir));
                }
            }
        }


        var pluginFileLists = new List<Tuple<string, System.IO.FileInfo[]>>();

        foreach (var pluginsPath in pluginDirs) {

            string failure = null;

            if (!pluginsPath.Exists)
            {
                failure = $"Plugins directory does not exist.";
            }
            else if ((pluginsPath.Attributes & System.IO.FileAttributes.Directory) != System.IO.FileAttributes.Directory)
            {
                failure = $"Plugins should be a directory but was a file.";
            }

            if (failure != null)
            {
                Log.Warn($"Not loading plugins at {pluginsPath.FullName} because: {failure}");
                continue;
            }

            pluginFileLists.Add(new Tuple<string, System.IO.FileInfo[]>(pluginsPath.FullName, pluginsPath.GetFiles()));
        }

        pluginFileLists.Add(new Tuple<string, System.IO.FileInfo[]>("Settings.json or CommandLineArgs", directPluginDirs.ToArray()));

        foreach(var dirInfo in pluginFileLists)
        {
            Log.Info($"Loading plugins at {dirInfo.Item1}");

            foreach (var file in dirInfo.Item2)
            {
                if (file.Extension == ".dll")
                {
                    Log.Info($"Attempting to load plugin file {file.Name}...");
                    var loadCount = 0;
                    try
                    {
                        Assembly loaded;
                        /*
                        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                        {
                            Log.Debug($"{args.RequestingAssembly} wants to load {args.Name}");
                            return null;
                        };
                        */
                        if (useLoadFrom)
                        {
                            loaded = Assembly.LoadFrom(file.FullName);
                        }
                        else
                        {
                            using (System.IO.Stream stream = System.IO.File.OpenRead(file.FullName))
                            {
                                byte[] rawAssembly = new byte[stream.Length];
                                stream.Read(rawAssembly, 0, (int)stream.Length);
                                var info = Assembly.ReflectionOnlyLoad(rawAssembly);
                                var alreadyLoaded = false;
                                foreach (var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
                                {
                                    if (loadedAssembly.FullName == info.FullName)
                                    {
                                        alreadyLoaded = true;
                                        break;
                                    }
                                }
                                if (alreadyLoaded)
                                {
                                    Log.Debug($"{file.Name} already loaded, skipping.");
                                    continue;
                                }
                                var symbolsPath = file.DirectoryName + "/" + file.Name.Substring(0, file.Name.Length - file.Extension.Length) + ".pdb";
                                if (new System.IO.FileInfo(symbolsPath).Exists)
                                {
                                    using (System.IO.Stream symStream = System.IO.File.OpenRead(symbolsPath))
                                    {
                                        byte[] rawSymbols = new byte[symStream.Length];
                                        symStream.Read(rawSymbols, 0, (int)symStream.Length);
                                        loaded = Assembly.Load(rawAssembly, rawSymbols);
                                        Log.Debug("Loaded assembly with debugging symbols");
                                    }
                                }
                                else
                                {
                                    loaded = Assembly.Load(rawAssembly);
                                }
                            }
                        }
                        foreach (var type in loaded.GetExportedTypes())
                        {
                            if (type.IsSubclassOf(typeof(DistanceServerPlugin)) && !type.IsAbstract && type.GetConstructor(System.Type.EmptyTypes) != null)
                            {
                                Plugins.Add((DistanceServerPlugin)System.Activator.CreateInstance(type));
                                loadCount++;
                            }
                        }
                        Log.Info($"Loaded plugin file {file.Name}");
                    }
                    catch (Exception e)
                    {
                        for (int i = 0; i < loadCount; i++)
                        {
                            Plugins.RemoveAt(Plugins.Count - 1);
                        }
                        Log.Error($"Failed to load plugin file {file.Name} because: {e}");
                    }
                }
            }
        }

        Plugins.Sort((a, b) => a.Priority - b.Priority);

        foreach (var plugin in Plugins)
        {
            try
            {
                Log.Info($"Loading plugin {plugin.DisplayName} by {plugin.Author}...");
                if (plugin.ServerVersion != ServerVersion)
                {
                    Log.Warn($"Server version: {ServerVersion}; Plugin was made for {plugin.ServerVersion}");
                    if (plugin.ServerVersion > ServerVersion)
                    {
                        Log.Warn($"Plugin {plugin.DisplayName} was made for a newer version and will likely work incorrectly!");
                    }
                    else if (plugin.ServerVersion.forkCode != ServerVersion.forkCode)
                    {
                        Log.Warn($"Plugin {plugin.DisplayName} was made for a different version with possibly-breaking API changes and will likely work incorrectly!");
                    }
                    else if (plugin.ServerVersion.major < ServerVersion.major || (ServerVersion.major == 0 && plugin.ServerVersion.minor < ServerVersion.minor))
                    {
                        Log.Warn($"Plugin {plugin.DisplayName} was made for an older version with breaking API changes and will likely work incorrectly!");
                    }
                    else if (plugin.ServerVersion.minor < ServerVersion.minor)
                    {
                        Log.Warn($"Plugin {plugin.DisplayName} was made for an older version with non-breaking feature improvements and may work incorrectly");
                    }
                    else if (plugin.ServerVersion.patch < ServerVersion.patch)
                    {
                        Log.Warn($"Plugin {plugin.DisplayName} was made for an older version without small bug fixes and may work incorrectly");
                    }
                }
                plugin.Manager = this;
                plugin.Server = Server;
                plugin.Start();
                Log.Info($"Loaded plugin {plugin.DisplayName} by {plugin.Author}");
            }
            catch (Exception e)
            {
                Log.Error($"Error starting plugin {plugin.DisplayName}by {plugin.Author}: {e}");
            }
        }
    }

    public override void Update()
    {
        if (Server != null)
        {
            Server.Update();
        }
    }

    public override void OnServerInitialized()
    {
        Server.OnServerInitialized();
    }

    public override void OnPlayerConnected(NetworkPlayer player)
    {
        Log.WriteLine($"Player joined: {player.guid} from {player.ipAddress}");
        Server.OnPlayerConnected(player);
    }

    public override void OnPlayerDisconnected(NetworkPlayer player)
    {
        Log.WriteLine($"Player left: {player.guid} from {player.ipAddress}");
        Server.OnPlayerDisconnected(player);
    }

    public override void OnDestroy()
    {
        Server.OnDestroy();
    }

    static int debugCount = 0;
    public static void DebugBytes(string name, byte[] bytes, List<NetworkEvent> eventLookup)
    {
        var outputTxt = "";
        outputTxt += $"{debugCount} {name}\n";
        debugCount++;

        if (eventLookup == null)
        {
            outputTxt = "\tNo event lookup\n";
        } else {
            debugBitStreamReader.Encapsulate(bytes);
            int index = 0;
            debugBitStreamReader.Serialize(ref index);
            if (index < 0 || index >= eventLookup.Count)
            {
                outputTxt += $"\tReceived invalid event index: {index} out of {eventLookup.Count}\n";
            }
            else
            {
                outputTxt += eventLookup[index].GetDebugRPCString(debugBitStreamReader);
            }
        }

        Log.WriteLine(outputTxt);
    }

    void ReceiveRPC(byte[] bytes, List<NetworkEvent> eventLookup, NetworkMessageInfo info)
    {
        bitStreamReader.Encapsulate(bytes);
        int index = 0;
        bitStreamReader.Serialize(ref index);
        if (index < 0 || index >= eventLookup.Count)
        {
            Log.WriteLine($"Received invalid event index: {index} out of {eventLookup.Count}");
        }
        eventLookup[index].ReceiveRPC(bitStreamReader, info);
    }

    public override void ReceiveBroadcastAllEvent(byte[] bytes, NetworkMessageInfo info)
    {
        DebugBytes("ReceiveBroadcastAllEvent", bytes, ClientToClientEvents);
        ReceiveRPC(bytes, ClientToClientEvents, info);
    }

    public override void ReceiveClientToServerEvent(byte[] bytes, NetworkMessageInfo info)
    {
        DebugBytes("ReceiveClientToServerEvent", bytes, ClientToServerEvents);
        ReceiveRPC(bytes, ClientToServerEvents, info);
    }

    public override void ReceiveServerToClientEvent(byte[] bytes, NetworkMessageInfo info)
    {
        DebugBytes("ReceiveServerToClientEvent", bytes, ServerToClientEvents);
        ReceiveRPC(bytes, ServerToClientEvents, info);
    }

    public override void ReceiveTargetedEventServerToClient(byte[] bytes, NetworkMessageInfo info)
    {
        DebugBytes("ReceiveTargetedEventServerToClient", bytes, ServerToClientEvents);
        ReceiveRPC(bytes, ServerToClientEvents, info);
    }

    public override void ReceiveSerializeEvent(byte[] bytes, NetworkMessageInfo info)
    {
        DebugBytes("ReceiveSerializeEvent", bytes, null);
    }

    public override void ReceiveServerNetworkTimeSync(int serverNetworkTimeIntHigh, int serverNetworkTimeIntLow, NetworkMessageInfo info)
    {
        Log.WriteLine("ReceiveServerNetworkTimeSync");
    }

    public override void SubmitServerNetworkTimeSync(NetworkMessageInfo info)
    {
        Log.WriteLine("SubmitServerNetworkTimeSync");
        double time = Network.time;
        int num;
        int num2;
        Distance::ConvertEx.DoubleToIntX2(out num, out num2, time);
        View.RPC("ReceiveServerNetworkTimeSync", info.sender, new object[]
        {
            num,
            num2
        });
    }

    public override void OnFailedToConnectToMasterServer(NetworkConnectionError error)
    {
        Server.OnFailedToConnectToMasterServer(error);
    }

    public override void OnMasterServerEvent(MasterServerEvent evt)
    {
        Server.OnMasterServer(evt);
    }
}