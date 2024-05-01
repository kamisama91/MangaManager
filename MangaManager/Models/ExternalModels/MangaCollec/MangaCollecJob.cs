using Newtonsoft.Json;

namespace MangaManager.Models.ExternalModels.MangaCollec
{
    public class MangaCollecJob
    {
        [JsonProperty("id")]
        public string Id { get; init; }

        [JsonProperty("title")]
        public string Title { get; init; }
    }
}