using Newtonsoft.Json;
using System;

namespace MangaManager.Models.ExternalModels.MangaCollec
{
    public class MangaCollecVolumeDetail : MangaCollecVolume
    {
        [JsonProperty("content")]
        public string Content { get; init; }

        [JsonProperty("nb_pages")]
        public int? NbPages { get; init; }

    }
}