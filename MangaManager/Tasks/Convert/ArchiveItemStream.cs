using System.IO;

namespace MangaManager.Tasks.Convert
{
    public class ArchiveItemStream
    {
        public Stream Stream { get; set; }
        public string FileName { get; set; }
        public string Extension { get; set; }

        public void Clear()
        {
            Stream?.Dispose();
            Stream = null;
            FileName = null;
            Extension = null;
        }

        public static implicit operator Stream(ArchiveItemStream i) => i.Stream;
    }
}