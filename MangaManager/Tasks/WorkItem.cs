using System;
using System.IO;
using System.Threading;

namespace MangaManager.Tasks
{
    public class WorkItem
    {
        public static int _instancesCount;
        public static int InstancesCount => _instancesCount;

        public WorkItem(string filePath)
        {
            InstanceId = Interlocked.Increment(ref _instancesCount);
            OriginalFilePath = filePath;
            OriginalLastWriteTime = File.GetLastWriteTime(filePath);
        }

        public int InstanceId { get; private set; }
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