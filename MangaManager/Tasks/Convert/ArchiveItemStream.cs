using System.IO;

namespace MangaManager.Tasks.Convert
{
    public class ArchiveItemStream
    {
        public MemoryStream Stream { get; set; }
        public string SourceFileName { get; set; }
        public string TargetFileName { get; set; }
        public string TargetExtension { get; set; }

        public void Clear()
        {
            Stream?.Dispose();
            Stream = null;
            SourceFileName = null;
            TargetFileName = null;
            TargetExtension = null;
        }

        public static implicit operator MemoryStream(ArchiveItemStream i) => i.Stream;
    }
}