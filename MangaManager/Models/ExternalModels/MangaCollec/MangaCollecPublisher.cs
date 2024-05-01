using Newtonsoft.Json;

namespace MangaManager.Models.ExternalModels.MangaCollec
{
    public class MangaCollecPublisher
    {
        [JsonProperty("id")]
        public string Id { get; init; }

        [JsonProperty("title")]
        public string Title { get; init; }

        [JsonProperty("closed")]
        public bool Closed { get; init; }
    }
}