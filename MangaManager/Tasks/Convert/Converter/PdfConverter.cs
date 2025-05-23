﻿using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UglyToad.PdfPig;

namespace MangaManager.Tasks.Convert.Converter
{
    public class PdfConverter : IWorkItemProvider, IWorkItemProcessor
    {
        private List<string> _acceptdExtensions = new List<string> { ".pdf" };

        public IEnumerable<WorkItem> GetItems()
        {
            return _acceptdExtensions
                .SelectMany(extension => Directory.EnumerateFiles(Program.Options.SourceFolder, $"*{extension}", SearchOption.AllDirectories))
                .OrderBy(filePath => filePath)
                .Select(filePath => CacheWorkItems.Create(filePath));
        }

        public bool Accept(WorkItem workItem)
        {
            if (!Program.Options.ConvertRegular)
            {
                return false;
            }

            var workingFileName = workItem.FilePath;
            return _acceptdExtensions.Contains(Path.GetExtension(workingFileName));
        }

        private IEnumerable<ArchiveItemStream> GetArchiveItemStreams(string file)
        {
            var sourceDocument = PdfDocument.Open(file);
            return sourceDocument.GetPages()
                .SelectMany(page => page.GetImages())
                .Select(image =>
                {
                    var ms = new MemoryStream(image.RawBytes.ToArray());
                    if (!ms.TryGetImageExtension(out var extension)) { return null; }
                    return new ArchiveItemStream { Stream = ms, TargetExtension = extension };
                })
                .Where(item => item != null);
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