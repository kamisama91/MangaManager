﻿using System.IO;
using System.Linq;
using EpubCore;
using System.Xml.Linq;
using System.Collections.Generic;
using System;

namespace MangaManager.Tasks.Convert.Converter
{
    public class EpubConverter : IWorkItemProvider, IWorkItemProcessor
    {
        private List<string> _acceptdExtensions = new List<string> { ".epub" };

        public IEnumerable<WorkItem> GetItems()
        {
            return _acceptdExtensions
                .SelectMany(extension => Directory.EnumerateFiles(Program.Options.SourceFolder, $"*{extension}", SearchOption.AllDirectories))
                .OrderBy(filePath => filePath)
                .Select(filePath => CacheWorkItems.Create(filePath));
        }

        public bool Accept(WorkItem workItem)
        {
            if (!Program.Options.Convert)
            {
                return false;
            }

            var workingFileName = workItem.FilePath;
            return _acceptdExtensions.Contains(Path.GetExtension(workingFileName));
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
                        return new ArchiveItemStream { Stream = ms, TargetExtension = extension };
                    });
        }

        public void Process(WorkItem workItem)
        {
            var file = workItem.FilePath;
            var outputPath = FileHelper.GetAvailableFilename(Path.Combine(Path.GetDirectoryName(file), $"{Path.GetFileNameWithoutExtension(file)}.cbz"));
            var isSuccess = ArchiveHelper.CreateZipFromArchiveItemStreams(file, outputPath, GetArchiveItemStreams);
            if (isSuccess)
            {
                File.Delete(file);
                workItem.UpdatePath(outputPath);
            }
        }
    }
}