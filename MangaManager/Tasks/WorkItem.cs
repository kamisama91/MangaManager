using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;

namespace MangaManager.Tasks
{
    public class WorkItem
    {
        private static ConcurrentBag<WorkItem> _instances = new ConcurrentBag<WorkItem>();
        private static int _instancesCount;
        public static int InstancesCount => _instances.Count;

        public static WorkItem Find(string path)
        {
            return _instances.SingleOrDefault(item => item.FilePath == path);
        }

        private DateTime OriginalLastWriteTime; 
        private string OriginalFilePath;
        private string WorkingFilePath;

        public int InstanceId { get; private set; }
        public string FilePath { get; private set; }
        
        public WorkItem(string filePath)
        {
            var id = Interlocked.Increment(ref _instancesCount);
            _instances.Add(this);

            InstanceId = id;
            OriginalLastWriteTime = File.GetLastWriteTime(filePath);
            OriginalFilePath = filePath;
            WorkingFilePath = filePath;
            FilePath = filePath;
        }

        public void UpdateFilePath(string path)
        {
            WorkingFilePath = path;

            if (!string.IsNullOrEmpty(WorkingFilePath) && (File.Exists(WorkingFilePath) || Directory.Exists(WorkingFilePath)))
            {
                FilePath = path;
                RestoreLastWriteTime();
            }
            else
            {
                FilePath = OriginalFilePath;
            }
        }

        public void RestoreLastWriteTime()
        {
            File.SetLastWriteTime(FilePath, OriginalLastWriteTime);
        }
    }
}