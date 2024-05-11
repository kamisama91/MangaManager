using MangaManager.Models.ExternalModels.MangaCollec;
using MangaManager.Tasks.HttpClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MangaManager.Tasks.Checker
{
    public class MangaCollecLibraryChecker : IWorkItemProcessor, IWorkItemProvider
    {
        private const string WORKITEM_NAME = "#MangaCollecLibraryChecker#";

        private MangaCollecHttpClient _httpClient = new MangaCollecHttpClient();

        public IEnumerable<WorkItem> GetItems()
        {
            if (Program.Options.OnlineCheck && !string.IsNullOrEmpty(Program.Options.ArchiveFolder))
                yield return CacheWorkItems.Create(WORKITEM_NAME);
        }

        public bool Accept(WorkItem workItem)
        {
            return workItem.FilePath == WORKITEM_NAME;
        }

        public void Process(WorkItem workItem)
        {
            _httpClient.Login();

            var archivedFiles = (Directory.Exists(Program.Options.ArchiveFolder) ? Directory.GetFiles(Program.Options.ArchiveFolder, "*.cbz", SearchOption.AllDirectories) : new string[0])
                .Select(f => Path.GetFileNameWithoutExtension(f).Replace(" (tag)", ""))
                .OrderBy(f => f)
                .ToList();

            var possessions = _httpClient.GetDataStore<MangaCollecUserCollection>("/v2/users/me/collection")
                .Possessions.Select(p => p.VolumeId)
                .Select(id => GetNameFromVolumeId(id))
                .OrderBy(f => f)
                .ToList();

            var logSeparator = $"{Environment.NewLine}   ";
            Program.View.Info($"In library {archivedFiles.Count} / Online: {possessions.Count}");
            Program.View.Warning($"Missing online:{logSeparator}{string.Join($"{logSeparator}", archivedFiles.Except(possessions))}");
            Program.View.Error($"Missing in library:{logSeparator}{string.Join($"{logSeparator}", possessions.Except(archivedFiles))}");
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