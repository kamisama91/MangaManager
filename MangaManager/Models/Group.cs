using Newtonsoft.Json;
using System.Collections.Generic;

namespace MangaManager.Models
{
    public class Group
    {
        [JsonProperty("Serie")]
        public string Title { get; init; }

        [JsonProperty("Related", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> RelatedTitles { get; init; }
    }
}
