using Newtonsoft.Json;
using System;

namespace MangaManager.Models
{
    public class Volume
    {
        [JsonProperty("Number")]
        public int Number { get; init; }

        [JsonProperty("Name")]
        public string Name { get; init; }

        [JsonProperty("Summary")]
        public string Summary { get; init; }

        [JsonProperty("ISBN")]
        public string ISBN { get; init; }

        [JsonProperty("Date")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime ReleaseDate { get; init; }

        [JsonProperty("Id")]
        public string MangaCollecVolumeId { get; init; }
    }
}
