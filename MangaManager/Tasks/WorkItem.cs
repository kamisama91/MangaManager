using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MangaManager.Tasks
{
    public class WorkItem
    {
        public static IEnumerable<WorkItem> GetAll()
        {
            //Only accept convertible files
            return WorkItemProcessors.Converters
                    .OfType<IWorkItemProvider>()
                    .SelectMany(convertor => convertor.GetItems())
                    .Distinct()
                    .OrderBy(item => item.OriginalFilePath);
        }

        public WorkItem(string filePath)
        {
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