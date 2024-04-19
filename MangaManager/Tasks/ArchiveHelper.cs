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
        public static bool IsZipArchive(string archiveFile)
        {
            using (var archive = ArchiveFactory.Open(archiveFile))
            {
                return archive.Type == ArchiveType.Zip;
            }
        }

        public static bool HasSubdirectories(string archiveFile)
        {
            using (var archive = ArchiveFactory.Open(archiveFile))
            {
                return archive.Entries.Any(e => e.IsDirectory);
            }
        }

        public static bool HasComicInfo(string archiveFile)
        {
            if (CacheComicInfos.Exists(archiveFile)) 
            { 
                return CacheComicInfos.Get(archiveFile) != null;
            }

            using (var archive = ArchiveFactory.Open(archiveFile))
            {
                var hasComicInfo = archive.Entries.Any(e => !e.IsDirectory && e.Key.Equals(ComicInfo.NAME, StringComparison.InvariantCultureIgnoreCase));
                CacheComicInfos.CreateOrUpdate(archiveFile, hasComicInfo ? ComicInfo.Empty : null);
                return hasComicInfo;
            }
        }

        public static ComicInfo GetComicInfo(string archiveFile)
        {
            if (CacheComicInfos.Get(archiveFile) != ComicInfo.Empty)
            {
                return CacheComicInfos.Get(archiveFile);
            }

            using (var archive = ArchiveFactory.Open(archiveFile))
            {
                var entry = archive.Entries.SingleOrDefault(e => !e.IsDirectory && e.Key.Equals(ComicInfo.NAME, StringComparison.InvariantCultureIgnoreCase));
                using var entryMemoryStream = new MemoryStream();
                entry.OpenEntryStream().CopyTo(entryMemoryStream);
                var comicInfo = ComicInfo.FromXmlStream(entryMemoryStream);
                CacheComicInfos.CreateOrUpdate(archiveFile, comicInfo);
                return comicInfo;
            }
        }

        public static void SetComicInfo(string archiveFile, ComicInfo comicInfo)
        {
            UpdateZipWithArchiveItemStreams(archiveFile, createdItems: new[] { new ArchiveItemStream { FileName = ComicInfo.NAME, Stream = comicInfo.ToXmlStream() } });
            CacheComicInfos.CreateOrUpdate(archiveFile, comicInfo);
        }

        public static bool CreateZipFromArchiveItemStreams(string sourceFile, string outputFile, Func<string, IEnumerable<ArchiveItemStream>> extractArchiveItemStreams)
        {
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
                        using (var reader = new BinaryReader(archiveItemStream.Stream))
                        {
                            archive.AddEntry(fileName, reader.ReadBytes((int)archiveItemStream.Stream.Length));
                            archiveItemStream.Clear();    //Free memory
                        }
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
            return true; 
        }

        public static bool UpdateZipWithArchiveItemStreams(string sourceFile, IEnumerable<ArchiveItemStream> createdItems = null, Dictionary<string, string> renamedItems = null, HashSet<string> deletedItems = null)
        {
            if (createdItems == null && renamedItems == null && deletedItems == null)
            {
                return true;
            }

            using (var archive = Ionic.Zip.ZipFile.Read(sourceFile))
            {
                if (createdItems != null)
                {
                    var newEntriesKey = createdItems.Select(e => e.FileName).ToHashSet();
                    archive.Where(e => newEntriesKey.Contains(e.FileName)).ToArray().ForEach(archive.RemoveEntry);
                    foreach (var archiveItemStream in createdItems)
                    {
                        using (var reader = new BinaryReader(archiveItemStream.Stream))
                        {
                            archive.AddEntry(archiveItemStream.FileName, reader.ReadBytes((int)archiveItemStream.Stream.Length));
                            archiveItemStream.Clear();    //Free memory
                        }
                    }
                }

                if (renamedItems != null)
                {
                    foreach (var entry in archive.Where(e => renamedItems.ContainsKey(e.FileName)).ToArray())
                    {
                        entry.FileName = renamedItems[entry.FileName];
                    }
                }

                if (deletedItems != null)
                {
                    archive.Where(e => deletedItems.Contains(e.FileName)).ToArray().ForEach(archive.RemoveEntry);
                }

                archive.Save();
            }

            return true;
        }
    }
}