using MangaManager.Models.JsonConverters;
using Newtonsoft.Json;
using System;

namespace MangaManager.Models.ExternalModels.MangaCollec
{
    public class MangaCollecVolume
    {
        [JsonProperty("id")]
        public string Id { get; init; }

        [JsonProperty("edition_id")]
        public string EditionId { get; init; }

        [JsonProperty("title")]
        public string Title { get; init; }

        [JsonProperty("number")]
        public int Number { get; init; }

        [JsonProperty("release_date")]
        [JsonConverter(typeof(CustomDateJsonConverter))]
        public DateTime? ReleaseDate { get; init; }

        [JsonProperty("isbn")]
        public string ISBN { get; init; }

        [JsonProperty("not_sold")]
        public bool NotSold { get; init; }
    }
}