using System;
using System.IO;

namespace MangaManager.Tasks.Move
{
    public class ToRootFolderMover : IWorkItemProcessor
    {
        public bool Accept(WorkItem workItem)
        {
            var workingFileName = workItem.FilePath;
            return Path.GetExtension(workingFileName) == ".cbz";
        }

        public bool Process(WorkItem workItem)
        {
            var file = workItem.FilePath;

            var filename = Path.GetFileNameWithoutExtension(file);
            var extension = Path.GetExtension(file);

            //Move file
            var movedPath = Path.Combine(Program.Options.SourceFolder, $"{filename}{extension}");
            if (!file.Equals(movedPath, StringComparison.InvariantCultureIgnoreCase))
            {
                movedPath = FileHelper.GetAvailableFilename(movedPath);
                FileHelper.Move(file, movedPath);
            }

            workItem.WorkingFilePath = movedPath;
            return true;
        }
    }
}