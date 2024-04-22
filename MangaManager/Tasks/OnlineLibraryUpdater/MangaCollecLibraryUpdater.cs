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

            var archiveInfo = CacheArchiveInfos.GetOrCreate(workItem.FilePath);
            return archiveInfo.IsZip && !archiveInfo.HasSubdirectories && archiveInfo.HasComicInfo;
        }

        public void Process(WorkItem workItem)
        {
        }
    }
}