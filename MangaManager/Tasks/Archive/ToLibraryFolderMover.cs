using System;
using System.IO;
using System.Linq;
using MangaManager.Models;
using MangaManager.Tasks.Rename;

namespace MangaManager.Tasks.Archive
{
    public class ToLibraryFolderMover : IWorkItemProcessor
    {
        public bool Accept(WorkItem workItem)
        {
            if (!Program.Options.Archive)
            {
                return false;
            }

            var archiveInfo = CacheArchiveInfos.GetOrCreate(workItem.FilePath);
            return archiveInfo.IsZip && !archiveInfo.HasSubdirectories && archiveInfo.HasComicInfo;
        }

        private string BuildSerieNameFromComicInfo(ComicInfo info)
        {
            ///TODO
            //Intermediate Folders

            var serie = info.Series;
            var authors = string.Join(" - ", new[] { info.Writer, info.Penciller }.Where(p => !string.IsNullOrEmpty(p)).Select(p => p.Split(' ').First()).Distinct());
            var publisher = info.Publisher;
            var edition = !string.IsNullOrEmpty(info.Imprint) && info.Imprint != "Edition Simple" ? $" - {info.Imprint}" : string.Empty;

            var name = serie;
            if (!string.IsNullOrEmpty(authors)) { name += $" ({authors})"; }
            if (!string.IsNullOrEmpty(publisher)) { name += $" ({publisher}{edition})"; }

            name = FileHelper.GetOsCompliantName(name);

            return name;
        }

        private string BuildVolumeNameFromComicInfo(ComicInfo info)
        {
            var volume = int.Parse(info.Number);
            var lastVolume = !string.IsNullOrEmpty(info.Count) ? int.Parse(info.Count) : 0;
            var length = lastVolume > 0
                ? 1 + (int)Math.Floor(Math.Log10(lastVolume))
                : 2;
            return BuildSerieNameFromComicInfo(info) + $" T{volume.ToString("N0").PadLeft(length, '0')}";
        }

        public void Process(WorkItem workItem)
        {
            var file = workItem.FilePath;
            var comicInfo = CacheArchiveInfos.GetOrCreate(workItem.FilePath).ComicInfo;

            //Guess Library Folder name
            var folderName = BuildSerieNameFromComicInfo(comicInfo);
            var archiveFolderPath = Path.Combine(Program.Options.ArchiveFolder, folderName);
            var archiveCompleteFolderPath = $"{archiveFolderPath} (complet)";
            if (Directory.Exists(archiveCompleteFolderPath)) { archiveFolderPath = archiveCompleteFolderPath; }

            //Guess Library File name and move into Library/Quarantine folder
            var regularFileName = BuildVolumeNameFromComicInfo(comicInfo);
            var taggedFileName = $"{regularFileName} (tag)";
            var fileName = FileNameParser.Parse(file).IsTagged ? taggedFileName : regularFileName;
            var archiveRegularFilePath = Path.Combine(archiveFolderPath, $"{regularFileName}.cbz");
            var archiveTaggedFilePath = Path.Combine(archiveFolderPath, $"{taggedFileName}.cbz");
            var archiveFilePath = Path.Combine(archiveFolderPath, $"{fileName}.cbz");
            if (archiveFilePath != file && (File.Exists(archiveRegularFilePath) || File.Exists(archiveTaggedFilePath)))
            {
                archiveFilePath = Path.Combine(Program.Options.QuarantineFolder, $"{fileName}.cbz");
                if (archiveFilePath != file) { archiveFilePath = FileHelper.GetAvailableFilename(Path.Combine(Program.Options.QuarantineFolder, $"{fileName}.cbz")); }
                //Program.View.Error($"{Path.GetFileName(archiveFilePath)} already in library, put in quarantine");
            }
            if (!Directory.Exists(Path.GetDirectoryName(archiveFilePath))) { Directory.CreateDirectory(Path.GetDirectoryName(archiveFilePath)); }
            FileHelper.Move(file, archiveFilePath);

            //Rename Library Folder when serie is complete
            if (archiveFolderPath != archiveCompleteFolderPath)
            {
                var comicInfos = Directory.EnumerateFiles(archiveFolderPath, "*.cbz", SearchOption.TopDirectoryOnly)
                    .Select(f => CacheArchiveInfos.GetOrCreate(f))
                    .Where(f => f.HasComicInfo)
                    .Select(f => f.ComicInfo)
                    .ToList();

                var lastVolume = comicInfos.Where(ci => !string.IsNullOrEmpty(ci.Count))
                    .Select(ci => int.Parse(ci.Count))
                    .Max();
                if (lastVolume > 0)
                {
                    var allVolumesPresent = true;
                    for (var i = 1; i <= lastVolume && allVolumesPresent; i++)
                    {
                        allVolumesPresent = allVolumesPresent && comicInfos.Any(ci => int.Parse(ci.Number) == i);
                    }

                    if (allVolumesPresent)
                    {
                        FileHelper.Move(archiveFolderPath, archiveCompleteFolderPath);
                        archiveFolderPath = archiveCompleteFolderPath;
                    }
                }
            }

            //Set Library Folder modification date to max modifiction date of inner files
            var lastWriteTime = Directory.EnumerateFiles(archiveFolderPath, "*.cbz", SearchOption.TopDirectoryOnly)
                .Select(f => File.GetLastWriteTime(f))
                .Max();
            Directory.SetLastWriteTime(archiveFolderPath, lastWriteTime);
        }
    }
}