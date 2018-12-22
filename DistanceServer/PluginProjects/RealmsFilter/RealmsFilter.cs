    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RealmsFilter
{
    public class RealmsFilter : DistanceServerPlugin
    {
        public override string Author => "Corecii; Discord: Corecii#3019";
        public override string DisplayName => "RealmsFilter";
        public override int Priority => 0;
        public override SemanticVersion ServerVersion => new SemanticVersion("0.1.3");

        public enum FilterModeType
        {
            NoRealms,
            RealmsOnly,
            GoodEasyOnly,
        }
        public FilterModeType FilterMode = FilterModeType.NoRealms;

        public void ReadSettings()
        {
            var filePath = new System.IO.FileInfo(Manager.ServerDirectory.FullName + "/RealmsFilter.json");
            if (!filePath.Exists)
            {
                Log.Info("No RealmsFilter.json, using default settings");
                return;
            }
            try
            {
                var txt = System.IO.File.ReadAllText(filePath.FullName);
                var reader = new JsonFx.Json.JsonReader();
                var dictionary = (Dictionary<string, object>)reader.Read(txt);
                string filterMode = null;
                TryGetValue(dictionary, "FilterMode", ref filterMode);
                Log.Debug($"filterMode: {filterMode}");
                if (filterMode != null)
                {
                    FilterMode = (FilterModeType)Enum.Parse(typeof(FilterModeType), filterMode);
                }
                Log.Info("Loaded settings from RealmsFilter.json");
            }
            catch (Exception e)
            {
                Log.Error($"Couldn't read BasicAutoServer.json. Is your json malformed?\n{e}");

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
            ReadSettings();
            Manager.GetPlugin<BasicAutoServer.BasicAutoServer>().AddWorkshopFilter(results =>
            {
                results.RemoveAll(result =>
                {
                    if (FilterMode != FilterModeType.GoodEasyOnly)
                    {
                        var isRealm = Regex.IsMatch(result.DistanceLevelResult.Name.ToLower(), "acceleracer|realm|hot.wheel");
                        if (FilterMode == FilterModeType.RealmsOnly)
                        {
                            isRealm = !isRealm;
                            isRealm = isRealm || result.WorkshopItemResult.Rating < 4 || Regex.IsMatch(result.DistanceLevelResult.Name.ToLower(), "old|lamp|meme");
                        }
                        return isRealm;
                    }
                    else
                    {
                        return result.WorkshopItemResult.Rating < 4;
                    }
                });
                return true;
            });
        }
    }
}
