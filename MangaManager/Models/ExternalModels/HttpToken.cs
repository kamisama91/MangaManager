using Newtonsoft.Json;

namespace MangaManager.Models.ExternalModels
{
    public class HttpToken
    {
        [JsonProperty("token_type")]
        public string TokenType { get; init; }

        [JsonProperty("created_at")]
        public int CreatedAt { get; init; }

        [JsonProperty("expires_in")]
        public int ExpriresIn { get; init; }

        [JsonProperty("access_token")]
        public string AccessToken { get; init; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; init; }
    }
}