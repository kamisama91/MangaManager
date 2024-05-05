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
        private static string _dataFolder = Path.Combine(Path.GetDirectoryName(typeof(CacheMetadatas).Assembly.Location), "data");

        private static Lazy<ConcurrentBag<Keyword>> _keywords = new Lazy<ConcurrentBag<Keyword>>(JsonConvert.DeserializeObject<ConcurrentBag<Keyword>>(File.ReadAllText(Path.Combine(_dataFolder, "KeywordsSettings.json"))));
        public static ConcurrentBag<Keyword> Keywords => _keywords.Value;

        private static Lazy<ConcurrentBag<Group>> _groups = new Lazy<ConcurrentBag<Group>>(JsonConvert.DeserializeObject<ConcurrentBag<Group>>(File.ReadAllText(Path.Combine(_dataFolder, "GroupSettings.json"))));
        public static ConcurrentBag<Group> Groups => _groups.Value;

        private static Lazy<ConcurrentBag<Serie>> _series = new Lazy<ConcurrentBag<Serie>>(JsonConvert.DeserializeObject<ConcurrentBag<Serie>>(File.ReadAllText(Path.Combine(_dataFolder, "ComicInfoDb.json"))));
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
