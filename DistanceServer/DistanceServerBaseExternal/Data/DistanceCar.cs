extern alias Distance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class DistanceCar : DistanceCarBase, IExternalData
{

    public DistancePlayer Player;

    public NetworkViewID PlayerViewId;
    public NetworkViewID CarViewId1;
    public NetworkViewID CarViewId2;

    public Distance::CarDirectives CarDirectives;
    public Rigidbody Rigidbody;
    public RigidbodyStateTransceiver RigidbodyStateTransceiver;
    public CarStateTransceiver CarStateTransceiver;

    public Distance::CarColors CarColors;
    public string PlayerName;
    public string CarName;

    public int Points = 0;
    public bool Finished = false;
    public int FinishData = -1;
    public Distance::FinishType FinishType = Distance::FinishType.Normal;
    public bool Spectator = false;

    public bool WingsOpen = false;
    public bool Alive = true;

    public List<object> ExternalData = new List<object>();
    public T GetExternalData<T>()
    {
        return (T)ExternalData.Find(val => val is T);
    }
    public void AddExternalData(object val)
    {
        ExternalData.Add(val);
    }

    public GameObject Networker;
    public NetworkView PlayerNetworkView;
    public NetworkView CarNetworkView1;
    public NetworkView CarNetworkView2;
    public List<NetworkEvent> InstancedEvents = new List<NetworkEvent> {
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

    public Transform Transform
    {
        get
        {
            return this.Networker?.transform;
        }
    }

    public T GetEvent<T>() where T : NetworkEvent
    {
        foreach (var evt in InstancedEvents)
        {
            if (evt != null && evt.GetType() == typeof(T))
            {
                return (T)evt;
            }
        }
        return null;
    }

    public DistanceCar(DistancePlayer player, Distance::PlayerInitializeData data)
    {
        Player = player;
        PlayerViewId = data.playerViewID_;
        CarViewId1 = data.carViewID0_;
        CarViewId2 = data.carViewID1_;
        CarColors = data.carColors_;
        PlayerName = data.playerName_;
        CarName = data.carName_;

        var index = 0;
        foreach (var evt in InstancedEvents)
        {
            if (evt != null)
            {
                var instancedEvt = (InstancedNetworkEventNonGeneric)evt;
                instancedEvt.eventIndex = index;
                instancedEvt.instance = this;
                instancedEvt.NonGenericWith((InstancedNetworkEventNonGeneric)DistanceServerMain.InstancedEvents[index]);
            }
            index++;
        }

        GetEvent<Events.Instanced.CarRespawn>().Connect(evtData =>
        {
            Transform.position = evtData.position_;
            Transform.rotation = evtData.rotation_;
            RigidbodyStateTransceiver.Clear();
        });
        GetEvent<Events.Instanced.PreTeleport>().Connect(evtData =>
        {
            RigidbodyStateTransceiver.Clear();
        });
        GetEvent<Events.Instanced.WingsStateChange>().Connect(evtData =>
        {
            WingsOpen = evtData.open_;
        });
        GetEvent<Events.Instanced.Death>().Connect(evtData =>
        {
            Alive = false;
        });
        GetEvent<Events.Instanced.CarRespawn>().Connect(evtData =>
        {
            Alive = true;
        });
        GetEvent<Events.Instanced.Finished>().Connect(evtData =>
        {
            Finished = true;
            FinishType = evtData.finishType_;
            FinishData = evtData.finishData_;
        });
    }

    public void BroadcastDNF()
    {
        DistanceServerMain.GetEvent<Events.ServerToClient.GameModeFinished>().Fire(
            Player.UnityPlayer,
            new Distance::Events.GameMode.Finished.Data()
        );
    }

    public void MakeNetworker()
    {
        RemoveNetworker();
        var networker = GameObject.Instantiate(Resources.Load("DistanceCarNetworker") as GameObject);
        networker.GetComponent<DistanceCarNetworkerBridge>().Car = this;
        Networker = networker;
        var networkViews = networker.GetComponents<NetworkView>();
        foreach(var view in networkViews)
        {
            Distance::NetworkViewEx.SetGroup(view, Distance::NetworkGroup.GameModeGroup);
        }
        PlayerNetworkView = networkViews.First(networkView => networkView.observed == null);
        CarNetworkView1 = networkViews.First(networkView => networkView.observed is NetworkStateTransceiverInternal && ((NetworkStateTransceiverInternal)networkView.observed).TransceiverId == "RigidBodyStateTransceiver");
        CarNetworkView2 = networkViews.First(networkView => networkView.observed is NetworkStateTransceiverInternal && ((NetworkStateTransceiverInternal)networkView.observed).TransceiverId == "CarStateTransceiver");

        PlayerNetworkView.viewID = PlayerViewId;
        CarNetworkView1.viewID = CarViewId1;
        CarNetworkView2.viewID = CarViewId2;

        RigidbodyStateTransceiver = new RigidbodyStateTransceiver();
        ((NetworkStateTransceiverInternal)CarNetworkView1.observed).External = RigidbodyStateTransceiver;
        CarStateTransceiver = new CarStateTransceiver();
        ((NetworkStateTransceiverInternal)CarNetworkView2.observed).External = CarStateTransceiver;

        foreach (var evt in InstancedEvents)
        {
            if (evt != null)
            {
                var instancedEvt = (InstancedNetworkEventNonGeneric)evt;
                instancedEvt.networkView = PlayerNetworkView;
            }
        }

        Rigidbody = networker.GetComponent<Rigidbody>();
        CarDirectives = CarStateTransceiver.CarLogicBridge.CarDirectives_;

        Log.WriteLine("Created DistanceCarNetworker");

    }

    public void RemoveNetworker()
    {
        if (Networker != null)
        {
            UnityEngine.Object.Destroy(Networker);
            Networker = null;
            PlayerNetworkView = null;
            CarNetworkView1 = null;
            CarNetworkView2 = null;

            foreach (var evt in InstancedEvents)
            {
                if (evt != null)
                {
                    var instancedEvt = (InstancedNetworkEventNonGeneric)evt;
                    instancedEvt.networkView = null;
                }
            }
        }
    }

    public Distance::PlayerInitializeData GetPlayerInitializeData()
    {
        return new Distance::PlayerInitializeData(PlayerViewId, CarViewId1, CarViewId2, CarColors, PlayerName, CarName);
    }

    public Distance::PlayerLateInitializeData GetPlayerLateInitializeData()
    {
        return new Distance::PlayerLateInitializeData(Points, Finished, FinishData, FinishType, Spectator);
    }

    Distance::BitStreamReader bitStreamReader = new Distance::BitStreamReader(null);

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
    
    public override void ReceiveSerializeEvent(byte[] bytes, NetworkMessageInfo info)
    {
        DistanceServerMain.DebugBytes("ReceiveSerializeEvent", bytes, InstancedEvents);
        ReceiveRPC(bytes, InstancedEvents, info);
    }

}
