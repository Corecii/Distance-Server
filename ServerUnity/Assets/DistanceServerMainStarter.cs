using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#pragma warning disable 612,618  // disable warnings for obsolete things. we have to use those here.

public class DistanceServerMainStarter : MonoBehaviour {
    public static DistanceServerMainStarter Instance;
    public int StarterVersion = 1;

    void Start()
    {
        var launchArgs = Environment.GetCommandLineArgs();
        for (var i = 0; i < launchArgs.Length; i++)
        {
            var arg = launchArgs[i];
            if (arg == "-masterserverworkaround")
            {
                Debug.Log($"Requesting Distance host list from default master server");

                MasterServer.ipAddress = "54.213.90.85";
                MasterServer.port = 23466;

                MasterServer.RequestHostList("Distance");
            }
        }

        StartCoroutine(LoadSoon());
    }

    IEnumerator LoadSoon()
    {
        yield return new WaitForSeconds(5);
        Instance = this;
        LoadServerBaseExternal();
    }
    
    void OnMasterServerEvent(MasterServerEvent msEvent)
    {
        External?.OnMasterServerEvent(msEvent);
    }

    DistanceServerMainBase External = null;

    void LoadServerBaseExternal()
    {
        Log.Info($"Starter version {StarterVersion}");
        Log.Info($"Attempting to load DistanceServerBaseExternal.dll...");
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
            var exeDir = new System.IO.DirectoryInfo(UnityEngine.Application.dataPath).Parent;
            var file = new System.IO.FileInfo(exeDir.FullName + "/DistanceServerBaseExternal.dll");
            using (System.IO.Stream stream = System.IO.File.OpenRead(file.FullName))
            {
                byte[] rawAssembly = new byte[stream.Length];
                stream.Read(rawAssembly, 0, (int)stream.Length);
                var info = Assembly.ReflectionOnlyLoad(rawAssembly);
                var symbolsPath = file.DirectoryName + "/" + file.Name.Substring(0, file.Name.Length - file.Extension.Length) + ".pdb";
                if (new System.IO.FileInfo(symbolsPath).Exists)
                {
                    using (System.IO.Stream symStream = System.IO.File.OpenRead(symbolsPath))
                    {
                        byte[] rawSymbols = new byte[symStream.Length];
                        symStream.Read(rawSymbols, 0, (int)symStream.Length);
                        loaded = Assembly.Load(rawAssembly, rawSymbols);
                        Log.Debug("Loaded DistanceServerBaseExternal.dll with debugging symbols");
                    }
                }
                else
                {
                    loaded = Assembly.Load(rawAssembly);
                }
            }
            var serverType = loaded.GetType("DistanceServerMain", true);
            External = (DistanceServerMainBase)System.Activator.CreateInstance(serverType);
            Log.Info($"Loaded plugin DistanceServerBaseExternal.dll");
            if (External.CompatibleStarterVersion != StarterVersion)
            {
                Log.Warn($"DistanceServerBaseExternal is made for {(External.CompatibleStarterVersion > StarterVersion ? "a newer" : "an older")} version of the starter ({External.CompatibleStarterVersion}), errors may occur.");
            }
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load DistanceServerBaseExternal.dll because: {e}");
            Log.Error("Abandoning server start because DistanceServerBaseExternal.dll is required, but cannot be loaded!");
        }
        if (External != null) {
            External.Awake();
        }
    }

    private void Update()
    {
        External?.Update();
    }

    void OnServerInitialized()
    {
        External?.OnServerInitialized();
    }

    void OnPlayerConnected(NetworkPlayer player)
    {
        External?.OnPlayerConnected(player);
    }

    void OnPlayerDisconnected(NetworkPlayer player)
    {
        External?.OnPlayerDisconnected(player);
    }

    private void OnDestroy()
    {
        External?.OnDestroy();
    }

    void OnFailedToConnectToMasterServer(NetworkConnectionError error)
    {
        External?.OnFailedToConnectToMasterServer(error);
    }

    [RPC]
    void ReceiveBroadcastAllEvent(byte[] bytes, NetworkMessageInfo info)
    {
        External?.ReceiveBroadcastAllEvent(bytes, info);
    }

    [RPC]
    void ReceiveClientToServerEvent(byte[] bytes, NetworkMessageInfo info)
    {
        External?.ReceiveClientToServerEvent(bytes, info);
    }

    [RPC]
    void ReceiveServerToClientEvent(byte[] bytes, NetworkMessageInfo info)
    {
        External?.ReceiveServerToClientEvent(bytes, info);
    }

    [RPC]
    void ReceiveTargetedEventServerToClient(byte[] bytes, NetworkMessageInfo info)
    {
        External?.ReceiveTargetedEventServerToClient(bytes, info);
    }

    [RPC]
    void ReceiveSerializeEvent(byte[] bytes, NetworkMessageInfo info)
    {
        External?.ReceiveSerializeEvent(bytes, info);
    }

    [RPC]
    void ReceiveServerNetworkTimeSync(int serverNetworkTimeIntHigh, int serverNetworkTimeIntLow, NetworkMessageInfo info)
    {
        External?.ReceiveServerNetworkTimeSync(serverNetworkTimeIntHigh, serverNetworkTimeIntLow, info);
    }

    [RPC]
    void SubmitServerNetworkTimeSync(NetworkMessageInfo info)
    {
        External?.SubmitServerNetworkTimeSync(info);
    }
}