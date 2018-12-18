using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WorkshopSearch
{
    public delegate bool DistanceFilterDelegate(List<DistanceSearchResultItem> levels);

    public class DistanceSearchParameters
    {
        public int MaxSearch = 0;
        public int MaxResults = 0;
        public WorkshopSearchParameters Search = null;
        public DistanceFilterDelegate DistanceLevelFilter = null;
    }

    public struct DistanceSearchResultItem
    {
        public WorkshopItem WorkshopItemResult;
        public DistanceLevel DistanceLevelResult;

        public DistanceSearchResultItem(WorkshopItem item, DistanceLevel level)
        {
            WorkshopItemResult = item;
            DistanceLevelResult = level;
        }
    }

    public class DistanceSearchRetriever
    {
        public DistanceSearchParameters Parameters;
        public Coroutine TaskCoroutine = null;
        public List<DistanceSearchResultItem> Results = new List<DistanceSearchResultItem>();
        public bool Finished = false;
        public string Error = null;
        public bool HasError { get { return Error != null; } }

        public DistanceSearchRetriever(DistanceSearchParameters parameters, bool startCoroutine = true)
        {
            Parameters = parameters;
            if (startCoroutine)
            {
                StartCoroutine();
            }
        }

        public Coroutine StartCoroutine()
        {
            var coroutine = DistanceServerMainStarter.Instance.StartCoroutine(Retrieve());
            TaskCoroutine = coroutine;
            return coroutine;
        }

        public System.Collections.IEnumerator Retrieve()
        {
            Log.SetDebugLineEnabled("Retrieve", false);
            Log.DebugLine("Retrieve", 0);
            var search = Parameters.Search;
            var searchCount = 0;
            Log.DebugLine("Retrieve");
            while ((Parameters.MaxSearch <= 0 || searchCount < Parameters.MaxSearch) && (Parameters.MaxResults <= 0 || Results.Count < Parameters.MaxResults))
            {
                Log.DebugLine("Retrieve", 2);
                var searchResult = search.Search();
                Log.DebugLine("Retrieve");
                yield return searchResult.TaskCoroutine;
                Log.DebugLine("Retrieve");
                if (searchResult.HasError)
                {
                    Log.DebugLine("Retrieve", 5);
                    Error = searchResult.Error;
                    Finished = true;
                    Log.DebugLine("Retrieve");
                    yield break;
                }
                Log.DebugLine("Retrieve", 7);
                var levelIds = new List<string>();
                foreach (var item in searchResult.Result.Items)
                {
                    Log.DebugLine("Retrieve", 8);
                    levelIds.Add(item.PublishedFileId);
                }
                Log.DebugLine("Retrieve", 9);
                var levelsResult = DistanceLevel.RetrieveWorkshopLevel(levelIds);
                Log.DebugLine("Retrieve");
                yield return levelsResult.WebCoroutine;
                Log.DebugLine("Retrieve");
                var searchResults = new List<DistanceSearchResultItem>();
                for (int i = 0; i < searchResult.Result.Items.Length; i++)
                {
                    Log.DebugLine("Retrieve", 12);
                    searchCount++;
                    if (Parameters.MaxSearch > 0 && searchCount > Parameters.MaxSearch)
                    {
                        break;
                    }
                    searchResults.Add(new DistanceSearchResultItem(searchResult.Result.Items[i], levelsResult.Levels[i]));
                    Log.DebugLine("Retrieve", 13);
                }
                Log.DebugLine("Retrieve", 14);
                var cont = true;
                if (Parameters.DistanceLevelFilter != null)
                {
                    Log.DebugLine("Retrieve", 15);
                    cont = Parameters.DistanceLevelFilter(searchResults);
                    Log.DebugLine("Retrieve");
                }
                Log.DebugLine("Retrieve", 17);
                if (Parameters.MaxResults > 0)
                {
                    Log.DebugLine("Retrieve", 18);
                    var max = Results.Count - Parameters.MaxResults + searchResults.Count;
                    for (int i = 0; i < max; i++)
                    {
                        Log.DebugLine("Retrieve", 19);
                        searchResults.RemoveAt(searchResults.Count - 1);
                    }
                }
                Log.DebugLine("Retrieve", 20);
                Results.AddRange(searchResults);
                Log.DebugLine("Retrieve");
                if (!searchResult.Result.HasNextPage || !cont)
                {
                    Log.DebugLine("Retrieve", 22);
                    break;
                }
                else
                {
                    Log.DebugLine("Retrieve", 23);
                    search = searchResult.Result.NextPage;
                }
                Log.DebugLine("Retrieve", 24);
            }
            Log.DebugLine("Retrieve", 25);
            Finished = true;
            yield break;
        }
    }
}
