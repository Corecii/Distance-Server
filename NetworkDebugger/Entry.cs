using Spectrum.API;
using Spectrum.API.Interfaces.Plugins;
using Spectrum.API.Interfaces.Systems;
using System;
using System.Reflection;
using Harmony;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Corecii.NetworkDebugger
{
    public class Entry : IPlugin
    {
        public string FriendlyName => "Network Debugger";
        public string Author => "Corecii";
        public string Contact => "SteamID: Corecii; Discord: Corecii#3019";
        public static string PluginVersion = "Version C.1.1.0";

        public static List<Type> ClientToClientDataList;
        public static List<Type> ClientToServerDataList;
        public static List<Type> ServerToClientDataList;
        public static List<Type> InstancedDataList = new List<Type>();

        public void Initialize(IManager manager, string ipcIdentifier)
        {
            try {
                var harmony = HarmonyInstance.Create("com.corecii.distance.networkDebugger");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception e)
            {
                Console.WriteLine("Patching errors!\n" + e);
            }

            try
            {
                Console.WriteLine("Events:");
                var eventTranscievers = getComponents<NetworkStaticEventTransceiver>();
                foreach (var eventTransciever in eventTranscievers)
                {
                    Console.WriteLine($"\t{eventTransciever.GetType()}");
                    var list = (IEnumerable)typeof(NetworkStaticEventTransceiver)
                        .GetField(
                            "list_",
                            BindingFlags.Instance | BindingFlags.NonPublic
                        )
                        .GetValue(eventTransciever);
                    var realList = new List<Type>();
                    if (eventTransciever.GetType() == typeof(ClientToClientNetworkTransceiver))
                    {
                        Console.WriteLine("\tClientToClient ReceiveBroadcastAllEvent");
                        ClientToClientDataList = realList;
                    }
                    else if (eventTransciever.GetType() == typeof(ClientToServerNetworkTransceiver))
                    {
                        Console.WriteLine("\tClientToServer ReceiveClientToServerEvent");
                        ClientToServerDataList = realList;
                    }
                    else if (eventTransciever.GetType() == typeof(ServerToClientNetworkTransceiver))
                    {
                        Console.WriteLine("\tServerToClient ReceiveServerToClientEvent ReceiveTargetedEventServerToClient");
                        ServerToClientDataList = realList;
                    }
                    var views = (NetworkView[]) typeof(NetworkStaticEventTransceiver)
                        .GetField(
                            "networkViews_",
                            BindingFlags.Instance | BindingFlags.NonPublic
                        )
                        .GetValue(eventTransciever);
                    int viewIndex = 0;
                    foreach (var view in views)
                    {
                        var viewId = view.viewID;
                        var a = (int) typeof(NetworkViewID)
                        .GetField(
                            "a",
                            BindingFlags.Instance | BindingFlags.NonPublic
                        )
                        .GetValue(viewId);
                        var b = (int)typeof(NetworkViewID)
                        .GetField(
                            "b",
                            BindingFlags.Instance | BindingFlags.NonPublic
                        )
                        .GetValue(viewId);
                        var c = (int)typeof(NetworkViewID)
                        .GetField(
                            "c",
                            BindingFlags.Instance | BindingFlags.NonPublic
                        )
                        .GetValue(viewId);
                        var group = (NetworkGroup)(viewIndex++);
                        Console.WriteLine($"{group} {a} {b} {c}");
                    }
                    var index = 0;
                    foreach (var obj in list)
                    {
                        Type type1 = obj.GetType();
                        Type type2 = type1.GetGenericArguments()[0];
                        realList.Add(type2);
                        Console.WriteLine($"\t{index++}: {type2}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception!\n" + e);
            }
        }

        public static T getComponent<T>() where T : MonoBehaviour
        {
            GameObject[] objs = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (GameObject tObj in objs)
            {
                T component = tObj.GetComponent<T>();
                if (component != null)
                    return component;
            }
            return null;
        }

        public static List<T> getComponents<T>() where T : MonoBehaviour
        {
            List<T> results = new List<T>();
            GameObject[] objs = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (GameObject tObj in objs)
            {
                T[] components = tObj.GetComponents<T>();
                results.AddRange(components);
            }
            return results;
        }

        static BitStreamReader debugBitStreamReader = new BitStreamReader(null);
        

        static string DeepPrint(object data, int depth, List<string> allowedProps)
        {
            string tabs = "";
            for (int i = 0; i < depth; i++)
            {
                tabs += "\t";
            }
            string outputTxt = "";
            try
            {
                foreach (var prop in data.GetType().GetProperties())
                {
                    if (prop.CanRead && prop.GetGetMethod().GetParameters().Length == 0) {
                        outputTxt += $"{tabs}{prop.Name}  {prop.GetValue(data, null)}\n";
                        if (allowedProps.Contains(prop.Name))
                        {
                            outputTxt += DeepPrint(prop.GetValue(data, null), depth + 1, allowedProps);
                        }
                    }
                }
                foreach (var field in data.GetType().GetFields())
                {
                    outputTxt += $"{tabs}{field.Name}  {field.GetValue(data)}\n";
                    if (allowedProps.Contains(field.Name))
                    {
                        outputTxt += DeepPrint(field.GetValue(data), depth + 1, allowedProps);
                    }
                }
            }
            catch (Exception e)
            {
                outputTxt += $"{tabs}Failed to read data because: {e}\n";
            }
            return outputTxt;
        }

        static List<string> printableProps = new List<string>() {"data_", "carColors_", "playerViewID_", "carViewID0_", "carViewID1_", "lateData_"};
        static int debugCount = 0;
        static void DebugBytes(string name, byte[] bytes, List<Type> dataLookup)
        {
            var hex = System.BitConverter.ToString(bytes).Replace("-", "");
            var ascii = System.Text.Encoding.ASCII.GetString(bytes);
            var outputTxt = "";
            outputTxt += $"{debugCount} RECV {name}\n";
            debugCount++;

            if (dataLookup == null)
            {
                outputTxt += $"\tNo data lookup\n";
                outputTxt += $"\tHex: {hex}\n";
                outputTxt += $"\tAscii: {ascii}\n";
            }
            else
            {
                debugBitStreamReader.Encapsulate(bytes);
                int index = 0;
                debugBitStreamReader.Serialize(ref index);
                if (index < 0 || index >= dataLookup.Count)
                {
                    outputTxt += $"\tReceived invalid event index: {index} out of {dataLookup.Count}\n";
                    outputTxt += $"\tHex: {hex}\n";
                    outputTxt += $"\tAscii: {ascii}\n";
                }
                else
                {
                    try
                    {
                        IBitSerializable data = (IBitSerializable) Activator.CreateInstance(dataLookup[index]);
                        data.Serialize(debugBitStreamReader);
                        outputTxt += $"\t{dataLookup[index]}\n";
                        outputTxt += DeepPrint(data, 2, printableProps) + "\n";
                    }
                    catch (Exception e)
                    {
                        outputTxt += $"\tFailed to read data because: {e}";
                    }
                }
            }

            Console.WriteLine(outputTxt);
        }

        [HarmonyPatch(typeof(NetworkingManager))]
        [HarmonyPatch("OnConnectedToServer")]
        class PatchClientConnected
        {
            static void Prefix()
            {
                Console.WriteLine($"{debugCount++} RECV Connected To Server");
            }
        }

        [HarmonyPatch(typeof(NetworkingManager))]
        [HarmonyPatch("OnPlayerConnected")]
        class PatchConnected
        {
            static void Prefix(NetworkPlayer player)
            {
                Console.WriteLine($"{debugCount++} RECV Player Connected: {player.guid}");
            }
        }

        [HarmonyPatch(typeof(NetworkingManager))]
        [HarmonyPatch("OnPlayerDisconnected")]
        class PatchDisconnected
        {
            static void Prefix(NetworkPlayer player)
            {
                Console.WriteLine($"{debugCount++} RECV Player Disconnected: {player.guid}");
            }
        }

        [HarmonyPatch(typeof(NetworkStaticEventTransceiver))]
        [HarmonyPatch("BroadcastEventRPC")]
        class Patch0
        {
            static void Prefix(NetworkGroup group, string rpcName, NetworkTarget networkTarget, int eventIndex, IBitSerializable serializable)
            {
                var outputTxt = "";
                outputTxt += $"{debugCount} SEND {rpcName}\n";
                debugCount++;
                if (networkTarget.UsingRPCMode_)
                {
                    var mode = typeof(NetworkTarget)
                        .GetField(
                            "mode_",
                            BindingFlags.Instance | BindingFlags.NonPublic
                        )
                        .GetValue(networkTarget);
                    if (networkTarget.SendToSelf_)
                    {
                        mode = RPCMode.All;
                    }
                    outputTxt += $"\tTo RPCMode {mode}\n";
                } else
                {
                    var recipient = typeof(NetworkTarget)
                        .GetField(
                            "recipient_",
                            BindingFlags.Instance | BindingFlags.NonPublic
                        )
                        .GetValue(networkTarget);
                    outputTxt += $"\tTo Player {recipient}\n";
                }
                outputTxt += $"\tIn {group}\n";

                List<Type> dataLookup = null;
                if (rpcName == "ReceiveBroadcastAllEvent")
                {
                    dataLookup = ClientToClientDataList;
                } else if (rpcName == "ReceiveClientToServerEvent")
                {
                    dataLookup = ClientToServerDataList;
                } else if (rpcName == "ReceiveServerToClientEvent" || rpcName == "ReceiveTargetedEventServerToClient")
                {
                    dataLookup = ServerToClientDataList;
                } else
                {
                    outputTxt += "\tUnknown Event\n";
                }
                if (dataLookup != null) {
                    if (eventIndex < 0 || eventIndex >= dataLookup.Count)
                    {
                        outputTxt += $"\tSending invalid event index: {eventIndex} out of {dataLookup.Count}\n";
                    }
                }
                try
                {
                    var data = serializable;
                    if (dataLookup != null)
                    {
                        outputTxt += $"\t{dataLookup[eventIndex]}\n";
                    } else
                    {
                        outputTxt += $"\t{data.GetType()}";
                    }
                    outputTxt += DeepPrint(data, 2, printableProps) + "\n";
                }
                catch (Exception e)
                {
                    outputTxt += $"\tFailed to read data because: {e}\n";
                }

                if (outputTxt.Length > 0)
                {
                    outputTxt = outputTxt.Substring(0, outputTxt.Length - 1);
                }

                Console.WriteLine(outputTxt);
            }
        }

        [HarmonyPatch(typeof(NetworkingManager))]
        [HarmonyPatch("RequestServerNetworkTimeSync")]
        class Patch01
        {
            static void Prefix()
            {
                if (!G.Sys.NetworkingManager_.IsServer_)
                {
                    Console.WriteLine($"{debugCount++} SEND SubmitServerNetworkTimeSync");
                }
            }
        }

        [HarmonyPatch(typeof(NetworkingManager))]
        [HarmonyPatch("SubmitServerNetworkTimeSync")]
        class Patch1
        {
            static void Prefix(NetworkMessageInfo info)
            {
                Console.WriteLine($"{debugCount++} RECV SubmitServerNetworkTimeSync");
                Console.WriteLine($"{debugCount++} SEND ReceiveServerNetworkTimeSync");
            }
        }

        [HarmonyPatch(typeof(NetworkingManager))]
        [HarmonyPatch("ReceiveServerNetworkTimeSync")]
        class Patch2
        {
            static void Prefix(NetworkMessageInfo info)
            {
                Console.WriteLine($"{debugCount++} RECV ReceiveServerNetworkTimeSync");
            }
        }

        [HarmonyPatch(typeof(ClientToClientNetworkTransceiver))]
        [HarmonyPatch("ReceiveBroadcastAllEvent")]
        class Patch3
        {
            static void Prefix(byte[] bytes)
            {
                DebugBytes("ReceiveBroadcastAllEvent", bytes, ClientToClientDataList);
            }
        }

        [HarmonyPatch(typeof(ClientToServerNetworkTransceiver))]
        [HarmonyPatch("ReceiveClientToServerEvent")]
        class Patch4
        {
            static void Prefix(byte[] bytes)
            {
                DebugBytes("ReceiveClientToServerEvent", bytes, ClientToServerDataList);
            }
        }

        [HarmonyPatch(typeof(ServerToClientNetworkTransceiver))]
        [HarmonyPatch("ReceiveServerToClientEvent")]
        class Patch5
        {
            static void Prefix(byte[] bytes)
            {
                DebugBytes("ReceiveServerToClientEvent", bytes, ServerToClientDataList);
            }
        }

        [HarmonyPatch(typeof(ServerToClientNetworkTransceiver))]
        [HarmonyPatch("ReceiveTargetedEventServerToClient")]
        class Patch6
        {
            static void Prefix(byte[] bytes)
            {
                DebugBytes("ReceiveTargetedEventServerToClient", bytes, ServerToClientDataList);
            }
        }

        [HarmonyPatch(typeof(NetworkInstancedEventTransceiver))]
        [HarmonyPatch("ReceiveSerializeEvent")]
        class Patch7
        {
            static void Prefix(NetworkInstancedEventTransceiver __instance, byte[] bytes)
            {
                DebugBytes("ReceiveSerializeEvent", bytes, InstancedDataList);

                try {
                    var outputTxt = "";

                    var ab = typeof(NetworkInstancedEventTransceiver);
                    var ac = ab.GetField(
                            "networkView_",
                            BindingFlags.Instance | BindingFlags.NonPublic
                        );
                    var networkView = (NetworkView)ac.GetValue(__instance);
                    if (networkView == null)
                    {
                        outputTxt += "\tNo NetworkView";
                        Console.WriteLine(outputTxt);
                        return;
                    }
                    var viewId = networkView.viewID;
                    var a = (int)typeof(NetworkViewID)
                        .GetField(
                            "a",
                            BindingFlags.Instance | BindingFlags.NonPublic
                        )
                        .GetValue(viewId);
                    var b = (int)typeof(NetworkViewID)
                        .GetField(
                            "b",
                            BindingFlags.Instance | BindingFlags.NonPublic
                        )
                        .GetValue(viewId);
                    var c = (int)typeof(NetworkViewID)
                        .GetField(
                            "c",
                            BindingFlags.Instance | BindingFlags.NonPublic
                        )
                        .GetValue(viewId);
                    outputTxt += $"\tNetworkView id: {a} {b} {c}\n";
                    Console.WriteLine(outputTxt);
                } catch (Exception e)
                    {
                        Console.WriteLine($"Error on NetworkInstancedEventTransceiver ReceiveSerializeEvent Debug: {e}");
                    }
    }
        }

        [HarmonyPatch(typeof(NetworkInstancedEventTransceiver))]
        [HarmonyPatch("OnSerializeEvent")]
        class Patch8
        {
            static void Prefix(int eventIndex, IBitSerializable serializable)
            {
                var outputTxt = "";
                outputTxt += $"{debugCount} SEND ReceiveSerializeEvent\n";
                debugCount++;
                
                if (eventIndex < 0 || eventIndex >= InstancedDataList.Count)
                {
                    outputTxt += $"\tSending invalid event index: {eventIndex} out of {InstancedDataList.Count}\n";
                    outputTxt += $"\t{serializable.GetType()}";
                }
                else
                {
                    outputTxt += $"\t{InstancedDataList[eventIndex]}\n";
                }

                try
                {
                    outputTxt += DeepPrint(serializable, 2, printableProps) + "\n";
                }
                catch (Exception e)
                {
                    outputTxt += $"\tFailed to read data because: {e}\n";
                }

                Console.WriteLine(outputTxt);
            }
        }

        [HarmonyPatch(typeof(NetworkStateTransceiver))]
        [HarmonyPatch("OnSerializeNetworkView")]
        class Patch9
        {
            static void Prefix(NetworkStateTransceiver __instance, BitStream stream, NetworkMessageInfo info)
            {
                try {
                    var outputTxt = "";
                    outputTxt += $"{debugCount} RECV OnSerializeNetworkView from {__instance.GetType()}\n";
                    debugCount++;
                    outputTxt += $"\tPlayer: {info.sender}\n";

                    var viewId = info.networkView.viewID;
                    var a = (int)typeof(NetworkViewID)
                    .GetField(
                        "a",
                        BindingFlags.Instance | BindingFlags.NonPublic
                    )
                    .GetValue(viewId);
                    var b = (int)typeof(NetworkViewID)
                    .GetField(
                        "b",
                        BindingFlags.Instance | BindingFlags.NonPublic
                    )
                    .GetValue(viewId);
                    var c = (int)typeof(NetworkViewID)
                    .GetField(
                        "c",
                        BindingFlags.Instance | BindingFlags.NonPublic
                    )
                    .GetValue(viewId);
                    outputTxt += $"\tNetworkView id: {a} {b} {c}";

                    Console.WriteLine(outputTxt);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error on OnSerializeNetworkView Debug: {e}");
                }
            }
        }

        /*
        [HarmonyPatch(typeof(ClientLogic))]
        [HarmonyPatch("UpdateLevelIfNecessaryThenGoToGameMode")]
        class PatchLoadFail
        {
            static bool Prefix(ClientLogic __instance)
            {
                try
                {
                    typeof(ClientLogic).GetMethod("NotEnoughDataToUpdateWorkshopLevel", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[0]);
                    return false;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error on UpdateLevelIfNecessaryThenGoToGameMode: {e}");
                    return true;
                }
            }
        }
        */

        [HarmonyPatch(typeof(LevelSetsManager))]
        [HarmonyPatch("Start")]
        class PatchLevels
        {
            static void Postfix()
            {
                try
                {
                    Console.WriteLine("Official Levels:");
                    int levelIndex = 0;
                    foreach (var level in G.Sys.LevelSets_.OfficialLevelInfosList_)
                    {
                        Console.WriteLine(levelIndex++);
                        Console.WriteLine($"\t{level.relativePath_}\n\t{level.levelName_}");
                    }
                    Console.WriteLine("----");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error logging official levels: {e}");
                }
            }
        }

        [HarmonyPatch(typeof(NetworkInstancedEventTransceiver))]
        [HarmonyPatch("Awake")]
        class Patch10
        {
            static void Postfix(NetworkInstancedEventTransceiver __instance)
            {
                try
                {
                    var outputTxt = "";
                    outputTxt += $"{debugCount} NetworkInstancedEventTransceiver Awake from {__instance.GetType()}\n";
                    debugCount++;

                    var ab = typeof(NetworkInstancedEventTransceiver);
                    var ac = ab.GetField(
                            "networkView_",
                            BindingFlags.Instance | BindingFlags.NonPublic
                        );
                    var networkView = (NetworkView)ac.GetValue(__instance);
                    if (networkView == null)
                    {
                        outputTxt += "\tNo NetworkView";
                        Console.WriteLine(outputTxt);
                        return;
                    }
                    var viewId = networkView.viewID;
                    var a = (int)typeof(NetworkViewID)
                        .GetField(
                            "a",
                            BindingFlags.Instance | BindingFlags.NonPublic
                        )
                        .GetValue(viewId);
                    var b = (int)typeof(NetworkViewID)
                        .GetField(
                            "b",
                            BindingFlags.Instance | BindingFlags.NonPublic
                        )
                        .GetValue(viewId);
                    var c = (int)typeof(NetworkViewID)
                        .GetField(
                            "c",
                            BindingFlags.Instance | BindingFlags.NonPublic
                        )
                        .GetValue(viewId);
                    outputTxt += $"\tNetworkView id: {a} {b} {c}\n";

                    var eventInstances = (RegisteredEvents.Instances)typeof(NetworkInstancedEventTransceiver)
                        .GetField(
                            "eventInstances_",
                            BindingFlags.Instance | BindingFlags.NonPublic
                        )
                        .GetValue(__instance);

                    var events = (Events.InstancedEventBase[])typeof(RegisteredEvents.Instances)
                        .GetField(
                            "events_",
                            BindingFlags.Instance | BindingFlags.NonPublic
                        )
                        .GetValue(eventInstances);
                    var tEvents = (Events.ITransceivedEvent[])typeof(RegisteredEvents.Instances)
                        .GetField(
                            "tEvents_",
                            BindingFlags.Instance | BindingFlags.NonPublic
                        )
                        .GetValue(eventInstances);

                    var count = 0;
                    if (events.Length == tEvents.Length)
                    {
                        var addEvents = InstancedDataList.Count == 0;
                        outputTxt += "\tEvents:";
                        for (int i = 0; i < events.Length; i++)
                        {
                            outputTxt += String.Format("\n\t\t{0}: {1,-50} {2,-50}", count++, events[i]?.GetType(), tEvents[i]?.GetType());
                            if (addEvents)
                            {
                                InstancedDataList.Add(events[i].GetType().GetNestedType("Data"));
                            }
                        }
                    }
                    else
                    {
                        outputTxt += "\tNormal events:";
                        foreach (var evt in events)
                        {
                            outputTxt += $"\n\t\t{count++}: {evt?.GetType()}";
                        }
                        outputTxt += "\n\tTransceived Events:";
                        count = 0;
                        foreach (var evt in tEvents)
                        {
                            outputTxt += $"\n\t\t{count++}: {evt?.GetType()}";
                        }
                    }

                    Console.WriteLine(outputTxt);
                } catch (Exception e)
                {
                    Console.WriteLine($"Error on NetworkInstancedEventTransceiver Awake Debug: {e}");
                }
            }
        }

        public void Shutdown() { }
    }
}
