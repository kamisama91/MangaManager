using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

namespace MangaManager.Models
{
    public enum KeywordType
    {
        Tag,
        Genre
    }

    public class Keyword
    {
        [JsonProperty("title")]
        public string Title { get; init; }

        [JsonProperty("type", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(KeywordType.Tag)]
        public KeywordType Type { get; init; }

        [JsonProperty("rating", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Rating { get; init; }
    }
}
