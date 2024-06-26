﻿using System;
using System.IO;

namespace MangaManager.Tasks.Move
{
    public class ToRootFolderMover : IWorkItemProcessor
    {
        public bool Accept(WorkItem workItem)
        {
            if (!Program.Options.Move)
            {
                return false;
            }

            var archiveInfo = CacheArchiveInfos.GetOrCreate(workItem.FilePath);
            return archiveInfo.IsZip;
        }

        public void Process(WorkItem workItem)
        {
            var file = workItem.FilePath;

            var filename = Path.GetFileNameWithoutExtension(file);
            var extension = ".cbz";

            //Move file
            var movedPath = Path.Combine(Program.Options.SourceFolder, $"{filename}{extension}");
            if (!file.Equals(movedPath, StringComparison.InvariantCultureIgnoreCase))
            {
                movedPath = FileHelper.GetAvailableFilename(movedPath);
                FileHelper.Move(file, movedPath);
            }
        }
    }
}