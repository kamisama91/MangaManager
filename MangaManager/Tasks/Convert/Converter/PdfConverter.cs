using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UglyToad.PdfPig;

namespace MangaManager.Tasks.Convert.Converter
{
    public class PdfConverter : IFileProvider, IFileProcessor
    {
        private List<string> _acceptdExtensions = new List<string> { ".pdf" };

        public string[] GetFiles()
        {
            return _acceptdExtensions.SelectMany(extension => Directory.EnumerateFiles(Program.Options.SourceFolder, $"*{extension}", SearchOption.AllDirectories)).ToArray();
        }

        public bool Accept(string file)
        {
            return _acceptdExtensions.Contains(Path.GetExtension(file));
        }

        private IEnumerable<ArchiveItemStream> GetArchiveItemStreams(string file)
        {
            var sourceDocument = PdfDocument.Open(file);
            return sourceDocument.GetPages()
                .SelectMany(page => page.GetImages())
                .Select(image =>
                {
                    var ms = new MemoryStream(image.RawBytes.ToArray());
                    if (!ms.TryGetImageExtension(out var extension)) { throw new FormatException(); }
                    return new ArchiveItemStream { Stream = ms, Extension = extension };
                });
        }

        public bool ProcessFile(string file, out string newFile)
        {
            var outputPath = FileHelper.GetAvailableFilename(Path.Combine(Path.GetDirectoryName(file), $"{Path.GetFileNameWithoutExtension(file)}.cbz"));
            var isSuccess = ArchiveHelper.CreateZipFromArchiveItemStreams(file, outputPath, GetArchiveItemStreams);
            if (isSuccess)
            {
                File.Delete(file);
            }
            newFile = outputPath;
            return isSuccess;
        }
    }
}