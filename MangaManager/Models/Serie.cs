using MangaManager.Models.JsonConverters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MangaManager.Models
{
    public class Serie
    {
        [JsonProperty("Alias")]
        public string Alias { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Edition")]
        public string Edition { get; set; }

        [JsonProperty("Writer")]
        public string Writer { get; set; }

        [JsonProperty("Penciler")]
        public string Penciler { get; set; }

        [JsonProperty("Publisher")]
        public string Publisher { get; set; }

        [JsonProperty("Keywords", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Keywords { get; set; }

        [JsonProperty("LastVolume")]
        public int? LastVolume { get; set; }

        [JsonProperty("IsInterrupted")]
        public bool IsInterrupted { get; set; }

        [JsonProperty("Volumes", NullValueHandling = NullValueHandling.Ignore)]
        public List<Volume> Volumes { get; set; }

        [JsonProperty("Id")]
        public string MangaCollecSerieId { get; set; }

        [JsonProperty("EditionId")]
        public string MangaCollecEditionId { get; set; }

        [JsonProperty("LastUpdateDate")]
        [JsonConverter(typeof(CustomDateJsonConverter))]
        public DateTime LastUpdateDate { get; set; }
    }
}
