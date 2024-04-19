using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MangaManager.Models;
using SharpCompress.Archives;

namespace MangaManager.Tasks.Convert.Converter
{
    public class ArchiveConverter : IFileProcessor
    {
        private List<string> _acceptdExtensions = new List<string> { ".c7z", ".7z", ".cbr", ".rar", ".cbz", ".zip", };

        public string[] GetFiles()
        {
            return _acceptdExtensions.SelectMany(extension => Directory.EnumerateFiles(Program.Options.SourceFolder, $"*{extension}", SearchOption.AllDirectories)).ToArray();
        }

        public bool Accept(string file)
        {
            return _acceptdExtensions.Contains(Path.GetExtension(file));
        }

        public bool ProcessFile(string file, out string newFile)
        {
            if (!ArchiveHelper.IsZipArchive(file))
            {
                return ConvertToCbz(file, out newFile);
            }

            if (ArchiveHelper.HasSubdirectories(file))
            {
                return FlattenCbz(file, out newFile);
            }

            //No conversion needed, juste extension update
            return RenameToCbz(file, out newFile);
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
        private bool ConvertToCbz(string file, out string newFile)
        {
            var originalExtension = Path.GetExtension(file);
            var finalPath = file.Replace($"{originalExtension}", ".cbz");
            if (!file.Equals(finalPath, StringComparison.InvariantCultureIgnoreCase)) { finalPath = FileHelper.GetAvailableFilename(finalPath); }
            var workingPath = finalPath.Replace($".cbz", ".cbz.tmp");

            var isSuccess = ArchiveHelper.CreateZipFromArchiveItemStreams(file, workingPath, GetArchiveItemStream);
            if (isSuccess)
            {
                File.Delete(file);
                FileHelper.Move(workingPath, finalPath);
            }
            newFile = finalPath;
            return isSuccess;
        }

        private bool FlattenCbz(string file, out string newFile)
        {
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
            return RenameToCbz(file, out newFile);
        }

        private bool RenameToCbz(string file, out string newFile)
        {
            var originalExtension = Path.GetExtension(file);
            var finalPath = file.Replace($"{originalExtension}", ".cbz");
            if (!file.Equals(finalPath, StringComparison.InvariantCultureIgnoreCase)) { finalPath = FileHelper.GetAvailableFilename(finalPath); }
            FileHelper.Move(file, finalPath);
            newFile = finalPath;
            return true;
        }
    }
}