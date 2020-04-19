using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ServerListTools
{
    public class Entry : DistanceServerPlugin
    {
        public override string Author { get; } = "Corecii; Discord: Corecii#3019";
        public override string DisplayName { get; } = "Server List Tools";
        public override int Priority { get; } = 100;
        public override SemanticVersion ServerVersion => new SemanticVersion("0.1.3");

        public double LogServersFrequency = -1.0;
        public double HealthCheckFrequency = -1.0;
        public double HealthCheckDelay = 60.0;
        public double HealthCheckTimeout = 120.0;

        public double LastSuccessfulHealthCheckTime = -1.0;

        public void ReadSettings()
        {
            var filePath = new System.IO.FileInfo(Manager.ServerDirectory.FullName + "/ServerListTools.json");
            if (!filePath.Exists)
            {
                Log.Info("No ServerListTools.json, using default settings");
                return;
            }
            try
            {
                var txt = System.IO.File.ReadAllText(filePath.FullName);
                var reader = new JsonFx.Json.JsonReader();
                var dictionary = (Dictionary<string, object>)reader.Read(txt);
                TryGetValue(dictionary, "LogServersFrequency", ref LogServersFrequency);
                TryGetValue(dictionary, "HealthCheckFrequency", ref HealthCheckFrequency);
                TryGetValue(dictionary, "HealthCheckDelay", ref HealthCheckDelay);
                TryGetValue(dictionary, "HealthCheckTimeout", ref HealthCheckTimeout);
                Log.Info("Loaded settings from ServerListTools.json");
            }
            catch (Exception e)
            {
                Log.Error($"Couldn't read ServerListTools.json. Is your json malformed?\n{e}");

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

        public override void Start()
        {
            Log.Info("Server List Tools Plugin started!");
            Log.Info($"My guid: {DistanceServerMain.View.owner.guid}");

            ReadSettings();

            if (LogServersFrequency > 0 || HealthCheckFrequency > 0)
            {
                Server.OnUpdateEvent.Connect(() =>
                {
                    MasterServer.PollHostList();
                });
            }

            if (LogServersFrequency > 0)
            {
                Log.Info("Will be logging servers...");
                DistanceServerMainStarter.Instance.StartCoroutine(LogServers());
            }
            if (HealthCheckFrequency > 0)
            {
                Log.Info("Will be running health checks...");
                DistanceServerMainStarter.Instance.StartCoroutine(HealthCheck());
            }
        }

        public System.Collections.IEnumerator HealthCheck()
        {
            yield return new WaitForSeconds((float)HealthCheckDelay);
            LastSuccessfulHealthCheckTime = DistanceServerMain.UnixTime;
            while (true)
            {
                if (DistanceServerMain.UnixTime - LastSuccessfulHealthCheckTime > HealthCheckTimeout)
                {
                    Log.Error($"Quitting because health check failed: server is not on the master server list");
                    Application.Quit();
                }
                DistanceServerMainStarter.Instance.StartCoroutine(ExitIfNotOnServerList());
                yield return new WaitForSeconds((float)HealthCheckFrequency);
            }
        }

        public System.Collections.IEnumerator LogServers()
        {
            while (true)
            {
                DistanceServerMainStarter.Instance.StartCoroutine(LogServerList());
                yield return new WaitForSeconds((float)LogServersFrequency);
            }
        }

        public System.Collections.IEnumerator ExitIfNotOnServerList()
        {
            MasterServer.ClearHostList();
            MasterServer.RequestHostList("Distance");
            yield return new WaitForServerList();
            var servers = MasterServer.PollHostList();
            var hasMe = servers.Any((server) =>
            {
                return server.guid == DistanceServerMain.View.owner.guid;
            });
            if (hasMe)
            {
                LastSuccessfulHealthCheckTime = DistanceServerMain.UnixTime;
            }
            yield break;
        }

        public System.Collections.IEnumerator LogServerList()
        {
            MasterServer.ClearHostList();
            MasterServer.RequestHostList("Distance");
            yield return new WaitForServerList();
            var servers = MasterServer.PollHostList();
            var i = 0;
            Log.Debug("Server list:");
            foreach (var server in servers)
            {
                i++;
                Log.Debug($"{i}: {String.Join(",", server.ip)}:{server.port} {server.gameName} {server.gameType} {server.connectedPlayers}/{server.playerLimit} {server.useNat} {(server.passwordProtected ? "private" : "public")} {server.comment} {server.guid}");
            }
            yield break;
        }
    }

    public class WaitForServerList : UnityEngine.CustomYieldInstruction
    {
        public override bool keepWaiting => !done;

        bool done = false;
        IEventConnection conn;

        public WaitForServerList()
        {
            conn = DistanceServerMain.Instance.Server.OnMasterServerEvent.Connect(evt =>
            {
                if (evt == MasterServerEvent.HostListReceived)
                {
                    conn.Disconnect();
                    conn = null;
                    done = true;
                }
            });
        }
    }
}
