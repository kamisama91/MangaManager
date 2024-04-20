using System;
using System.IO;
using System.Linq;
using MangaManager.Models;

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

            var archiveInfo = ArchiveHelper.GetOrCreateArchiveInfo(workItem.FilePath);
            return archiveInfo.IsZip && !archiveInfo.HasSubdirectories && archiveInfo.HasComicInfo;
        }

        private string BuildSerieNameFromComicInfo(ComicInfo info)
        {
            ///TODO
            //Intermediate Folders

            var serie = info.Series;
            var authors = string.Join (" - ", new[] { info.Writer, info.Penciller }.Where(p => !string.IsNullOrEmpty(p)).Select(p => p.Split(' ').First()).Distinct());
            var publisher = info.Publisher;
            var edition = (!string.IsNullOrEmpty(info.Imprint) && info.Imprint != "Edition Simple") ? $" - {info.Imprint}" : string.Empty;

            var name = serie;
            if (!string.IsNullOrEmpty(authors)) { name += $" ({authors})"; }
            if (!string.IsNullOrEmpty(publisher)) { name += $" ({publisher}{edition})"; }
            
            name = System.Net.WebUtility.HtmlDecode(name);
            name = FileHelper.GetOsCompliantName(name);

            return name;
        }

        private string BuildVolumeNameFromComicInfo(ComicInfo info)
        {
            ///TODO
            //Check file is flagged as Tagged
            //var tagSuffix = Regex.IsMatch(filename, @"\[tag\]|\(tag\)", RegexOptions.IgnoreCase) ? " (tag)" : string.Empty;

            var volume = int.Parse(info.Number);
            var lastVolume = !string.IsNullOrEmpty(info.Count) ? int.Parse(info.Count) : 0;
            var length = lastVolume > 0
                ? 1 + (int)Math.Floor(Math.Log10(lastVolume))
                : 2;
            return BuildSerieNameFromComicInfo(info) + $" T{volume.ToString("N0").PadLeft(length, '0')}";
        }

        public bool Process(WorkItem workItem)
        {
            var file = workItem.FilePath;

            var comicInfo = ArchiveHelper.GetOrCreateArchiveInfo(workItem.FilePath).ComicInfo;
            
            //Guess Library Folder name
            var folderName = BuildSerieNameFromComicInfo(comicInfo);
            var archiveFolderPath = Path.Combine(Program.Options.ArchiveFolder, folderName);
            var archiveCompleteFolderPath = $"{archiveFolderPath} (complet)";
            if (Directory.Exists(archiveCompleteFolderPath)) { archiveFolderPath = archiveCompleteFolderPath; }

            //Guess Library File name and move into Library/Quarantine folder
            var fileName = BuildVolumeNameFromComicInfo(comicInfo);
            var archiveFilePath = Path.Combine(archiveFolderPath, $"{fileName}.cbz");
            if (File.Exists(archiveFilePath)) 
            { 
                archiveFilePath = FileHelper.GetAvailableFilename(Path.Combine(Program.Options.QuarantineFolder, $"{fileName}.cbz"));
                Program.View.Error($"{Path.GetFileName(archiveFilePath)} already in library, put in quarantine");
            }
            if (!Directory.Exists(Path.GetDirectoryName(archiveFilePath))) { Directory.CreateDirectory(Path.GetDirectoryName(archiveFilePath)); }
            FileHelper.Move(file, archiveFilePath);

            //Rename Library Folder when serie is complete
            if (archiveFolderPath != archiveCompleteFolderPath)
            {
                var comicInfos = Directory.EnumerateFiles(archiveFolderPath, "*.cbz", SearchOption.TopDirectoryOnly)
                    .Select(f => ArchiveHelper.GetOrCreateArchiveInfo(f))
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
                        archiveFilePath = Path.Combine(archiveCompleteFolderPath, $"{fileName}.cbz");
                        archiveFolderPath = archiveCompleteFolderPath;
                    }
                }
            }

            //Set Library Folder modification date to max modifiction date of inner files
            var lastWriteTime = Directory.EnumerateFiles(archiveFolderPath, "*.cbz", SearchOption.TopDirectoryOnly)
                .Select(f => File.GetLastWriteTime(f))
                .Max();
            Directory.SetLastWriteTime(archiveFolderPath, lastWriteTime);

            workItem.WorkingFilePath = archiveFilePath;
            return true;
        }
    }
}