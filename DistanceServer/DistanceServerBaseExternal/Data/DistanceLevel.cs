extern alias Distance;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class DistanceLevel : IExternalData
{
    public string Name = "";
    public string RelativeLevelPath = "";
    public string WorkshopFileId = "";
    public string LevelVersion = "";
    public string GameMode = "";
    public string[] SupportedModes = new string[0];

    public List<object> ExternalData = new List<object>();
    public T GetExternalData<T>()
    {
        return (T)ExternalData.Find(val => val is T);
    }
    public void AddExternalData(object val)
    {
        ExternalData.Add(val);
    }

    public DistanceLevel Clone()
    {
        return (DistanceLevel)MemberwiseClone();
    }

    public DistanceLevel WithGameMode(string gameMode)
    {
        var level = Clone();
        level.GameMode = gameMode;
        return level;
    }

    public bool SupportsGameMode(string gameMode)
    {
        return SupportedModes.Contains(gameMode);
    }

    public Distance::Events.ServerToClient.SetLevelName.Data GetLevelNameData(bool hideLevelName, bool isCustomPlaylist)
    {
        return new Distance::Events.ServerToClient.SetLevelName.Data()
        {
            levelName_ = Name,
            relativeLevelPath_ = RelativeLevelPath,
            workshopPublishedFileID_ = WorkshopFileId,
            hideLevelName_ = hideLevelName,
            isCustomPlaylist_ = isCustomPlaylist
        };
    }

    public Distance::Events.ServerToClient.SetGameMode.Data GetGameModeData()
    {
        return new Distance::Events.ServerToClient.SetGameMode.Data()
        {
            mode_ = GameMode
        };
    }


    const string fileDetailsUrl = "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";

    public static string GenerateWorkshopInfoBody(List<string> workshopFileIds)
    {
        string body = $"itemcount={workshopFileIds.Count}";
        for (int i = 0; i < workshopFileIds.Count; i++)
        {
            body += $"&publishedfileids%5B{i}%5D={workshopFileIds[i]}";
        }
        return body;
    }
    
    public static WorkshopLevelRetreiver RetrieveWorkshopLevel(List<string> workshopLevelIds, bool startCoroutine = true, string defaultGameMode = "Sprint")
    {
        return new WorkshopLevelRetreiver(workshopLevelIds, startCoroutine, defaultGameMode);
    }

    public class WorkshopLevelRetreiver
    {
        public List<string> WorkshopLevelIds;
        public Coroutine WebCoroutine = null;
        public Dictionary<string, DistanceLevel> LevelsByPublishedFileId = new Dictionary<string, DistanceLevel>();
        public List<DistanceLevel> ValidLevels = new List<DistanceLevel>();
        public string Error = null;
        public bool HasError { get { return Error != null; } }
        public bool Finished = false;
        public string DefaultGameMode;

        public WorkshopLevelRetreiver(List<string> workshopLevelIds, bool startCoroutine = true, string defaultGameMode = "Sprint")
        {
            WorkshopLevelIds = workshopLevelIds;
            DefaultGameMode = defaultGameMode;
            if (startCoroutine)
            {
                StartCoroutine();
            }
        }

        public Coroutine StartCoroutine()
        {
            var coroutine = DistanceServerMainStarter.Instance.StartCoroutine(RetrieveLevel());
            WebCoroutine = coroutine;
            return coroutine;
        }

        IEnumerator RetrieveLevel()
        {
            var body = GenerateWorkshopInfoBody(WorkshopLevelIds);
            var request = new UnityWebRequest(fileDetailsUrl);
            request.uploadHandler = new UploadHandlerRaw(Encoding.ASCII.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.method = UnityWebRequest.kHttpVerbPOST;
            request.uploadHandler.contentType = "application/x-www-form-urlencoded";

            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Finished = true;
                Error = request.error;
                yield break;
            }

            var reader = new JsonFx.Json.JsonReader();
            Dictionary<string, object> responseJson;
            try
            {
                responseJson = reader.Read<Dictionary<string, object>>(request.downloadHandler.text);
            }
            catch (Exception e)
            {
                Finished = true;
                Error = e.ToString();
                yield break;
            }

            if (!responseJson.ContainsKey("response"))
            {
                Finished = true;
                Error = "No response key returned from server";
                yield break;
            }

            var response = (Dictionary<string, object>)responseJson["response"];
            if (!response.ContainsKey("publishedfiledetails"))
            {
                Finished = true;
                Error = "No publishedfiledetails key returned from server";
                yield break;
            }

            var fileDetails = (object[])response["publishedfiledetails"];
            var index = 0;
            foreach (var detailBase in fileDetails)
            {
                var detail = (Dictionary<string, object>)detailBase;
                if (!detail.ContainsKey("result") || (int)detail["result"] != 1 || !detail.ContainsKey("title") || !detail.ContainsKey("creator") || !detail.ContainsKey("filename"))
                {
                    continue;
                }
                if (!detail.ContainsKey("publishedfileid") || detail["publishedfileid"].GetType() != typeof(string) || (string)detail["publishedfileid"] == string.Empty)
                {
                    continue;
                }
                if (detail["title"].GetType() != typeof(string))
                {
                    continue;
                }
                if (detail["creator"].GetType() != typeof(string) || (string)detail["creator"] == string.Empty)
                {
                    continue;
                }
                if (detail["filename"].GetType() != typeof(string) || (string)detail["filename"] == "")
                {
                    continue;
                }
                string levelVersion = "";
                if (detail.ContainsKey("time_updated"))
                {
                    DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                    dtDateTime = dtDateTime.AddSeconds((int)detail["time_updated"]).ToLocalTime();
                    levelVersion = dtDateTime.ToBinary().ToString();
                }

                List<string> supportedModes = new List<string>();
                if (detail.ContainsKey("tags"))
                {
                    foreach (var tagBase in (object[])detail["tags"])
                    {
                        var tag = (Dictionary<string, object>)tagBase;
                        if (tag.ContainsKey("tag") && (string)tag["tag"] != "Level")
                        {
                            supportedModes.Add((string)tag["tag"]);
                        }
                    }
                }
                var level = new DistanceLevel()
                {
                    Name = (string)detail["title"],
                    RelativeLevelPath = $"WorkshopLevels/{(string)detail["creator"]}/{(string)detail["filename"]}",
                    // Example: "WorkshopLevels/76561198145035078/a digital frontier.bytes"
                    LevelVersion = levelVersion,
                    WorkshopFileId = (string)detail["publishedfileid"],
                    GameMode = DefaultGameMode,
                    SupportedModes = supportedModes.ToArray(),
                };
                ValidLevels.Add(level);
                LevelsByPublishedFileId.Add(level.WorkshopFileId, level);
                index++;
            }
            Finished = true;
        }
    }
}
