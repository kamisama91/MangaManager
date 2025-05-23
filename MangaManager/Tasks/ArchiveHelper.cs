﻿using SharpCompress.Common;
using System.Collections.Generic;
using System;
using System.IO;
using MangaManager.Tasks.Convert;
using SharpCompress.Archives;
using System.Linq;
using MangaManager.Models;
using SharpCompress;
using IronPython.Runtime;
using System.Text;

namespace MangaManager.Tasks
{
    public static class ArchiveHelper
    {
        public static ArchiveInfo GetArchiveInfo(string archiveFile)
        {
            if (!File.Exists(archiveFile))
            {
                return new ArchiveInfo
                {
                    IsZip = false,
                    IsCalibreArchive = false,
                    HasSubdirectories = false,
                    ComicInfo = null,
                    RenameMap = null,
                };
            }

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
                        else if (Path.GetFileName(entry.Key).Equals(RenameMap.NAME, StringComparison.InvariantCultureIgnoreCase))
                        {
                            using var entryMemoryStream = new MemoryStream();
                            entry.OpenEntryStream().CopyTo(entryMemoryStream);
                            archiveInfo.RenameMap = RenameMap.FromCsvStream(entryMemoryStream);
                        }
                    }
                }
            }
            return archiveInfo;
        }

        public static bool CreateZipFromArchiveItemStreams(string sourceFile, string outputFile, Func<string, IEnumerable<ArchiveItemStream>> extractArchiveItemStreams)
        {
            ComicInfo comicInfo = null;
            RenameMap renameMap = null;
            try
            {
                var renamedEntries = new Dictionary<string, string>();

                using (var archive = new Ionic.Zip.ZipFile(outputFile))
                {
                    int i = 0;
                    foreach (var archiveItemStream in extractArchiveItemStreams(sourceFile))
                    {
                        var fileName = archiveItemStream.TargetFileName;
                        if (string.IsNullOrEmpty(fileName))
                        {
                            fileName = $"{i:00000}.{archiveItemStream.TargetExtension}";
                            i++;
                        }
                        else if (fileName.Equals(ComicInfo.NAME, StringComparison.InvariantCultureIgnoreCase))
                        {
                            comicInfo = ComicInfo.FromXmlStream(archiveItemStream.Stream);
                        }

                        if (!string.IsNullOrEmpty(archiveItemStream.SourceFileName) && !archiveItemStream.SourceFileName.Equals(fileName))
                        {
                            renamedEntries.Add(archiveItemStream.SourceFileName, fileName);
                        }

                        var bytes = new byte[archiveItemStream.Stream.Length];
                        archiveItemStream.Stream.Read(bytes, 0, bytes.Length);
                        archive.AddEntry(fileName, bytes);
                        archiveItemStream.Clear();
                    }

                    if (renamedEntries.Count != 0)
                    {
                        renameMap = new RenameMap(renamedEntries);
                        archive.AddEntry(RenameMap.NAME, renameMap.ToCsvStream());
                    }

                    archive.Save();
                }
            }
            catch (FormatException)
            {
                Program.ViewController.Error($"{Path.GetFileName(sourceFile)} contains some invalid files");
                File.Delete(outputFile);
                return false;
            }

            var archiveInfo = CacheArchiveInfos.GetOrCreate(outputFile);
            archiveInfo.IsZip = true;
            archiveInfo.IsCalibreArchive = false;
            archiveInfo.HasSubdirectories = false;
            archiveInfo.ComicInfo = comicInfo;
            archiveInfo.RenameMap = renameMap;

            return true;
        }

        public static void UpdateZipWithArchiveItemStreams(string sourceFile, List<ArchiveItemStream> createdItems = null, Dictionary<string, string> renamedItems = null, HashSet<string> deletedItems = null)
        {
            if (createdItems == null && renamedItems == null && deletedItems == null)
            {
                return;
            }

            if (renamedItems != null && renamedItems.Count != 0)
            {
                createdItems = createdItems ?? new List<ArchiveItemStream>();
                var renameMap = new RenameMap(renamedItems);
                createdItems.Add(new ArchiveItemStream { TargetFileName = RenameMap.NAME, Stream = renameMap.ToCsvStream() });
            }
            
            using (var archive = Ionic.Zip.ZipFile.Read(sourceFile))
            {
                if (createdItems != null)
                {
                    var newEntriesKey = createdItems.Select(e => e.TargetFileName).ToHashSet();
                    archive.Where(e => newEntriesKey.Contains(e.FileName)).ToArray().ForEach(archive.RemoveEntry);
                    foreach (var archiveItemStream in createdItems)
                    {
                        var bytes = new byte[archiveItemStream.Stream.Length];
                        archiveItemStream.Stream.Read(bytes, 0, bytes.Length);
                        archive.AddEntry(archiveItemStream.TargetFileName, bytes);
                        archiveItemStream.Clear();
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

            CacheArchiveInfos.RemoveItem(sourceFile);
            var archiveInfo = CacheArchiveInfos.GetOrCreate(sourceFile);

            CacheWorkItems.Get(sourceFile)?.RestoreLastWriteTime();
        }
    }
}