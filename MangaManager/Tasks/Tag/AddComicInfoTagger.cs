using MangaManager.Models;
using MangaManager.Tasks.Convert;
using MangaManager.Tasks.Rename;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;

namespace MangaManager.Tasks.Tag
{
    public class AddComicInfoTagger : IWorkItemProcessor
    {
        public bool Accept(WorkItem workItem)
        {
            if (!Program.Options.TagRegular && !Program.Options.TagForce)
            {
                return false;
            }

            var archiveInfo = CacheArchiveInfos.GetOrCreate(workItem.FilePath);
            return archiveInfo.IsZip && !archiveInfo.HasSubdirectories && !archiveInfo.IsCalibreArchive && (Program.Options.TagForce || !archiveInfo.HasComicInfo);
        }

        public void Process(WorkItem workItem)
        {
            var file = workItem.FilePath;

            var parsedFileName = FileNameParser.Parse(Path.GetFileNameWithoutExtension(file));
            var serie = parsedFileName.Serie;
            var volume = parsedFileName.Volume;

            var serieInfo = CacheMetadatas.Series.SingleOrDefault(s => s.Aliases.Contains(serie.ToLowerInvariant()));
            if (serieInfo == null)
            {
                Program.View.Error($"Missing metadata: {Path.GetFileName(file)}");
                return;
            }

            var volumeInfo = serieInfo.LastVolume == 0 && volume == 1
                ? serieInfo.Volumes?.SingleOrDefault(v => v.Number == 0)       //One-Shot serie
                : serieInfo.Volumes?.SingleOrDefault(v => v.Number == volume);
            volumeInfo ??= new Volume { Number = volume };

            var comicInfo = ComicInfoHelper.BuildComicInfoFromSerieAndVolume(serieInfo, volumeInfo);

            ArchiveHelper.UpdateZipWithArchiveItemStreams(file, createdItems: new List<ArchiveItemStream> { new ArchiveItemStream { TargetFileName = ComicInfo.NAME, Stream = comicInfo.ToXmlStream() } });
        }
    }
}