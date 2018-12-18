using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#pragma warning disable 612,618  // disable warnings for obsolete things. we have to use those here.

public class DistanceServerMainStarter : MonoBehaviour {
    public static DistanceServerMainStarter Instance;

    void Awake()
    {
        Instance = this;
        LoadServerBaseExternal();
	}

    DistanceServerMainBase External = null;

    void LoadServerBaseExternal()
    {
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
            var file = new System.IO.FileInfo("DistanceServerBaseExternal.dll");
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
        External.Update();
    }

    void OnServerInitialized()
    {
        External.OnServerInitialized();
    }

    void OnPlayerConnected(NetworkPlayer player)
    {
        External.OnPlayerConnected(player);
    }

    void OnPlayerDisconnected(NetworkPlayer player)
    {
        External.OnPlayerDisconnected(player);
    }

    private void OnDestroy()
    {
        External.OnDestroy();
    }
	 
    [RPC]
    void ReceiveBroadcastAllEvent(byte[] bytes)
    {
        External.ReceiveBroadcastAllEvent(bytes);
    }

    [RPC]
    void ReceiveClientToServerEvent(byte[] bytes)
    {
        External.ReceiveClientToServerEvent(bytes);
    }

    [RPC]
    void ReceiveServerToClientEvent(byte[] bytes)
    {
        External.ReceiveServerToClientEvent(bytes);
    }

    [RPC]
    void ReceiveTargetedEventServerToClient(byte[] bytes)
    {
        External.ReceiveTargetedEventServerToClient(bytes);
    }

    [RPC]
    void ReceiveSerializeEvent(byte[] bytes)
    {
        External.ReceiveSerializeEvent(bytes);
    }

    [RPC]
    void ReceiveServerNetworkTimeSync(int serverNetworkTimeIntHigh, int serverNetworkTimeIntLow, NetworkMessageInfo info)
    {
        External.ReceiveServerNetworkTimeSync(serverNetworkTimeIntHigh, serverNetworkTimeIntLow, info);
    }

    [RPC]
    void SubmitServerNetworkTimeSync(NetworkMessageInfo info)
    {
        External.SubmitServerNetworkTimeSync(info);
    }
}