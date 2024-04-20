using System.IO;

namespace MangaManager.Tasks.OnlineLibraryUpdater
{
    public class MangaCollecLibraryUpdater : IWorkItemProcessor
    {
        public bool Accept(WorkItem workItem)
        {
            if (!Program.Options.OnlineUpdate)
            {
                return false;
            }

            var workingFileName = workItem.FilePath;
            return Path.GetExtension(workingFileName) == ".cbz" && ArchiveHelper.HasComicInfo(workingFileName);
        }

        public bool Process(WorkItem workItem)
        {
            return false;
        }
    }
}