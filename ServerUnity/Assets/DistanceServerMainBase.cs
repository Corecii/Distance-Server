﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public abstract class DistanceServerMainBase
{
    public GameObject gameObject
    {
        get
        {
            return DistanceServerMainStarter.Instance.gameObject;
        }
    }

    public abstract int CompatibleStarterVersion { get; }

    public abstract void Awake();
    public abstract void Update();
    public abstract void OnServerInitialized();
    public abstract void OnPlayerConnected(NetworkPlayer player);
    public abstract void OnPlayerDisconnected(NetworkPlayer player);
    public abstract void OnFailedToConnectToMasterServer(NetworkConnectionError error);
    public abstract void OnMasterServerEvent(MasterServerEvent evt);
    public abstract void OnDestroy();
    public abstract void ReceiveBroadcastAllEvent(byte[] bytes, NetworkMessageInfo info);
    public abstract void ReceiveClientToServerEvent(byte[] bytes, NetworkMessageInfo info);
    public abstract void ReceiveServerToClientEvent(byte[] bytes, NetworkMessageInfo info);
    public abstract void ReceiveTargetedEventServerToClient(byte[] bytes, NetworkMessageInfo info);
    public abstract void ReceiveSerializeEvent(byte[] bytes, NetworkMessageInfo info);
    public abstract void ReceiveServerNetworkTimeSync(int serverNetworkTimeIntHigh, int serverNetworkTimeIntLow, NetworkMessageInfo info);
    public abstract void SubmitServerNetworkTimeSync(NetworkMessageInfo info);
}
