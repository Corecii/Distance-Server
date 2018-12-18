using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkshopSearch
{
    public class WorkshopSearchParameters
    {
        public string AppId = ""; //?appid=233610
        public SearchTypeType SearchType = SearchTypeType.GameFiles; //?section=
        public string SearchText = null; //&searchtext=text
        public SortType Sort = SortType.Default; //&browsesort=trend
        public FilterType Filter = FilterType.Default; //&browsefilter=
        public int Days = -1; //&days=7
        public string[] RequiredTags = new string[0];//&requiredtags[]=Sprint&requiredtags[]=Reverse+Tag
        public int Page = 1; //&p=1
        public int NumPerPage = 30; //&numperpage=30 //only support 9, 18, and 30

        public string UserId = null;

        public SortMethod Sort2 = SortMethod.Default; //&sortmethod=

        public string CollectionFileId = null; //?id=

        public const int DaysAllTime = -1;
        public const string GameSearchUrl = "https://steamcommunity.com/workshop/browse/";
        public const string UserSearchUrl = "https://steamcommunity.com/profiles/{0}/myworkshopfiles/";
        public const string CollectionFilesUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id={0}";

        public static string[] emptyTags = new string[0];

        public static WorkshopSearchParameters GameFiles(string appId, string searchText = "", SortType sort = SortType.Default, FilterType filter = FilterType.Default, int days = DaysAllTime, string[] requiredTags = null, int page = 1, int numPerPage = 30)
        {
            return new WorkshopSearchParameters()
            {
                SearchType = SearchTypeType.GameFiles,
                AppId = appId,
                SearchText = searchText,
                Sort = sort,
                Filter = filter,
                Days = days,
                RequiredTags = requiredTags ?? emptyTags,
                Page = page,
                NumPerPage = numPerPage,
            };
        }
        public static WorkshopSearchParameters UserFiles(string appId, string userId = "", bool favorites = false, string[] requiredTags = null, int page = 1, int numPerPage = 30)
        {
            return new WorkshopSearchParameters()
            {
                SearchType = SearchTypeType.GameFiles,
                AppId = appId,
                UserId = userId,
                Sort = SortType.Default,
                Filter = favorites ? FilterType.MyFavorites : FilterType.Files,
                RequiredTags = requiredTags ?? emptyTags,
                Page = page,
                NumPerPage = numPerPage,
            };
        }

        public static WorkshopSearchParameters GameCollections(string appId, string searchText = "", SortType sort = SortType.Default, FilterType filter = FilterType.Default, int days = DaysAllTime, string[] requiredTags = null, int page = 1, int numPerPage = 30)
        {
            return new WorkshopSearchParameters()
            {
                SearchType = SearchTypeType.GameCollections,
                AppId = appId,
                SearchText = searchText,
                Sort = sort,
                Filter = filter,
                Days = days,
                RequiredTags = requiredTags ?? emptyTags,
                Page = page,
                NumPerPage = numPerPage,
            };
        }
        public static WorkshopSearchParameters UserCollections(string appId, string userId = "", bool favorites = false, string[] requiredTags = null, int page = 1, int numPerPage = 30)
        {
            return new WorkshopSearchParameters()
            {
                SearchType = SearchTypeType.UserCollections,
                AppId = appId,
                UserId = userId,
                Sort = SortType.Default,
                Filter = favorites ? FilterType.MyFavorites : FilterType.Files,
                RequiredTags = requiredTags ?? emptyTags,
                Page = page,
                NumPerPage = numPerPage,
            };
        }

        public static WorkshopSearchParameters CollectionFiles(string collectionFileId)
        {
            return new WorkshopSearchParameters()
            {
                SearchType = SearchTypeType.CollectionFiles,
                CollectionFileId = collectionFileId,
            };
        }

        public WorkshopSearchRetriever Search(bool startCoroutine = true)
        {
            return Workshop.Search(this, startCoroutine);
        }

        public string ConstructUrl()
        {
            string searchUrl = "";
            switch (SearchType)
            {
                case SearchTypeType.GameFiles:
                case SearchTypeType.GameCollections:
                    searchUrl = GameSearchUrl;
                    break;
                case SearchTypeType.UserFiles:
                case SearchTypeType.UserCollections:
                    searchUrl = string.Format(UserSearchUrl, UserId);
                    break;
                case SearchTypeType.CollectionFiles:
                    searchUrl = string.Format(CollectionFilesUrl, CollectionFileId);
                    return searchUrl;
            }

            searchUrl += $"?appid={AppId}";

            switch (SearchType)
            {
                case SearchTypeType.GameCollections:
                case SearchTypeType.UserCollections:
                    searchUrl += $"&section=collections";
                    break;
                case SearchTypeType.GameFiles:
                    searchUrl += $"&section=readytouseitems";
                    break;
            }

            if (SearchText != null)
            {
                searchUrl += $"&searchtext={UnityEngine.WWW.EscapeURL(SearchText)}";
            }

            string sort = null;
            switch (Sort)
            {
                case SortType.Popular:
                    sort = "trend";
                    break;
                case SortType.Playtime:
                    sort = "playtime_trend";
                    break;
                case SortType.Recent:
                    sort = "mostrecent";
                    break;
                case SortType.Subscribed:
                    sort = "totaluniquesubscribers";
                    break;
                case SortType.Relevance:
                    sort = "textsearch";
                    break;
            }
            if (sort != null)
            {
                searchUrl += $"&browsesort={sort}";
            }

            string sort2 = null;
            switch (Sort2)
            {
                case SortMethod.Date:
                    sort2 = "subscriptiondate";
                    break;
                case SortMethod.Alphabetically:
                    sort2 = "alpha";
                    break;
                case SortMethod.Updated:
                    sort2 = "lastupdated";
                    break;
                case SortMethod.Created:
                    sort2 = "creationorder";
                    break;
            }
            if (sort2 != null)
            {
                searchUrl += $"&sortmethod={sort2}";
            }

            string filter = null;
            switch (Filter)
            {
                case FilterType.Trend:
                    filter = "trend";
                    break;
                case FilterType.YourFavorites:
                    filter = "yourfavorites";
                    break;
                case FilterType.FriendFavorites:
                    filter = "favoritedbyfriends";
                    break;
                case FilterType.FriendItems:
                    filter = "createdbyfriends";
                    break;
                case FilterType.Following:
                    filter = "createdbyfollowed";
                    break;
                case FilterType.Files:
                    filter = "myfiles";
                    break;
                case FilterType.MyFavorites:
                    filter = "myfavorites";
                    break;
                case FilterType.Played:
                    filter = "myplayedfiles";
                    break;
                case FilterType.Subscribed:
                    filter = "mysubscriptions";
                    break;
            }
            if (filter != null)
            {
                searchUrl += $"&browsefilter=filter";
            }

            if (Days != 0)
            {
                searchUrl += $"&days={Days}";
            }

            if (Page != 0)
            {
                searchUrl += $"&p={Page}";
            }

            foreach (var tag in RequiredTags)
            {
                searchUrl += $"&requiredtags[]={tag}";
            }

            if (NumPerPage != 0)
            {
                searchUrl += $"&numperpage={NumPerPage}";
            }

            return searchUrl;
        }

        public static string TagToString(Tag tag)
        {
            switch (tag)
            {
                case Tag.Casual: return "Casual";
                case Tag.Normal: return "Normal";
                case Tag.Advanced: return "Advanced";
                case Tag.Expert: return "Expert";
                case Tag.Nightmare: return "Nightmare";
                case Tag.Sprint: return "Sprint";
                case Tag.ReverseTag: return "Reverse+Tag";
                case Tag.Challenge: return "Challenge";
                case Tag.Stunt: return "Stunt";
                case Tag.Trackmogrify: return "Trackmogrify";
                case Tag.MainMenu: return "Main+Menu";
                case Tag.SurviveTheEditor: return "Survive+the+Editor";
                case Tag.DistanceAdventCalendar2014: return "Distance+Advent+Calendar+2014";
                case Tag.DistanceAdventCalendar2015: return "Distance+Advent+Calendar+2015";
                case Tag.DistanceAdventCalendar2016: return "Distance+Advent+Calendar+2016";
                case Tag.DistanceAdventCalendar2017: return "Distance+Advent+Calendar+2017";
            }
            return null;
        }
        public static string[] TagsToString(Tag[] tags)
        {
            string[] tagsString = new string[tags.Length];
            int index = 0;
            foreach (var tag in tags)
            {
                tagsString[index] = TagToString(tag);
                index++;
            }
            return tagsString;
        }

        public enum SearchTypeType
        {
            //https://steamcommunity.com/workshop/browse/
            GameFiles, //?section=readytouseitems
            GameCollections, //?section=collections
            //https://steamcommunity.com/profiles/76561198041668285/myworkshopfiles/
            UserFiles, //?appid=###
            UserCollections, //?section=collections&appid=###
            //https://steamcommunity.com/sharedfiles/filedetails/?id=686854308
            CollectionFiles,
        }
        public enum Tag
        {
            Casual, //"Casual"
            Normal, //"Normal"
            Advanced, //"Advanced"
            Expert, //"Expert"
            Nightmare, //"Nightmare"

            Sprint, //"Sprint"
            ReverseTag, //"Reverse+Tag"
            Challenge, //"Challenge"
            Stunt, //"Stunt"
            Trackmogrify, //"Trackmogrify"
            MainMenu, //"Main+Menu"

            SurviveTheEditor, //"Survive+the+Editor"
            DistanceAdventCalendar2014, //"Distance+Advent+Calendar+2014"
            DistanceAdventCalendar2015, //"Distance+Advent+Calendar+2015"
            DistanceAdventCalendar2016, //"Distance+Advent+Calendar+2016"
            DistanceAdventCalendar2017, //"Distance+Advent+Calendar+2017"
        }
        public enum SortType
        {
            Default,
            Popular, // "trend"
            Playtime, //"playtime_trend"
            Recent, //"mostrecent"
            Subscribed, //"mostuniquesubscribers"
            Relevance, //"textsearch"
        }
        public enum FilterType
        {
            Default,
            //when looking items or collections
            Trend, //"trend"
            YourFavorites, //"yourfavorites"
            FriendFavorites, //"favoritedbyfriends"
            FriendItems, //"createdbyfriends"
            Following, //"createdbyfollowed"

            //when looking at a user's items
            Files, //"myfiles"
            MyFavorites, //"myfavorites"

            //when looking at your own workshop page
            Played, //"myplayedfiles"
            Subscribed, //"mysubscriptions"
        }
        public enum SortMethod  //only works when looking at a user's favorited items
        {
            Default,
            Date, //"subscriptiondate"
            Alphabetically, //"alpha"
            Updated, //"lastupdated"
            Created, //"creationorder"
        }
    }
}
