using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace MangaManager.Models
{
    public class Serie
    {
        [JsonProperty("Alias")]
        public string Alias { get; init; }

        [JsonProperty("Name")]
        public string Name { get; init; }

        [JsonProperty("Edition")]
        public string Edition { get; init; }

        [JsonProperty("Writer")]
        public string Writer { get; init; }

        [JsonProperty("Penciler")]
        public string Penciler { get; init; }

        [JsonProperty("Publisher")]
        public string Publisher { get; init; }

        [JsonProperty("Keywords", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Keywords { get; init; }

        [JsonProperty("LastVolume")]
        public int? LastVolume { get; init; }

        [JsonProperty("IsInterrupted")]
        public bool IsInterrupted { get; init; }

        [JsonProperty("Volumes", NullValueHandling = NullValueHandling.Ignore)]
        public List<Volume> Volumes { get; init; }

        [JsonProperty("Id")]
        public string MangaCollecSerieId { get; init; }

        [JsonProperty("EditionId")]
        public string MangaCollecEditionId { get; init; }

        [JsonProperty("LastUpdateDate")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime LastUpdateDate { get; init; }
    }
}
