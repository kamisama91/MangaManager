using SharpCompress;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MangaManager.Models
{
    public class RenameMap
    {
        public const string NAME = "RenameMap.csv";
        private const string HEADER = "\"Original Path\";\"New Path\"\r\n";

        public Dictionary<string, string> Map { get; private set; }
        
        public RenameMap(Dictionary<string, string> map)
        {
            Map = map;
        }

        public static RenameMap FromCsvStream (MemoryStream inputStream)
        {
            inputStream.Position = 0;
            var reader = new StreamReader(inputStream);
            var map = reader.ReadToEnd()
                .Split("\r\n", System.StringSplitOptions.RemoveEmptyEntries)
                .Skip(1) //header
                .Select(e => e.Split(";", 2))
                .ToDictionary (e => e[0].Trim('\"'), e => e[1].Trim('\"'));
            inputStream.Position = 0;
            return new RenameMap(map);
        }

        public MemoryStream ToCsvStream()
        {
            var outputStream = new MemoryStream();
            var writer = new StreamWriter(outputStream);
            writer.Write(HEADER);
            Map.Where(kv => !string.IsNullOrEmpty(kv.Value) && !kv.Value.Equals(kv.Key)).ForEach(kv => writer.Write($"\"{kv.Key}\";\"{kv.Value}\"\r\n"));
            writer.Flush();
            outputStream.Position = 0;
            return outputStream;
        }
    }
}