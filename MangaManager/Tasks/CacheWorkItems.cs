using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;

namespace MangaManager.Tasks
{
    public class WorkItem
    {
        public int InstanceId { get; private set; }
        public string FilePath { get; private set; }
        public string OriginalFilePath { get; private set; }
        public DateTime OriginalLastWriteTime { get; private set; }

        //Must not be called except by CacheWorkItems.Create
        public WorkItem(int instanceId, string filePath)
        {
            InstanceId = instanceId;
            FilePath = filePath;
            OriginalFilePath = filePath;
            if (File.Exists(FilePath))
            {
                OriginalLastWriteTime = File.GetLastWriteTime(filePath);
            }
            else if (Directory.Exists(FilePath))
            {
                OriginalLastWriteTime = Directory.GetLastWriteTime(filePath);
            }
        }

        public void UpdatePath(string path)
        {
            if (FilePath == path)
            {
                return;
            }

            FilePath = path;
            RestoreLastWriteTime();
        }

        public void RestoreLastWriteTime()
        {
            File.SetLastWriteTime(FilePath, OriginalLastWriteTime);
        }
    }

    public static class CacheWorkItems
    {
        private static int s_LastUsedInstanceId = 0;
        private static ConcurrentBag<WorkItem> s_WorkItemsCache = new ConcurrentBag<WorkItem>();
        
        public static int InstancesCount => s_WorkItemsCache.Count;


        public static int GetNextInstanceId()
        {
            return Interlocked.Increment(ref s_LastUsedInstanceId);
        }

        public static WorkItem Create(string path)
        {
            if (s_WorkItemsCache.Any(item => item.OriginalFilePath == path))
            {
                throw new InvalidOperationException("WorkItem already cached");
            }

            var workItem = new WorkItem(GetNextInstanceId(), path);
            s_WorkItemsCache.Add(workItem);
            return workItem;
        }

        public static WorkItem Get(string path)
        {
            return s_WorkItemsCache.SingleOrDefault(item => item.FilePath == path);
        }
    }
}