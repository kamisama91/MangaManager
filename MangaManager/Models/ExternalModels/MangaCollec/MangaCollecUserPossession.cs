using Newtonsoft.Json;

namespace MangaManager.Models.ExternalModels.MangaCollec
{
    public class MangaCollecUserPossession
    {
        [JsonProperty("volume_id")]
        public string VolumeId { get; init; }
    }
}