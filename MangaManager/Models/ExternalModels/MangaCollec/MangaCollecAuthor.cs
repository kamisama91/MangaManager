using Newtonsoft.Json;

namespace MangaManager.Models.ExternalModels.MangaCollec
{
    public class MangaCollecAuthor
    {
        [JsonProperty("id")]
        public string Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("first_name")]
        public string FirstName { get; init; }
    }
}