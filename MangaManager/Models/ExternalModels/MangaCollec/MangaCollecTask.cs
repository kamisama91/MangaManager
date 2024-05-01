using Newtonsoft.Json;

namespace MangaManager.Models.ExternalModels.MangaCollec
{
    public class MangaCollecTask
    {
        [JsonProperty("id")]
        public string Id { get; init; }

        [JsonProperty("series_id")]
        public string SeriesId { get; init; }

        [JsonProperty("job_id")]
        public string JobId { get; init; }

        [JsonProperty("job")]
        public MangaCollecJob Job { get; init; }

        [JsonProperty("author_id")]
        public string AuthorId { get; init; }

        [JsonProperty("author")]
        public MangaCollecAuthor Author { get; init; }
    }
}