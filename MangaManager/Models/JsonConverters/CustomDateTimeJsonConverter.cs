using Newtonsoft.Json.Converters;

namespace MangaManager.Models.JsonConverters
{
    public class CustomDateTimeJsonConverter : IsoDateTimeConverter
    {
        public CustomDateTimeJsonConverter()
        {
            DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        }
    }
}