using System.IO;
using System.Linq;
using EpubCore;
using System.Xml.Linq;
using System.Collections.Generic;
using System;

namespace MangaManager.Tasks.Convert.Converter
{
    public class EpubConverter : IFileProvider, IFileProcessor
    {
        private List<string> _acceptdExtensions = new List<string> { ".epub" };

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
            var sourceDocument = EpubReader.Read(file);
            var imageElements = new[] { "img", "image" };
            var imageSrcAttributes = new[] { "src", "xlink:src", "href", "xlink:href" };
            return sourceDocument.Format.Opf.Spine.ItemRefs
                .Select(spine => sourceDocument.Format.Opf.Manifest.Items.Where(manifest => manifest.Id == spine.IdRef).Single())
                .Select(manifest => sourceDocument.Resources.Html.Where(resource => resource.Href == manifest.Href).Single())
                .SelectMany(html => XDocument.Parse(html.TextContent).DescendantNodes().OfType<XElement>().Where(element => imageElements.Contains(element.Name.LocalName)))
                .Select(imageElement => imageElement.Attributes().Where(attribute => imageSrcAttributes.Contains(attribute.Name.LocalName)).Single())
                .Select(imageSrcAttribute => sourceDocument.Resources.All.Where(resource => resource.Href == imageSrcAttribute.Value).Single())
                .Select(image =>
                    {
                        var ms = new MemoryStream(image.Content);
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