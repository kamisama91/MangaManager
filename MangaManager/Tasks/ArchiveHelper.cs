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
        private static ArchiveInfo BuildArchiveInfo (string archiveFile)
        {
            var archiveInfo = new ArchiveInfo();
            using (var archive = ArchiveFactory.Open(archiveFile))
            {
                archiveInfo.IsZip = (archive.Type == ArchiveType.Zip);
                archiveInfo.HasSubdirectories = archive.Entries.Any(e => e.IsDirectory);

                var entry = archive.Entries.SingleOrDefault(e => !e.IsDirectory && e.Key.Equals(ComicInfo.NAME, StringComparison.InvariantCultureIgnoreCase));
                if (entry != null)
                {
                    using var entryMemoryStream = new MemoryStream();
                    entry.OpenEntryStream().CopyTo(entryMemoryStream);
                    archiveInfo.ComicInfo = ComicInfo.FromXmlStream(entryMemoryStream);
                }
            }
            return archiveInfo;
        }

        public static ArchiveInfo GetOrCreateArchiveInfo(string archiveFile)
        {
            if (CacheArchiveInfos.Exists(archiveFile))
            {
                return CacheArchiveInfos.Get(archiveFile);
            }

            var archiveInfo = BuildArchiveInfo(archiveFile);
            CacheArchiveInfos.CreateOrUpdate(archiveFile, archiveInfo);
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
                        else
                        {
                            if (fileName.Equals(ComicInfo.NAME, StringComparison.InvariantCultureIgnoreCase))
                            {
                                comicInfo = ComicInfo.FromXmlStream(archiveItemStream.Stream);
                            }
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

            var archiveInfo = new ArchiveInfo()
            {
                IsZip = true,
                HasSubdirectories = false,
                ComicInfo = comicInfo
            };
            CacheArchiveInfos.CreateOrUpdate(outputFile, archiveInfo);

            return true; 
        }

        public static bool UpdateZipWithArchiveItemStreams(string sourceFile, IEnumerable<ArchiveItemStream> createdItems = null, Dictionary<string, string> renamedItems = null, HashSet<string> deletedItems = null)
        {
            if (createdItems == null && renamedItems == null && deletedItems == null)
            {
                return false;
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

            var archiveInfo = GetOrCreateArchiveInfo(sourceFile);
            archiveInfo.HasSubdirectories = false;
            archiveInfo.ComicInfo = comicInfo;

            return true;
        }
    }
}