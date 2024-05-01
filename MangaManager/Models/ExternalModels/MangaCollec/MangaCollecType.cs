using Newtonsoft.Json;

namespace MangaManager.Models.ExternalModels.MangaCollec
{
    public class MangaCollecType
    {
        [JsonProperty("id")]
        public string Id { get; init; }

        [JsonProperty("title")]
        public string Title { get; init; }

        [JsonProperty("to_display")]
        public bool ToDisplay { get; init; }
    }
}