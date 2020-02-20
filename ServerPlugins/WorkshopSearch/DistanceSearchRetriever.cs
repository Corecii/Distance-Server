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
            Log.SetDebugLineEnabled("Retrieve", true);
            var search = Parameters.Search;
            var searchCount = 0;
            while ((Parameters.MaxSearch <= 0 || searchCount < Parameters.MaxSearch) && (Parameters.MaxResults <= 0 || Results.Count < Parameters.MaxResults))
            {
                Log.DebugLine("Retrieve", 1);
                var searchResult = search.Search();
                yield return searchResult.TaskCoroutine;
                if (searchResult.HasError)
                {
                    Error = searchResult.Error;
                    Finished = true;
                    yield break;
                }
                Log.DebugLine("Retrieve");
                var levelIds = new List<string>();

                if (searchResult.Result == null)
                {
                    Log.Warn("HtmlAgilityPack might not be loaded");
                }
                foreach (var item in searchResult.Result.Items)
                {
                    levelIds.Add(item.PublishedFileId);
                }
                Log.DebugLine("Retrieve");
                var levelsResult = DistanceLevel.RetrieveWorkshopLevel(levelIds);
                yield return levelsResult.WebCoroutine;
                var searchResults = new List<DistanceSearchResultItem>();
                for (int i = 0; i < searchResult.Result.Items.Length; i++)
                {
                    searchCount++;
                    if (Parameters.MaxSearch > 0 && searchCount > Parameters.MaxSearch)
                    {
                        break;
                    }
                    var item = searchResult.Result.Items[i];
                    if (levelsResult.LevelsByPublishedFileId.ContainsKey(item.PublishedFileId))
                    {
                        searchResults.Add(new DistanceSearchResultItem(searchResult.Result.Items[i], levelsResult.LevelsByPublishedFileId[item.PublishedFileId]));
                    }
                }
                Log.DebugLine("Retrieve");
                var cont = true;
                if (Parameters.DistanceLevelFilter != null)
                {
                    cont = Parameters.DistanceLevelFilter(searchResults);
                }
                Log.DebugLine("Retrieve");
                if (Parameters.MaxResults > 0)
                {
                    var max = Results.Count + searchResults.Count - Parameters.MaxResults;
                    for (int i = 0; i < max; i++)
                    {
                        searchResults.RemoveAt(searchResults.Count - 1);
                    }
                }
                Log.DebugLine("Retrieve");
                Results.AddRange(searchResults);
                Log.DebugLine("Retrieve");
                if (!searchResult.Result.HasNextPage || !cont)
                {
                    break;
                }
                else
                {
                    search = searchResult.Result.NextPage;
                }
                Log.DebugLine("Retrieve");
            }
            Finished = true;
            yield break;
        }
    }
}
