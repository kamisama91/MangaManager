using System;
using System.IO;

namespace MangaManager.Tasks
{
    public class WorkItem
    {
        public static int InstancesCount { get; private set; }

        public WorkItem(string filePath)
        {
            InstancesCount++;

            OriginalFilePath = filePath;
            OriginalLastWriteTime = File.GetLastWriteTime(filePath);
        }

        public string OriginalFilePath { get; private set; }
        public string WorkingFilePath { get; set; }
        public DateTime OriginalLastWriteTime { get; private set; }

        public string FilePath => !string.IsNullOrEmpty(WorkingFilePath) && (File.Exists(WorkingFilePath) || Directory.Exists(WorkingFilePath)) ? WorkingFilePath : OriginalFilePath;

        public void RestoreLastWriteTime()
        {
            File.SetLastWriteTime(FilePath, OriginalLastWriteTime);
        }
    }
}