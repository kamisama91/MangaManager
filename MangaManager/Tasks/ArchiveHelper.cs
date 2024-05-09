using SharpCompress.Common;
using System.Collections.Generic;
using System;
using System.IO;
using MangaManager.Tasks.Convert;
using SharpCompress.Archives;
using System.Linq;
using MangaManager.Models;
using SharpCompress;
using IronPython.Runtime;

namespace MangaManager.Tasks
{
    public static class ArchiveHelper
    {
        public static ArchiveInfo GetArchiveInfo(string archiveFile)
        {
            var archiveInfo = new ArchiveInfo();
            using (var archive = ArchiveFactory.Open(archiveFile))
            {
                archiveInfo.IsZip = (archive.Type == ArchiveType.Zip);
                foreach (var entry in archive.Entries)
                {
                    if (entry.IsDirectory)
                    {
                        archiveInfo.HasSubdirectories = true;
                    }
                    else
                    {
                        if (!archiveInfo.HasSubdirectories && !Path.GetFileName(entry.Key).Equals(entry.Key))
                        {
                            archiveInfo.HasSubdirectories = true;
                        }

                        if (Path.GetFileName(entry.Key).Equals("calibreHtmlOutBasicCss.css", StringComparison.InvariantCultureIgnoreCase))
                        {
                            archiveInfo.IsCalibreArchive = true;
                        }
                        else if (Path.GetFileName(entry.Key).Equals(ComicInfo.NAME, StringComparison.InvariantCultureIgnoreCase))
                        {
                            using var entryMemoryStream = new MemoryStream();
                            entry.OpenEntryStream().CopyTo(entryMemoryStream);
                            archiveInfo.ComicInfo = ComicInfo.FromXmlStream(entryMemoryStream);
                        }
                    }
                }
            }
            return archiveInfo;
        }

        public static bool CreateZipFromArchiveItemStreams(string sourceFile, string outputFile, Func<string, IEnumerable<ArchiveItemStream>> extractArchiveItemStreams)
        {
            ComicInfo comicInfo = null;
            try
            {
                using (var archive = new Ionic.Zip.ZipFile(outputFile))
                {
                    int i = 0;
                    foreach (var archiveItemStream in extractArchiveItemStreams(sourceFile))
                    {
                        var fileName = archiveItemStream.FileName;
                        if (string.IsNullOrEmpty(fileName))
                        {
                            fileName = $"{i:00000}.{archiveItemStream.Extension}";
                            i++;
                        }
                        else if (fileName.Equals(ComicInfo.NAME, StringComparison.InvariantCultureIgnoreCase))
                        {
                            comicInfo = ComicInfo.FromXmlStream(archiveItemStream.Stream);
                        }

                        var bytes = new byte[archiveItemStream.Stream.Length];
                        archiveItemStream.Stream.Read(bytes, 0, bytes.Length);
                        archive.AddEntry(fileName, bytes);
                        archiveItemStream.Clear();
                    }
                    archive.Save();
                }
            }
            catch (FormatException)
            {
                Program.View.Error($"{Path.GetFileName(sourceFile)} contains some invalid files");
                File.Delete(outputFile);
                return false;
            }

            var archiveInfo = CacheArchiveInfos.GetOrCreate(outputFile);
            archiveInfo.IsZip = true;
            archiveInfo.IsCalibreArchive = false;
            archiveInfo.HasSubdirectories = false;
            archiveInfo.ComicInfo = comicInfo;

            return true;
        }

        public static void UpdateZipWithArchiveItemStreams(string sourceFile, IEnumerable<ArchiveItemStream> createdItems = null, Dictionary<string, string> renamedItems = null, HashSet<string> deletedItems = null)
        {
            if (createdItems == null && renamedItems == null && deletedItems == null)
            {
                return;
            }

            ComicInfo comicInfo = null;
            using (var archive = Ionic.Zip.ZipFile.Read(sourceFile))
            {
                if (createdItems != null)
                {
                    var newEntriesKey = createdItems.Select(e => e.FileName).ToHashSet();
                    archive.Where(e => newEntriesKey.Contains(e.FileName)).ToArray().ForEach(archive.RemoveEntry);
                    foreach (var archiveItemStream in createdItems)
                    {
                        if (archiveItemStream.FileName.Equals(ComicInfo.NAME, StringComparison.InvariantCultureIgnoreCase))
                        {
                            comicInfo = ComicInfo.FromXmlStream(archiveItemStream.Stream);
                        }
                        var bytes = new byte[archiveItemStream.Stream.Length];
                        archiveItemStream.Stream.Read(bytes, 0, bytes.Length);
                        archive.AddEntry(archiveItemStream.FileName, bytes);
                        archiveItemStream.Clear();
                    }
                }

                if (renamedItems != null)
                {
                    foreach (var entry in archive.Where(e => renamedItems.ContainsKey(e.FileName)).ToArray())
                    {
                        entry.FileName = renamedItems[entry.FileName];
                        if (entry.FileName.Equals(ComicInfo.NAME, StringComparison.InvariantCultureIgnoreCase))
                        {
                            using var entryMemoryStream = new MemoryStream();
                            using var inputStream = entry.OpenReader();
                            inputStream.CopyTo(entryMemoryStream);
                            comicInfo = ComicInfo.FromXmlStream(entryMemoryStream);
                        }
                    }
                }

                if (deletedItems != null)
                {
                    archive.Where(e => deletedItems.Contains(e.FileName)).ToArray().ForEach(archive.RemoveEntry);
                    if (deletedItems.Any(f => f.Equals(ComicInfo.NAME, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        comicInfo = null;
                    }
                }

                archive.Save();
            }

            var archiveInfo = CacheArchiveInfos.GetOrCreate(sourceFile);
            archiveInfo.IsCalibreArchive = false; 
            archiveInfo.HasSubdirectories = false;            
            archiveInfo.ComicInfo = comicInfo;

            CacheWorkItems.Get(sourceFile)?.RestoreLastWriteTime();
        }
    }
}