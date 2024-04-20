using System;
using System.Globalization;
using System.IO;

namespace MangaManager.Tasks.Rename
{
    public class FromFileNameRenamer : IWorkItemProcessor
    {
        public bool Accept(WorkItem workItem)
        {
            if (!Program.Options.Rename)
            {
                return false;
            }

            var archiveInfo = ArchiveHelper.GetOrCreateArchiveInfo(workItem.FilePath);
            return archiveInfo.IsZip;
        }

        public bool Process(WorkItem workItem)
        {
            var file = workItem.FilePath;

            var folder = Path.GetDirectoryName(file);
            var filename = Path.GetFileNameWithoutExtension(file);
            var extension = ".cbz";

            //Check file is flagged as Tagged
            var tagSuffix = FileNameParser.Parse(Path.GetFileNameWithoutExtension(filename)).IsTagged ? " (tag)" : string.Empty;

            //Enrich filename whith prent folders names
            var enrichedFilename = filename;
            var parentFolder = folder;
            while (parentFolder.TrimEnd(Path.DirectorySeparatorChar) != Program.Options.SourceFolder.TrimEnd(Path.DirectorySeparatorChar)
                && (string.IsNullOrEmpty(Program.Options.ArchiveFolder) || parentFolder.TrimEnd(Path.DirectorySeparatorChar) != Program.Options.ArchiveFolder.TrimEnd(Path.DirectorySeparatorChar))
                && (string.IsNullOrEmpty(Program.Options.QuarantineFolder) || parentFolder.TrimEnd(Path.DirectorySeparatorChar) != Program.Options.QuarantineFolder.TrimEnd(Path.DirectorySeparatorChar)))
            {
                var parentFolderSerie = FileNameParser.Parse(Path.GetFileNameWithoutExtension(parentFolder)).Serie;
                if (!enrichedFilename.Contains(parentFolderSerie, StringComparison.InvariantCultureIgnoreCase))
                {
                    enrichedFilename = $"{parentFolderSerie} {enrichedFilename}";
                }
                parentFolder = Path.GetFullPath(Path.Combine(parentFolder, ".."));
            }

            //Extract Serie and Volume from name
            var parsedFileName = FileNameParser.Parse(enrichedFilename);
            var serie = parsedFileName.Serie;
            var volume = parsedFileName.Volume;

            //Rename file
            var renamedPath = Path.Combine(folder, $"{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(serie)} T{volume:D2}{tagSuffix}{extension}");
            if (!file.Equals(renamedPath, StringComparison.InvariantCultureIgnoreCase))
            {
                var workingPath = file.Replace($".cbz", ".cbz.tmp");
                FileHelper.Move(file, workingPath);
                renamedPath = FileHelper.GetAvailableFilename(renamedPath);
                FileHelper.Move(workingPath, renamedPath);
            }

            workItem.WorkingFilePath = renamedPath;
            return true;
        }
    }
}