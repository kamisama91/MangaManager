using MangaManager.Models.ExternalModels.MangaCollec;
using MangaManager.Tasks.Scrap;
using SharpCompress;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace MangaManager.Tasks.OnlineLibraryUpdater
{
    public class MangaCollecLibraryUpdater : IWorkItemProcessor
    {
        private ConcurrentBag<string> s_PossessionsIds = new ConcurrentBag<string>();

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
            InitializePossessions();
                        
            var comicInfo = CacheArchiveInfos.GetOrCreate(workItem.FilePath).ComicInfo;
            var serie = CacheMetadatas.Series.SingleOrDefault(s => s.Name == System.Net.WebUtility.HtmlDecode(comicInfo.Series) && s.Publisher == comicInfo.Publisher && s.Edition == comicInfo.Imprint);
            var volume = serie?.Volumes.SingleOrDefault(v => v.Number.ToString() == comicInfo.Number);
            var volumeId = volume?.MangaCollecVolumeId;

            if (!string.IsNullOrEmpty(volumeId))
            {
                AddPossession(volumeId);
            }
            else
            {
                Program.View.Error($"Unknown volume: {Path.GetFileName(workItem.FilePath)}");
            }
        }

        private void InitializePossessions()
        {
            if (s_PossessionsIds.Count == 0)
            {
                lock (s_PossessionsIds)
                {
                    if (s_PossessionsIds.Count == 0)
                    {
                        MangaCollecHttpClients.User.GetDataStore<MangaCollecUserCollection>("/v2/users/me/collection")
                            .Possessions.Select(p => p.VolumeId)
                            .ForEach(s_PossessionsIds.Add);
                    }
                }
            }
        }

        private void AddPossession(string volumeId)
        {
            if (!s_PossessionsIds.Contains(volumeId))
            {
                var newPossessions = new MangaCollecUserPossession[] { new() { VolumeId = volumeId } };

                MangaCollecHttpClients.User.Post<MangaCollecUserPossession[], MangaCollecUserCollection>("/v1/possessions_multiple", newPossessions)
                                .Possessions.Select(p => p.VolumeId)
                                .ForEach(s_PossessionsIds.Add);
            }
        }
    }
}