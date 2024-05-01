using Newtonsoft.Json;

namespace MangaManager.Models.ExternalModels.MangaCollec
{
    public class MangaCollecSerie
    {
        [JsonProperty("id")]
        public string Id { get; init; }

        [JsonProperty("title")]
        public string Title { get; init; }

        [JsonProperty("type_id")]
        public string TypeId { get; init; }

        [JsonProperty("adult_content")]
        public bool AdultContent { get; init; }

        [JsonProperty("editions_count")]
        public int EditionsCount { get; init; }
    }
}