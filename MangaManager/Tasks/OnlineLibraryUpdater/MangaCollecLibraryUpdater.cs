using System.IO;

namespace MangaManager.Tasks.OnlineLibraryUpdater
{
    public class MangaCollecLibraryUpdater : IWorkItemProcessor
    {
        public bool Accept(WorkItem workItem)
        {
            var workingFileName = workItem.FilePath;
            return Path.GetExtension(workingFileName) == ".cbz" && ArchiveHelper.HasComicInfo(workingFileName);
        }

        public bool Process(WorkItem workItem)
        {
            return false;
        }
    }
}