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
                .Select(filePath => new WorkItem(filePath));
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

        public bool Process(WorkItem workItem)
        {
            var workingFile = workItem.FilePath;

            if (!ArchiveHelper.IsZipArchive(workingFile))
            {
                return ConvertToCbz(workItem);
            }

            if (ArchiveHelper.HasSubdirectories(workingFile))
            {
                return FlattenCbz(workItem);
            }

            //No conversion needed, juste extension update
            return RenameToCbz(workItem);
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
                        //else if (archiveReader.Entry.Attrib.HasValue && ((FileAttributes)archiveReader.Entry.Attrib.Value & FileAttributes.System) != FileAttributes.None)
                        //{
                        //    //Ignore files with System attributes (Thumbs.db...)
                        //    continue;
                        //}
                        else
                        {
                            //Program.View.Error($"Unkown archive entry: {Path.GetFileName(file)}>{archiveReader.Entry}");
                            throw new FormatException();
                        }
                    }
                }
            }
        }
        private bool ConvertToCbz(WorkItem workItem)
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
                workItem.WorkingFilePath = finalPath;
            }
            return isSuccess;
        }

        private bool FlattenCbz(WorkItem workItem)
        {
            var file = workItem.FilePath;

            var renamedEntries = new Dictionary<string, string>();
            var deletedEntries = new HashSet<string>();

            using (var archiveReader = ArchiveFactory.Open(file).ExtractAllEntries())
            {
                int i = 0;
                while (archiveReader.MoveToNextEntry())
                {
                    if (archiveReader.Entry.IsDirectory)
                    {
                        deletedEntries.Add(archiveReader.Entry.Key);
                    }
                    else
                    {
                        using var entryStream = archiveReader.OpenEntryStream();
                        using var entryMemoryStream = new MemoryStream();
                        entryStream.CopyTo(entryMemoryStream);
                        if (entryMemoryStream.TryGetImageExtension(out var extension))
                        {
                            renamedEntries.Add(archiveReader.Entry.Key, $"{i:00000}.{extension}");
                            i++;
                        }
                        else if (Path.GetFileName(archiveReader.Entry.Key).Equals(ComicInfo.NAME, StringComparison.InvariantCultureIgnoreCase))
                        {
                            //Preserve CommicInfo.xml
                            renamedEntries.Add(archiveReader.Entry.Key, ComicInfo.NAME);
                        }
                        else
                        {
                            //Program.View.Error($"Unkown archive entry: {Path.GetFileName(file)}>{archiveReader.Entry}");
                            throw new FormatException();
                        }
                    }
                }
            }

            ArchiveHelper.UpdateZipWithArchiveItemStreams(file, renamedItems: renamedEntries, deletedItems: deletedEntries);
            return RenameToCbz(workItem);
        }

        private bool RenameToCbz(WorkItem workItem)
        {
            var file = workItem.FilePath;
            var originalExtension = Path.GetExtension(file);
            var finalPath = file.Replace($"{originalExtension}", ".cbz");
            if (!file.Equals(finalPath, StringComparison.InvariantCultureIgnoreCase)) { finalPath = FileHelper.GetAvailableFilename(finalPath); }
            FileHelper.Move(file, finalPath);
            workItem.WorkingFilePath = finalPath;
            return true;
        }
    }
}