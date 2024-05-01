using Newtonsoft.Json;
using System;

namespace MangaManager.Models
{
    public class Volume
    {
        [JsonProperty("Number")]
        public int Number { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Summary")]
        public string Summary { get; set; }

        [JsonProperty("ISBN")]
        public string ISBN { get; set; }

        [JsonProperty("Date")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime ReleaseDate { get; set; }

        [JsonProperty("Id")]
        public string MangaCollecVolumeId { get; set; }
    }
}
