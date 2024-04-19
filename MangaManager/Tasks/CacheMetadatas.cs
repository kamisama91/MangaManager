using MangaManager.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace MangaManager.Tasks
{
    public class CacheMetadatas
    {
        private static string _dataFolder = Path.Combine(Path.GetDirectoryName(typeof(CacheMetadatas).Assembly.Location), "data");

        private static Lazy<List<Keyword>> _keywords = new Lazy<List<Keyword>>(JsonConvert.DeserializeObject<List<Keyword>>(File.ReadAllText(Path.Combine(_dataFolder, "KeywordsSettings.json"))));
        public static List<Keyword> Keywords => _keywords.Value;

        private static Lazy<List<Group>> _groups = new Lazy<List<Group>>(JsonConvert.DeserializeObject<List<Group>>(File.ReadAllText(Path.Combine(_dataFolder, "GroupSettings.json"))));
        public static List<Group> Groups => _groups.Value;

        private static Lazy<List<Serie>> _series = new Lazy<List<Serie>>(JsonConvert.DeserializeObject<List<Serie>>(File.ReadAllText(Path.Combine(_dataFolder, "ComicInfoDb.json"))));
        public static List<Serie> Series => _series.Value;
    }
}
