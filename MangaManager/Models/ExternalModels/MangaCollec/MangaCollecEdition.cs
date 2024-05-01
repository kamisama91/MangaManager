using Newtonsoft.Json;

namespace MangaManager.Models.ExternalModels.MangaCollec
{
    public class MangaCollecEdition
    {
        [JsonProperty("id")]
        public string Id { get; init; }

        [JsonProperty("title")]
        public string Title { get; init; }

        [JsonProperty("series_id")]
        public string SeriesId { get; init; }

        [JsonProperty("publisher_id")]
        public string PublisherId { get; init; }

        [JsonProperty("publisher")]
        public MangaCollecPublisher Publisher { get; init; }

        [JsonProperty("volumes_count")]
        public int VolumesCount { get; init; }

        [JsonProperty("last_volume_number")]
        public int? LastVolumeNumber { get; init; }

        [JsonProperty("commercial_stop")]
        public bool CommercialStop { get; init; }

        [JsonProperty("not_finished")]
        public bool NotFinished { get; init; }

        [JsonProperty("volumes")]
        public MangaCollecVolume[] Volumes { get; init; }
    }
}