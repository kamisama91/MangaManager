using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MangaManager.Models;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace MangaManager.Tasks.Convert.Converter
{
    public class ArchiveConverter : IWorkItemProvider, IWorkItemProcessor
    {
        private List<string> _acceptdExtensions = new List<string> { ".cb7", ".7z", ".cbr", ".rar", ".cbt", ".tar", ".cbz", ".zip", };

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

        public void Process(WorkItem workItem)
        {
            var archiveInfo = CacheArchiveInfos.GetOrCreate(workItem.FilePath);

            if (!archiveInfo.IsZip || archiveInfo.IsCalibreArchive)
            {
                ConvertToCbz(workItem);
            }
            else if (archiveInfo.HasSubdirectories)
            {
                FlattenCbz(workItem);
            }
            else
            {
                //No conversion needed, just potential extension update
                RenameToCbz(workItem);
            }
        }

        private IEnumerable<ArchiveItemStream> GetArchiveItemStream(string file)
        {
            var BuildArchiveItemStream = (string key, MemoryStream ms) =>
            {
                if (ms.TryGetImageExtension(out var extension))
                {
                    return new ArchiveItemStream { Stream = ms, Extension = extension };
                }
                else if (Path.GetFileName(key).Equals(ComicInfo.NAME, StringComparison.InvariantCultureIgnoreCase))
                {
                    //Preserve CommicInfo.xml
                    return new ArchiveItemStream { Stream = ms, FileName = ComicInfo.NAME };
                }
                else if (Path.GetFileName(key).Equals("thumbs.db", StringComparison.InvariantCultureIgnoreCase)
                      || Path.GetFileName(key).Equals("desktop.ini", StringComparison.InvariantCultureIgnoreCase))
                {
                    //ignore those windows files...
                    return null;
                }
                else
                {
                    throw new FormatException();
                }
            };

            using var archive = ArchiveFactory.Open(file);

            var sortedEntries = archive.Entries.Where(e => !e.IsDirectory)
                .OrderBy(e => int.TryParse(Regex.Replace(Path.GetDirectoryName(e.Key), @"^.*?(\d+)$", "$1"), out var folderNum) ? folderNum : 0)
                .ThenBy(e => Path.GetDirectoryName(e.Key))
                .ThenBy(e => int.TryParse(Regex.Replace(Path.GetFileNameWithoutExtension(e.Key), @"^.*?(\d+)$", "$1"), out var fileNum) ? fileNum : 0)
                .ThenBy(e => Path.GetFileNameWithoutExtension(e.Key))
                .Select(e => new { Entry = e, DecompressedStream = new MemoryStream() })
                .ToList();

            if (archive.Type == ArchiveType.SevenZip)
            {
                //Cannot iterates over all sortedEntries to call OpenEntryStream: it is very slow for 7z archive
                //As a consequence we will store all extracted content in memory...
                using (var archiveReader = ArchiveFactory.Open(file).ExtractAllEntries())
                {
                    while (archiveReader.MoveToNextEntry())
                    {
                        var sortedEntry = sortedEntries.SingleOrDefault(e => e.Entry.Key.Equals(archiveReader.Entry.Key));
                        if (sortedEntry != null)
                        {
                            using var entryStream = archiveReader.OpenEntryStream();
                            entryStream.CopyTo(sortedEntry.DecompressedStream);
                        }
                    }
                }
                foreach (var archiveItem in sortedEntries)
                {
                    if (BuildArchiveItemStream(archiveItem.Entry.Key, archiveItem.DecompressedStream) is ArchiveItemStream archiveItemStream)
                    {
                        yield return archiveItemStream;
                    }
                }
            }
            else
            {
                foreach (var archiveItem in sortedEntries)
                {
                    using var entryStream = archiveItem.Entry.OpenEntryStream();
                    var entryMemoryStream = new MemoryStream();
                    entryStream.CopyTo(entryMemoryStream);
                    if (BuildArchiveItemStream(archiveItem.Entry.Key, entryMemoryStream) is ArchiveItemStream archiveItemStream)
                    {
                        yield return archiveItemStream;
                    }
                }
            }
        }
        private IEnumerable<ArchiveItemStream> GetCalibreArchiveItemStream(string file)
        {
            var workingDir = Directory.GetCurrentDirectory();

            var htmlExtensions = new[] { ".htm", ".html", ".xhtml" };
            var prevPageRegex = new Regex("<a (?:href|xlink:href)=[\"']([^\\\"']*)[\"'].*?class=[\"']calibreAPrev[\"']>");
            var nextPageRegex = new Regex("<a (?:href|xlink:href)=[\"']([^\\\"']*)[\"'].*?class=[\"']calibreANext[\"']>");
            var imageRegex = new Regex("<(?:img|image).*?(?:src|href|xlink:src|xlink:href)=[\"']([^\\\"']*)[\"']");

            using var archive = ArchiveFactory.Open(file);
            var htmlEntries = archive.Entries
                    .Where(e => htmlExtensions.Contains(Path.GetExtension(e.Key)))
                    .Select(e =>
                    {
                        //Extract content of html pages
                        using var es = e.OpenEntryStream();
                        using var ms = new MemoryStream();
                        es.CopyTo(ms);
                        var buffer = new byte[ms.Length];
                        ms.Position = 0;
                        ms.Read(buffer, 0, buffer.Length);
                        var content = new string(buffer.Select(b => (char)b).ToArray());

                        var prevPageResult = prevPageRegex.Match(content);
                        var prevPage = prevPageResult.Success
                            ? Path.GetFullPath(Path.Combine(Path.GetDirectoryName(e.Key), prevPageResult.Groups[1].Value)).Replace(workingDir, string.Empty).Replace(Path.DirectorySeparatorChar, '/').TrimStart('/')
                            : string.Empty;

                        var nextPageResult = nextPageRegex.Match(content);
                        var nextPage = nextPageResult.Success
                            ? Path.GetFullPath(Path.Combine(Path.GetDirectoryName(e.Key), nextPageResult.Groups[1].Value)).Replace(workingDir, string.Empty).Replace(Path.DirectorySeparatorChar, '/').TrimStart('/')
                            : string.Empty;

                        var imageResult = imageRegex.Match(content);
                        var image = imageResult.Success
                            ? Path.GetFullPath(Path.Combine(Path.GetDirectoryName(e.Key), imageResult.Groups[1].Value)).Replace(workingDir, string.Empty).Replace(Path.DirectorySeparatorChar, '/').TrimStart('/')
                            : string.Empty;

                        return new
                        {
                            page = e.Key,
                            prevPage = prevPage,
                            nextPage = nextPage,
                            image = image,
                        };
                    })
                    .ToDictionary(a => a.page, a => a);

            var item = htmlEntries.Values.FirstOrDefault(v => string.IsNullOrEmpty(v.prevPage));
            while (item != null)
            {
                if (!string.IsNullOrEmpty(item.image))
                {
                    var e = archive.Entries.Single(e => e.Key.Equals(item.image));
                    using var es = e.OpenEntryStream();
                    using var ms = new MemoryStream();
                    es.CopyTo(ms);
                    ms.Position = 0;
                    if (ms.TryGetImageExtension(out var extension))
                    {
                        yield return new ArchiveItemStream { Stream = ms, Extension = extension };
                    }
                }
                item = htmlEntries.ContainsKey(item.nextPage) ? htmlEntries[item.nextPage] : null;
            }
        }

        private void ConvertToCbz(WorkItem workItem)
        {
            var file = workItem.FilePath;

            var originalExtension = Path.GetExtension(file);
            var finalPath = file.Replace($"{originalExtension}", ".cbz");
            if (!file.Equals(finalPath, StringComparison.InvariantCultureIgnoreCase)) { finalPath = FileHelper.GetAvailableFilename(finalPath); }
            var workingPath = finalPath.Replace($".cbz", ".cbz.tmp");

            Func<string, IEnumerable<ArchiveItemStream>> getArchiveItemStreamMethod = CacheArchiveInfos.GetOrCreate(workItem.FilePath).IsCalibreArchive 
                ? GetCalibreArchiveItemStream
                : GetArchiveItemStream;
            var isSuccess = ArchiveHelper.CreateZipFromArchiveItemStreams(file, workingPath, getArchiveItemStreamMethod);
            if (isSuccess)
            {
                CacheArchiveInfos.RemoveItem(file);
                CacheWorkItems.Get(file)?.UpdatePath(workingPath);
                File.Delete(file);
                FileHelper.Move(workingPath, finalPath);
            }
        }

        private void FlattenCbz(WorkItem workItem)
        {
            var file = workItem.FilePath;

            var renamedEntries = new Dictionary<string, string>();
            var deletedEntries = new HashSet<string>();

            using (var archive = Ionic.Zip.ZipFile.Read(file))
            {
                int i = 0;
                var entries = archive.Where(e => !e.IsDirectory)
                    .OrderBy(e => int.TryParse(Regex.Replace(Path.GetDirectoryName(e.FileName), @"^.*?(\d+)$", "$1"), out var folderNum) ? folderNum : 0)
                    .ThenBy(e => Path.GetDirectoryName(e.FileName))
                    .ThenBy(e => int.TryParse(Regex.Replace(Path.GetFileNameWithoutExtension(e.FileName), @"^.*?(\d+)$", "$1"), out var fileNum) ? fileNum : 0)
                    .ThenBy(e => Path.GetFileNameWithoutExtension(e.FileName))
                    .ToArray();
                foreach (var entry in entries)
                {
                    using var entryStream = entry.OpenReader();
                    using var entryMemoryStream = new MemoryStream();
                    entryStream.CopyTo(entryMemoryStream);
                    if (entryMemoryStream.TryGetImageExtension(out var extension))
                    {
                        renamedEntries.Add(entry.FileName, $"{i:00000}.{extension}");
                        i++;
                    }
                    else if (Path.GetFileName(entry.FileName).Equals(ComicInfo.NAME, StringComparison.InvariantCultureIgnoreCase))
                    {
                        //Preserve CommicInfo.xml
                        renamedEntries.Add(entry.FileName, ComicInfo.NAME);
                    }
                    else if (Path.GetFileName(entry.FileName).Equals("thumbs.db", StringComparison.InvariantCultureIgnoreCase)
                            || Path.GetFileName(entry.FileName).Equals("desktop.ini", StringComparison.InvariantCultureIgnoreCase))
                    {
                        //ignore those windows files...
                        deletedEntries.Add(entry.FileName);
                    }
                    else
                    {
                        throw new FormatException();
                    }
                }
                foreach (var entry in archive.Where(e => e.IsDirectory).ToArray())
                {
                    deletedEntries.Add(entry.FileName);
                }
            }

            ArchiveHelper.UpdateZipWithArchiveItemStreams(file, renamedItems: renamedEntries, deletedItems: deletedEntries);
            RenameToCbz(workItem);
        }

        private void RenameToCbz(WorkItem workItem)
        {
            var file = workItem.FilePath;
            var originalExtension = Path.GetExtension(file);
            var finalPath = file.Replace($"{originalExtension}", ".cbz");
            var needRename = !file.Equals(finalPath, StringComparison.InvariantCultureIgnoreCase);
            if (needRename) 
            { 
                finalPath = FileHelper.GetAvailableFilename(finalPath);
                FileHelper.Move(file, finalPath);
            }
        }
    }
}