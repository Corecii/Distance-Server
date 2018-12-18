using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WorkshopSearch
{
    public class Workshop
    {
        public const string DistanceAppId = "233610";
        public static WorkshopSearchRetriever Search(WorkshopSearchParameters parameters, bool startCoroutine = true)
        {
            return new WorkshopSearchRetriever(parameters, startCoroutine);
        }
        public static DistanceLevel.WorkshopLevelRetreiver RetrieveDistanceLevels(WorkshopItem[] items)
        {
            List<string> levelIds = new List<string>(items.Length);
            foreach (var item in items)
            {
                levelIds.Add(item.PublishedFileId);
            }
            return DistanceLevel.RetrieveWorkshopLevel(levelIds);
        }
    }
}
