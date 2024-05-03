using MangaManager.Models.JsonConverters;
using Newtonsoft.Json;
using System;

namespace MangaManager.Models.ExternalModels.MangaCollec
{
    public class MangaCollecUserPossessionDetail : MangaCollecUserPossession
    {
        [JsonProperty("id")]
        public string Id { get; init; }

        [JsonProperty("user_id")]
        public string UserId { get; init; }

        [JsonProperty("created_at")]
        [JsonConverter(typeof(CustomDateTimeJsonConverter))]
        public DateTime CreatedAt { get; init; }
    }
}