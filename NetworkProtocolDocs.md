# Distance Network Protocol Docs

Distance builds its networking protocol on top of [Unity's deprecated Network View RPC](https://docs.unity3d.com/Manual/NetworkReferenceGuide.html)

This RPC system allows clients and servers to call special methods on each other's computers that are marked with the [RPC] class. Distance has 7 methods that have this label:

* `NetworkingManager.ReceiveServerNetworkTimeSync(int serverNetworkTimeIntHigh, int serverNetworkTimeIntLow, NetworkMessageInfo info)`
* `NetworkingManager.SubmitServerNetworkTimeSync(NetworkMessageInfo info)`
    * These two events handle synchronising the time between the server and client and are called as soon as the client joins. Submit is called by the client. Receive is called by the server in response.
* `ClientToClientNetworkTransciever.ReceiveBroadcastAllEvent(byte[] bytes)`
* `ClientToServerNetworkTransciever.ReceiveClientToServerEvent(byte[] bytes)`
* `ServerToClientNetworkTransciever.ReceiveServerToClientEvent(byte[] bytes)`
* `ServerToClientNetworkTransciever.ReceiveTargetedEventServerToClient(byte[] bytes)`
    * These four methods call the networked RPC events.
* `NetworkedInstancedEventTransciever.ReceiveSerializeEvent(byte[] bytes)`
    * This method calls into PlayerEvents

---

The four methods grouped together above handle most of the communication in the game. Each method fits into one of three categories:

* ClientToClient
    * `ClientToClientNetworkTransciever.ReceiveBroadcastAllEvent`
* ClientToServer
    * `ClientToServerNetworkTransciever.ReceiveClientToServerEvent`
* ServerToClient
    * `ServerToClientNetworkTransciever.ReceiveServerToClientEvent`
    * `ServerToClientNetworkTransciever.ReceiveTargetedEventServerToClient`

Each category has its own list of events that is generated on game startup. This list assigns each event a number.

* When an event is fired, the event number is serialized first, then followed by the event's data object.
* When an event is received, the event number is deserialized from the byte array. That number is used to find the event object. Then `ReceiveRPC` is called on the event object, which deserializes the event's data object from the byte array then fires the event.

---

`NetworkedInstancedEventTransciever.ReceiveSerializeEvent` is used for player events.

The `PlayerEvents` class creates a `NetworkedInstancedEventTransciever`.

`PlayerEvents` is a MonoBehavior that is included with some objects and referenced by many components. `PlayerEvents` seems to exist regardless of if it's connected to the server, but when connected to the server it will send and receive data.

`PlayerEvents` has a list of events that it uses in the compiled Distance DLL. In other words, the networked events for `PlayerEvents` is *not* generated at startup. Only some of the events on the list become networked events. The normal events are transformed into network events using `as ITranscievedEvent`, so some return null and don't become networked events.

These events are attached to objects using NetworkViewIDs. The NetworkViewIDs are set after creation according to values from the `CreatePlayer` event.

Instanced events use the first view id transferred in the `CreatePlayer` event, the player view id.

---

NetworkStateTransceivers

NetworkStateTransceivers have OnSerializeNetworkEvent called on every frame to update their data. These are used for car positions and car directives.

* RigidbodyStateTransceiver transfers data about the car's position, orientation, velocity, and angular velocity. It uses the second view id transferred in `CreatePlayer`.
* CarStateTransceiver transfers data about the car's controls. It hooks up to a CarLogic which hooks up to CarDirectives. It uses the third view id transferred in `CreatePlayer`.

NetworkStateTransceivers need their NetworkViews to be set to Unreliable mode (Synchronization mode 2) in order for them to work properly.

---

Implications

* Custom servers can be written by either...
    * Including Distance's code, generating the list like Distance does, and using the existing event Data objects.
    * *or* by generating a list of number-to-Data-object links and using reimplemented compatible Data structs.
* Modded clients and servers can send custom data by...
    * Initialization by either:
        * Finding an unused event and using it to signal clients or servers that the client or server is modded.
        * *or* by attaching extra data to the end of an already used event. Unmodded clients will discard the data, but modded clients will recognize it.
    * Once clients and servers can tell who is modded, they can establish the maximum protocol version, after which they can talk to each other using high-number events ids that will never be used by the game (e.g. `1000`). Harmony or other mods can be used to intercept and cancel the code that normally processes events if a modded network message is detected.
    * Ideally mod messages would use strings instead of ints to communicate what event to fire, as it's not feasible to keep ints synced up between server and client with lots of mods. Detect mod RPC -> get string event id -> hand off to mod to deserialize using its own data struct.

---

Static Events as of 2018-07-01:

* ClientToServerNetworkTransceiver
* ClientToServer ReceiveClientToServerEvent
    * 0: `Events.ReverseTag.HitTagBubble+Data`
    * 1: `Events.ClientToServer.SubmitPlayerData+Data`
    * 2: `Events.ClientToServer.CompletedRequest+Data`
    * 3: `Events.ClientToServer.SubmitLevelCompatabilityInfo+Data`
    * 4: `Events.ClientToServer.SubmitPlayerInfo+Data`
* ServerToClientNetworkTransceiver
* ServerToClient ReceiveServerToClientEvent ReceiveTargetedEventServerToClient
    * 0: `Events.RaceMode.FinalCountdownCancel+Data`
    * 1: `Events.ServerToClient.ModeFinished+Data`
    * 2: `Events.ServerToClient.RemovePlayerFromClientList+Data`
    * 3: `Events.ServerToClient.UpdatePlayerLevelCompatibilityStatus+Data`
    * 4: `Events.ServerToClient.TimeWarning+Data`
    * 5: `Events.ServerToClient.InstantiatePrefab+Data`
    * 6: `Events.ServerToClient.InstantiatePrefabNoScale+Data`
    * 7: `Events.SprintMode.SyncModeOptions+Data`
    * 8: `Events.RaceMode.FinalCountdownActivate+Data`
    * 9: `Events.RaceModeInfo.SyncPlayerInfo+Data`
    * 10: `Events.Stunt.StuntBubbleStarted+Data`
    * 11: `Events.Stunt.StuntCollectibleSpawned+Data`
    * 12: `Events.ReverseTag.TaggedPlayer+Data`
    * 13: `Events.ReverseTag.Finished+Data`
    * 14: `Events.GameMode.SyncMode+Data`
    * 15: `Events.GameMode.Finished+Data`
    * 16: `Events.ModePlayerInfo.SyncPlayerInfo+Data`
    * 17: `Events.ServerToClient.CreatePlayer+Data`
    * 18: `Events.ServerToClient.CreateExistingCar+Data`
    * 19: `Events.ServerToClient.SetLevelName+Data`
    * 20: `Events.ServerToClient.StartMode+Data`
    * 21: `Events.ServerToClient.RequestLevelCompatabilityInfo+Data`
    * 22: `Events.ServerToClient.Request+Data`
    * 23: `Events.ServerToClient.SetServerChat+Data`
    * 24: `Events.ServerToClient.SetGameMode+Data`
    * 25: `Events.ServerToClient.SetServerName+Data`
    * 26: `Events.ServerToClient.SetMaxPlayers+Data`
    * 27: `Events.ServerToClient.AddClient+Data`
* ClientToClientNetworkTransceiver
* ClientToClient ReceiveBroadcastAllEvent
    * 0: `Events.Stunt.HitTagStuntCollectible+Data`
    * 1: `Events.ClientToAllClients.ChatMessage+Data`
    * 2: `Events.ClientToAllClients.SetReady+Data`

Instanced Events as of 2018-07-01:

| Index | Local event | Networked event |
| ----: | :---------- | :-------------- |
| 0 | Events.Player.CarRespawn                           | Events.Player.CarRespawn                        |
| 1 | Events.Player.CarFailedToRespawn                                                                     ||
| 2 | Events.Player.CarInstantiate                                                                         ||
| 3 | Events.Player.Uninitialize                                                                           ||
| 4 | Events.Player.ReverseTagPlayerTagged                                                                 ||
| 5 | Events.Player.ReverseTagPlayerUntagged                                                               ||
| 6 | Events.Player.Finished                             | Events.Player.Finished                          |
| 7 | Events.Player.CameraInstantiate                                                                      ||
| 8 | Events.Player.ReplayPlayerFinished                                                                   ||
| 9 | Events.Car.Split                                   | Events.Car.Split                                |
| 10 | Events.Car.PreSplit                                                                                 ||
| 11 | Events.Car.Death                                  | Events.Car.Death                                |
| 12 | Events.Car.PreExplode                                                                               ||
| 13 | Events.Car.Explode                                                                                  ||
| 14 | Events.Car.Impact                                                                                   ||
| 15 | Events.Car.Jump                                   | Events.Car.Jump                                 |
| 16 | Events.Car.WingsStateChange                       | Events.Car.WingsStateChange                     |
| 17 | Events.Car.TrickComplete                          | Events.Car.TrickComplete                        |
| 18 | Events.Car.CheckpointHit                          | Events.Car.CheckpointHit                        |
| 19 | Events.Car.BrokeObject                            | Events.Car.BrokeObject                          |
| 20 | Events.Car.ModeSpecial                            | Events.Car.ModeSpecial                          |
| 21 | Events.Car.Horn                                   | Events.Car.Horn                                 |
| 22 | Events.Car.AbilityFailure                                                                           ||
| 23 | Events.Car.AbilityStateChanged                                                                      ||
| 24 | Events.Teleport                                                                                     ||
| 25 | Events.PreTeleport                                | Events.PreTeleport                              |
| 26 | Events.Car.GravityToggled                         | Events.Car.GravityToggled                       |
| 27 | Events.Car.Cooldown                               | Events.Car.Cooldown                             |
| 28 | Events.Car.WarpAnchorHit                          | Events.Car.WarpAnchorHit                        |
| 29 | Events.DropperDroneStateChange                    | Events.DropperDroneStateChange                  |
| 30 | Events.ShardClusterStateChange                    | Events.ShardClusterStateChange                  |
| 31 | Events.ShardClusterFireShard                      | Events.ShardClusterFireShard                    |
| 32 | Events.Car.WheelsSlicedOff                                                                          ||
| 33 | Events.Player.StuntPlayerEnteredBubble                                                              ||
| 34 | Events.Player.StuntPlayerExitedBubble                                                               ||
| 35 | Events.Player.StuntPlayerComboChanged                                                               ||
| 36 | Events.Player.StuntPlayerCollectibleComboChanged                                                    ||