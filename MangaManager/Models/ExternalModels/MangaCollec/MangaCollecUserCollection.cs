using Newtonsoft.Json;

namespace MangaManager.Models.ExternalModels.MangaCollec
{
    public class MangaCollecUserCollection
    {
        [JsonProperty("possessions")]
        public MangaCollecUserPossessionDetail[] Possessions { get; init; }
    }
}