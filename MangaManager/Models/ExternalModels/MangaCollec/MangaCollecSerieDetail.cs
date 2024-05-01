using Newtonsoft.Json;

namespace MangaManager.Models.ExternalModels.MangaCollec
{
    public class MangaCollecSerieDetail : MangaCollecSerie
    {
        [JsonProperty("type")]
        public MangaCollecType Type { get; init; }

        [JsonProperty("tasks")]
        public MangaCollecTask[] Tasks { get; init; }

        [JsonProperty("editions")]
        public MangaCollecEdition[] Editions { get; init; }

        [JsonProperty("kinds")]
        public MangaCollecKind[] Kinds { get; init; }
    }
}