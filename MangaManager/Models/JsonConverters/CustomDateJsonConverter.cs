using Newtonsoft.Json.Converters;

namespace MangaManager.Models.JsonConverters
{
    public class CustomDateJsonConverter : IsoDateTimeConverter
    {
        public CustomDateJsonConverter()
        {
            DateTimeFormat = "yyyy-MM-dd";
        }
    }
}
