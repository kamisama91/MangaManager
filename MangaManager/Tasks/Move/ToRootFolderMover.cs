using System;
using System.IO;

namespace MangaManager.Tasks.Move
{
    public class ToRootFolderMover : IFileProcessor
    {
        public bool Accept(string file)
        {
            return Path.GetExtension(file) == ".cbz";
        }

        public bool ProcessFile(string file, out string newFile)
        {
            var sourcefolder = Path.GetDirectoryName(file);
            var filename = Path.GetFileNameWithoutExtension(file);
            var extension = Path.GetExtension(file);

            //Move file
            var movedPath = Path.Combine(Program.Options.SourceFolder, $"{filename}{extension}");
            if (!file.Equals(movedPath, StringComparison.InvariantCultureIgnoreCase))
            {
                movedPath = FileHelper.GetAvailableFilename(movedPath);
                FileHelper.Move(file, movedPath);
            }

            newFile = movedPath;
            return true;
        }
    }
}