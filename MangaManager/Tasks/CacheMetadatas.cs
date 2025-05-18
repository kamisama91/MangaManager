using MangaManager.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace MangaManager.Tasks
{
    public class CacheMetadatas
    {
        static CacheMetadatas()
        {
            _dataFolder = !string.IsNullOrEmpty(Program.Options.DataFolder) && Directory.Exists(Program.Options.DataFolder)
                            ? Program.Options.DataFolder
                            : Path.Combine(Path.GetDirectoryName(typeof(CacheMetadatas).Assembly.Location), "data");

            _keywords = new Lazy<ConcurrentBag<Keyword>>(JsonConvert.DeserializeObject<ConcurrentBag<Keyword>>(File.ReadAllText(Path.Combine(_dataFolder, "KeywordsSettings.json"))));
            _groups = new Lazy<ConcurrentBag<Group>>(JsonConvert.DeserializeObject<ConcurrentBag<Group>>(File.ReadAllText(Path.Combine(_dataFolder, "GroupSettings.json"))));
            _series = new Lazy<ConcurrentBag<Serie>>(JsonConvert.DeserializeObject<ConcurrentBag<Serie>>(File.ReadAllText(Path.Combine(_dataFolder, "ComicInfoDb.json"))));
        }

        private static string _dataFolder;

        private static Lazy<ConcurrentBag<Keyword>> _keywords;
        public static ConcurrentBag<Keyword> Keywords => _keywords.Value;

        private static Lazy<ConcurrentBag<Group>> _groups;
        public static ConcurrentBag<Group> Groups => _groups.Value;

        private static Lazy<ConcurrentBag<Serie>> _series;
        public static ConcurrentBag<Serie> Series => _series.Value;

        public static void SaveSeries()
        {
            lock(_series)
            {
                File.WriteAllText(Path.Combine(_dataFolder, "ComicInfoDb.json"), JsonConvert.SerializeObject(Series.OrderBy(s => s.Name), Formatting.Indented));
            }
        }
    }
}
