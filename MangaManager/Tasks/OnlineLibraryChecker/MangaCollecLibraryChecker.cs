using MangaManager.Models.ExternalModels.MangaCollec;
using MangaManager.Tasks.HttpClient;
using System;
using System.IO;
using System.Linq;

namespace MangaManager.Tasks.OnlineLibraryUpdater
{
    public class MangaCollecLibraryChecker : IWorkItemProcessor
    {
        private MangaCollecHttpClient _httpClient;       

        public MangaCollecLibraryChecker()
        {
            _httpClient = new MangaCollecHttpClient();
            _httpClient.Login();
        }

        public bool Accept(WorkItem workItem)
        {
            return Program.Options.OnlineCheck
                && !string.IsNullOrEmpty(Program.Options.ArchiveFolder)
                && workItem.InstanceId == CacheWorkItems.InstancesCount;
        }

        public void Process(WorkItem workItem)
        {
            var archivedFiles = (Directory.Exists(Program.Options.ArchiveFolder) ? Directory.GetFiles(Program.Options.ArchiveFolder, "*.cbz", SearchOption.AllDirectories) : new string[0])
                .Select(f => Path.GetFileNameWithoutExtension(f).Replace(" (tag)", ""))
                .OrderBy(f => f)            
                .ToList();

            var possessions = _httpClient.GetDataStore<MangaCollecUserCollection>("/v2/users/me/collection")
                .Possessions.Select(p => p.VolumeId)
                .Select(id => GetNameFromVolumeId(id))
                .OrderBy(f => f)           
                .ToList();

            Program.View.Info($"In library {archivedFiles.Count} / Online: {possessions.Count}");
            Program.View.Warning($"Missing online:{Environment.NewLine}   {string.Join(Environment.NewLine, archivedFiles.Except(possessions))}");
            Program.View.Error($"Missing in library:{Environment.NewLine}   {string.Join(Environment.NewLine, possessions.Except(archivedFiles))}");
        }

        private string GetNameFromVolumeId(string id)
        {
            var serieInfo = CacheMetadatas.Series.Single(s => s.Volumes?.Any(v => v.MangaCollecVolumeId == id) ?? false);
            var volumeInfo = serieInfo.Volumes.Single(v => v.MangaCollecVolumeId == id);
            var comicInfo = ComicInfoHelper.BuildComicInfoFromSerieAndVolume(serieInfo, volumeInfo);
            return comicInfo.BuildVolumeNameFromComicInfo();
        }
    }
}