using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MangaManager.Models;
using SharpCompress.Archives;

namespace MangaManager.Tasks.Convert.Converter
{
    public class ArchiveConverter : IWorkItemProvider, IWorkItemProcessor
    {
        private List<string> _acceptdExtensions = new List<string> { ".c7z", ".7z", ".cbr", ".rar", ".cbz", ".zip", };

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

            if (!archiveInfo.IsZip)
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
            using (var archiveReader = ArchiveFactory.Open(file).ExtractAllEntries())
            {
                while (archiveReader.MoveToNextEntry())
                {
                    if (!archiveReader.Entry.IsDirectory)
                    {
                        using var entryStream = archiveReader.OpenEntryStream();
                        using var entryMemoryStream = new MemoryStream();
                        entryStream.CopyTo(entryMemoryStream);
                        if (entryMemoryStream.TryGetImageExtension(out var extension))
                        {
                            yield return new ArchiveItemStream { Stream = entryMemoryStream, Extension = extension };
                        }
                        else if (Path.GetFileName(archiveReader.Entry.Key).Equals(ComicInfo.NAME, StringComparison.InvariantCultureIgnoreCase))
                        {
                            //Preserve CommicInfo.xml
                            yield return new ArchiveItemStream { Stream = entryMemoryStream, FileName = ComicInfo.NAME };
                        }
                        else if (Path.GetFileName(archiveReader.Entry.Key).Equals("thumbs.db", StringComparison.InvariantCultureIgnoreCase)
                              || Path.GetFileName(archiveReader.Entry.Key).Equals("desktop.ini", StringComparison.InvariantCultureIgnoreCase))
                        {
                            //ignore those windows files...
                            continue;
                        }
                        else
                        {
                            throw new FormatException();
                        }
                    }
                }
            }
        }
        private void ConvertToCbz(WorkItem workItem)
        {
            var file = workItem.FilePath;

            var originalExtension = Path.GetExtension(file);
            var finalPath = file.Replace($"{originalExtension}", ".cbz");
            if (!file.Equals(finalPath, StringComparison.InvariantCultureIgnoreCase)) { finalPath = FileHelper.GetAvailableFilename(finalPath); }
            var workingPath = finalPath.Replace($".cbz", ".cbz.tmp");

            var isSuccess = ArchiveHelper.CreateZipFromArchiveItemStreams(file, workingPath, GetArchiveItemStream);
            if (isSuccess)
            {
                File.Delete(file);
                FileHelper.Move(workingPath, finalPath);
                workItem.UpdatePath(finalPath);
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