using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace WorkshopSearch
{
    public struct WorkshopItem
    {
        public string PublishedFileId;
        public string AppId;
        public string AuthorName;
        public string ItemName;
        public string FileImageUrl;
        public int Rating;
        public WorkshopItemType ItemType;
    }

    public enum WorkshopItemType
    {
        File,
        Collection,
    }

    public class WorkshopSearchResult
    {
        public WorkshopItem[] Items;
        public int Page;
        public int PageCount;
        public int TotalItemCount;
        public WorkshopItemType ItemType;
        public WorkshopSearchParameters Search;

        public bool HasNextPage { get { return Page < PageCount; } }
        public WorkshopSearchParameters NextPage
        {
            get
            {
                var page = Search;
                page.Page = Page + 1;
                return page;
            }
        }

        public bool HasPrevPage { get { return Page > 1; } }
        public WorkshopSearchParameters PrevPage
        {
            get
            {
                var page = Search;
                page.Page = Page - 1;
                return page;
            }
        }
    }

    public class WorkshopSearchRetriever
    {
        public WorkshopSearchParameters Parameters;
        public Coroutine TaskCoroutine = null;
        public WorkshopSearchResult Result = null;
        public bool Finished = false;
        public string Error = null;
        public bool HasError { get { return Error != null; } }

        public WorkshopSearchRetriever(WorkshopSearchParameters parameters, bool startCoroutine = true)
        {
            Parameters = parameters;
            StartCoroutine();
        }

        public Coroutine StartCoroutine()
        {
            var coroutine = DistanceServerMainStarter.Instance.StartCoroutine(Retrieve());
            TaskCoroutine = coroutine;
            return coroutine;
        }

        public System.Collections.IEnumerator Retrieve()
        {
            switch (Parameters.SearchType)
            {
                case WorkshopSearchParameters.SearchTypeType.UserFiles:
                case WorkshopSearchParameters.SearchTypeType.GameFiles:
                    yield return RetrieveFiles();
                    break;
                case WorkshopSearchParameters.SearchTypeType.UserCollections:
                case WorkshopSearchParameters.SearchTypeType.GameCollections:
                    yield return RetrieveCollections();
                    break;
                case WorkshopSearchParameters.SearchTypeType.CollectionFiles:
                    yield return RetrieveCollectionFiles();
                    break;
            }
            yield break;
        }

        public System.Collections.IEnumerator RetrieveCollectionFiles()
        {
            var url = Parameters.ConstructUrl();

            var request = new UnityWebRequest(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.method = UnityWebRequest.kHttpVerbGET;

            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Finished = true;
                Error = request.error;
                yield break;
            }

            try
            {

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(request.downloadHandler.text);

                var globalAppId = htmlDoc.DocumentNode.SelectSingleNode(@".//div[contains(concat(' ', @class, ' '), ' apphub_OtherSiteInfo ')]")?.SelectSingleNode(@".//a")?.GetAttributeValue("href", null);
                if (globalAppId != null)
                {
                    var appIdMatch = Regex.Match(globalAppId, @"/app/(\d+)$");
                    if (appIdMatch.Success)
                    {
                        globalAppId = appIdMatch.Groups[1].Value;
                    }
                    else
                    {
                        globalAppId = null;
                    }
                }

                var itemNodes = htmlDoc.DocumentNode.SelectNodes(@".//div[contains(concat(' ', @class, ' '), ' collectionItem ')]");

                if (itemNodes != null && itemNodes.Count > 0)
                {
                    WorkshopItem[] items = new WorkshopItem[itemNodes.Count];

                    var index = 0;
                    foreach (var itemNode in itemNodes)
                    {
                        var fileId = itemNode.SelectSingleNode(@".//div[contains(@class, 'workshopItem')]")?.SelectSingleNode(@".//a")?.GetAttributeValue("href", null);
                        if (fileId != null)
                        {
                            var fileIdMatch = Regex.Match(fileId, @"?id=(\d+)$");
                            if (fileIdMatch.Success)
                            {
                                fileId = fileIdMatch.Groups[1].Value;
                            }
                            else
                            {
                                fileId = null;
                            }
                        }
                        var image = itemNode.SelectSingleNode(@".//img[contains(@class, 'workshopItemPreviewImage')]").GetAttributeValue("src", "");
                        var authorNode = itemNode.SelectSingleNode(@".//span[contains(@class, 'workshopItemAuthorName')]").SelectSingleNode("a");
                        var authorName = authorNode.InnerText;
                        var itemTitle = itemNode.SelectSingleNode(@".//div[contains(@class, 'workshopItemTitle')]").InnerText;
                        var ratingSrc = itemNode.SelectSingleNode(@".//img[contains(@class, 'fileRating')]").GetAttributeValue("src", "");
                        var ratingMatch = Regex.Match(ratingSrc, @"(\d+)-star");
                        var rating = -1;
                        if (ratingMatch.Success)
                        {
                            rating = int.Parse(ratingMatch.Groups[1].ToString());
                        }
                        items[index] = new WorkshopItem()
                        {
                            AppId = globalAppId,
                            PublishedFileId = fileId,
                            AuthorName = authorName,
                            ItemName = itemTitle,
                            FileImageUrl = image,
                            Rating = rating,
                            ItemType = WorkshopItemType.File,
                        };
                        index++;
                    }

                    Result = new WorkshopSearchResult()
                    {
                        Items = items,
                        Page = 1,
                        PageCount = 1,
                        TotalItemCount = items.Length,
                        Search = Parameters,
                        ItemType = WorkshopItemType.File,
                    };
                }
                else
                {
                    Result = new WorkshopSearchResult()
                    {
                        Items = new WorkshopItem[0],
                        Page = 0,
                        PageCount = 0,
                        TotalItemCount = 0,
                        Search = Parameters,
                        ItemType = WorkshopItemType.File,
                    };
                }
            }
            catch (Exception e)
            {
                Error = e.ToString();
            }

            Finished = true;
            yield break;
        }

        public System.Collections.IEnumerator RetrieveCollections()
        {
            var url = Parameters.ConstructUrl();

            var request = new UnityWebRequest(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.method = UnityWebRequest.kHttpVerbGET;

            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Finished = true;
                Error = request.error;
                yield break;
            }

            try
            {

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(request.downloadHandler.text);

                var itemNodes = htmlDoc.DocumentNode.SelectNodes(@".//div[contains(concat(' ', @class, ' '), ' workshopItemCollection ')]");

                if (itemNodes != null && itemNodes.Count > 0)
                {
                    WorkshopItem[] items = new WorkshopItem[itemNodes.Count];

                    var index = 0;
                    foreach (var itemNode in itemNodes)
                    {
                        var appId = itemNode.GetAttributeValue("data-appid", "");
                        var fileId = itemNode.GetAttributeValue("data-publishedfileid", "");
                        var image = itemNode.SelectSingleNode(@".//img[contains(@class, 'workshopItemPreviewImage')]").GetAttributeValue("src", "");
                        var authorNode = itemNode.SelectSingleNode(@".//span[contains(@class, 'workshopItemAuthorName')]").SelectSingleNode("a");
                        var authorName = authorNode.InnerText;
                        var itemTitle = itemNode.SelectSingleNode(@".//div[contains(@class, 'workshopItemTitle')]").InnerText;
                        var ratingNode = itemNode.SelectSingleNode(@".//img[contains(@class, 'fileRating')]");
                        var rating = -1;
                        if (ratingNode != null)
                        {
                            var ratingSrc = ratingNode.GetAttributeValue("src", "");
                            var ratingMatch = Regex.Match(ratingSrc, @"(\d+)-star");
                            if (ratingMatch.Success)
                            {
                                rating = int.Parse(ratingMatch.Groups[1].ToString());
                            }
                        }
                        items[index] = new WorkshopItem()
                        {
                            AppId = appId,
                            PublishedFileId = fileId,
                            AuthorName = authorName,
                            ItemName = itemTitle,
                            FileImageUrl = image,
                            Rating = rating,
                            ItemType = WorkshopItemType.Collection,
                        };
                        index++;
                    }

                    var entryCountNode = htmlDoc.DocumentNode.SelectSingleNode(@".//div[contains(@class, 'workshopBrowsePagingInfo')]");
                    var entryCountMatch = Regex.Match(entryCountNode.InnerText, @"Showing [\d,]+-[\d,]+ of ([\d,]+) entries");
                    var entryCountStr = entryCountMatch.Groups[1].ToString().Replace(",", "");
                    var entryCount = int.Parse(entryCountStr);

                    var pagingNode = htmlDoc.DocumentNode.SelectSingleNode(@".//div[contains(@class, 'workshopBrowsePagingControls')]");
                    var currentPage = 1;
                    var lastPage = 1;

                    if (pagingNode != null)
                    {
                        var currentPageNode = pagingNode.ChildNodes.FirstOrDefault(node =>
                        {
                            return Regex.Match(node.OuterHtml, @"(?:&nbsp;)+\d+(?:&nbsp;)+").Success;
                        });
                        if (currentPageNode != null)
                        {
                            currentPage = int.Parse(currentPageNode.InnerText.Replace(",", "").Replace("&nbsp;", ""));
                            var pageNodes = pagingNode.SelectNodes(@".//a[contains(@class, 'pagelink')]");
                            lastPage = currentPage;
                            if (pageNodes != null && pageNodes.Count > 0)
                            {
                                var lastPageNode = pageNodes[pageNodes.Count - 1];
                                var lastPagePossible = int.Parse(lastPageNode.InnerText.Replace(",", ""));
                                if (lastPagePossible > currentPage)
                                {
                                    lastPage = lastPagePossible;
                                }
                            }
                        }
                    }

                    Result = new WorkshopSearchResult()
                    {
                        Items = items,
                        Page = currentPage,
                        PageCount = lastPage,
                        TotalItemCount = entryCount,
                        Search = Parameters,
                        ItemType = WorkshopItemType.Collection,
                    };
                }
                else
                {
                    Result = new WorkshopSearchResult()
                    {
                        Items = new WorkshopItem[0],
                        Page = 0,
                        PageCount = 0,
                        TotalItemCount = 0,
                        Search = Parameters,
                        ItemType = WorkshopItemType.Collection,
                    };
                }
            }
            catch (Exception e)
            {
                Error = e.ToString();
            }

            Finished = true;
            yield break;
        }

        public System.Collections.IEnumerator RetrieveFiles()
        {
            var url = Parameters.ConstructUrl();

            Log.Debug($"Url: {url}");
            var request = new UnityWebRequest(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.method = UnityWebRequest.kHttpVerbGET;

            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Finished = true;
                Error = request.error;
                yield break;
            }

            try
            {
                Log.Debug($"Result:\n{request.downloadHandler.text}");

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(request.downloadHandler.text);

                var globalUserName = htmlDoc.DocumentNode.SelectSingleNode(@".//div[contains(concat(' ', @class, ' '), ' HeaderUserInfoName ')]")?.SelectSingleNode(@".//a")?.InnerText;

                var itemNodes = htmlDoc.DocumentNode.SelectNodes(@".//div[contains(concat(' ', @class, ' '), ' workshopItem ')]");

                if (itemNodes != null && itemNodes.Count > 0)
                {
                    WorkshopItem[] items = new WorkshopItem[itemNodes.Count];

                    var index = 0;
                    foreach (var itemNode in itemNodes)
                    {
                        var infoNode = itemNode.SelectSingleNode(@".//a[contains(@class, 'ugc')]");
                        var appId = infoNode.GetAttributeValue("data-appid", "");
                        var fileId = infoNode.GetAttributeValue("data-publishedfileid", "");
                        var image = itemNode.SelectSingleNode(@".//img[contains(@class, 'workshopItemPreviewImage')]").GetAttributeValue("src", "");
                        var authorNode = itemNode.SelectSingleNode(@".//div[contains(@class, 'workshopItemAuthorName')]")?.SelectSingleNode("a");
                        var authorName = authorNode?.InnerText ?? globalUserName;
                        var itemTitle = itemNode.SelectSingleNode(@".//div[contains(@class, 'workshopItemTitle')]").InnerText;
                        var ratingSrc = itemNode.SelectSingleNode(@".//img[contains(@class, 'fileRating')]").GetAttributeValue("src", "");
                        var ratingMatch = Regex.Match(ratingSrc, @"(\d+)-star");
                        var rating = -1;
                        if (ratingMatch.Success)
                        {
                            rating = int.Parse(ratingMatch.Groups[1].ToString());
                        }
                        items[index] = new WorkshopItem()
                        {
                            AppId = appId,
                            PublishedFileId = fileId,
                            AuthorName = authorName,
                            ItemName = itemTitle,
                            FileImageUrl = image,
                            Rating = rating,
                            ItemType = WorkshopItemType.File,
                        };
                        index++;
                    }

                    var entryCountNode = htmlDoc.DocumentNode.SelectSingleNode(@".//div[contains(@class, 'workshopBrowsePagingInfo')]");
                    var entryCountMatch = Regex.Match(entryCountNode.InnerText, @"Showing [\d,]+-[\d,]+ of ([\d,]+) entries");
                    var entryCountStr = entryCountMatch.Groups[1].ToString().Replace(",", "");
                    var entryCount = int.Parse(entryCountStr);

                    var pagingNode = htmlDoc.DocumentNode.SelectSingleNode(@".//div[contains(@class, 'workshopBrowsePagingControls')]");
                    var currentPage = 1;
                    var lastPage = 1;

                    if (pagingNode != null)
                    {
                        var currentPageNode = pagingNode.ChildNodes.FirstOrDefault(node =>
                        {
                            return Regex.Match(node.OuterHtml, @"(?:&nbsp;)+\d+(?:&nbsp;)+").Success;
                        });
                        if (currentPageNode != null)
                        {
                            currentPage = int.Parse(currentPageNode.InnerText.Replace(",", "").Replace("&nbsp;", ""));
                            var pageNodes = pagingNode.SelectNodes(@".//a[contains(@class, 'pagelink')]");
                            lastPage = currentPage;
                            if (pageNodes != null && pageNodes.Count > 0)
                            {
                                var lastPageNode = pageNodes[pageNodes.Count - 1];
                                var lastPagePossible = int.Parse(lastPageNode.InnerText.Replace(",", ""));
                                if (lastPagePossible > currentPage)
                                {
                                    lastPage = lastPagePossible;
                                }
                            }
                        }
                    }

                    Result = new WorkshopSearchResult()
                    {
                        Items = items,
                        Page = currentPage,
                        PageCount = lastPage,
                        TotalItemCount = entryCount,
                        Search = Parameters,
                        ItemType = WorkshopItemType.File,
                    };
                }
                else
                {
                    Result = new WorkshopSearchResult()
                    {
                        Items = new WorkshopItem[0],
                        Page = 0,
                        PageCount = 0,
                        TotalItemCount = 0,
                        Search = Parameters,
                        ItemType = WorkshopItemType.File,
                    };
                }
            }
            catch (Exception e)
            {
                Error = e.ToString();
            }

            Finished = true;
            yield break;
        }
    }
}
