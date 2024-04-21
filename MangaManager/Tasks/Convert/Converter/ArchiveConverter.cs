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
            var archiveInfo = ArchiveHelper.GetOrCreateArchiveInfo(workItem.FilePath);

            if (!archiveInfo.IsZip)
            {
                return ConvertToCbz(workItem);
            }

            if (archiveInfo.HasSubdirectories)
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
                        else if (Path.GetFileName(archiveReader.Entry.Key).Equals("thumbs.db", StringComparison.InvariantCultureIgnoreCase)
                              || Path.GetFileName(archiveReader.Entry.Key).Equals("desktop.ini", StringComparison.InvariantCultureIgnoreCase))
                        {
                            //ignore those windows files...
                            continue;
                        }
                        //else if (archiveReader.Entry.Attrib.HasValue && ((FileAttributes)archiveReader.Entry.Attrib.Value & FileAttributes.System) != FileAttributes.None)
                        //{
                        //    //Ignore files with System attributes (Thumbs.db...)
                        //    continue;
                        //}
                        else
                        {
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

            using (var archive = Ionic.Zip.ZipFile.Read(file))
            {
                int i = 0;
                foreach (var entry in archive.Where(e => !e.IsDirectory).ToArray())
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

            var isFlattened = ArchiveHelper.UpdateZipWithArchiveItemStreams(file, renamedItems: renamedEntries, deletedItems: deletedEntries);
            var isRenamed = RenameToCbz(workItem);
            return isFlattened || isRenamed;
        }

        private bool RenameToCbz(WorkItem workItem)
        {
            var file = workItem.FilePath;
            var originalExtension = Path.GetExtension(file);
            var finalPath = file.Replace($"{originalExtension}", ".cbz");
            var needRename = !file.Equals(finalPath, StringComparison.InvariantCultureIgnoreCase);
            if (needRename) 
            { 
                finalPath = FileHelper.GetAvailableFilename(finalPath);
                FileHelper.Move(file, finalPath);
                workItem.WorkingFilePath = finalPath;

            }
            return needRename;
        }
    }
}