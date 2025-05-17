using System.IO;
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
                .SelectMany(html => XDocument.Parse(html.TextContent).DescendantNodes().OfType<XElement>().Where(element => imageElements.Contains(element.Name.LocalName)).Select(imageElement => new { html, imageElement }))
                .Select(item => new { item.html, imageSrcAttribute = item.imageElement.Attributes().Where(attribute => imageSrcAttributes.Contains(attribute.Name.LocalName)).Single() })
                .Select(item =>
                    {
                        var webStyleAbsolutePathes = new List<string>();
                        if (item.imageSrcAttribute.Value.StartsWith('/'))
                        {
                            //Path is rooted so only solution to get images is to use input path
                            webStyleAbsolutePathes.Add(item.imageSrcAttribute.Value);
                        }
                        else
                        {
                            //Path is not rooted so check for relative path from html file path
                            var basePath = Path.GetFullPath("/");
                            var notEvaluatedPath = Path.Combine(Path.GetDirectoryName(item.html.AbsolutePath), item.imageSrcAttribute.Value);
                            var osStylePath = Path.GetRelativePath(basePath, notEvaluatedPath);
                            webStyleAbsolutePathes.Add($"/{osStylePath.Replace("\\", "/")}");
                            
                            if (!item.imageSrcAttribute.Value.StartsWith('.'))
                            {
                                //Path is not explicitely relative (starting with . ou ..)
                                //So it could also be a rooted path without explicit / initial character (lower priority than possible relative path)
                                webStyleAbsolutePathes.Add($"/{item.imageSrcAttribute.Value}");
                            }
                        }
                        //Look for image using AbsolutePath (more accurate than href)
                        return sourceDocument.Resources.All.Where(resource => webStyleAbsolutePathes.Contains(resource.AbsolutePath)).First();
                    })
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