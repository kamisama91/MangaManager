using System.IO;
using System.Xml.Serialization;

namespace MangaManager.Models
{
    public class ComicInfo
    {
        public static ComicInfo Empty { get; } = new ComicInfo();

        public const string NAME = "ComicInfo.xml";

        public string Title { get; set; }
        public string Series { get; set; }
        public string Number { get; set; }
        public string Imprint { get; set; }
        public string SeriesGroup { get; set; }
        public string Summary { get; set; }
        public string Count { get; set; }
        public string Writer { get; set; }
        public string Penciller { get; set; }
        public string Publisher { get; set; }
        public string Genre { get; set; }
        public string Tags { get; set; }
        public string AgeRating { get; set; }
        public string Year { get; set; }
        public string Month { get; set; }
        public string Day { get; set; }
        public string LanguageISO { get; set; } = "fr";
        public string Manga { get; set; } = "Yes";        

        public static ComicInfo FromXmlStream (MemoryStream inputStream)
        {
            inputStream.Position = 0;
            var serializer = new XmlSerializer(typeof(ComicInfo));
            var reader = new StreamReader(inputStream);
            var comicInfo = serializer.Deserialize(reader) as ComicInfo;
            inputStream.Position = 0;
            return comicInfo;
        }

        public MemoryStream ToXmlStream()
        {
            var outputStream = new MemoryStream();
            var serializer = new XmlSerializer(typeof(ComicInfo));
            var writer = new StreamWriter(outputStream);
            serializer.Serialize(writer, this);
            outputStream.Position = 0;
            return outputStream;
        }
    }
}