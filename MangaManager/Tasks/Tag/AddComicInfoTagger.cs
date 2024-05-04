using MangaManager.Models;
using MangaManager.Tasks.Convert;
using MangaManager.Tasks.Rename;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;

namespace MangaManager.Tasks.Tag
{
    public class AddComicInfoTagger : IWorkItemProcessor
    {
        public bool Accept(WorkItem workItem)
        {
            if (!Program.Options.Tag)
            {
                return false;
            }

            var archiveInfo = CacheArchiveInfos.GetOrCreate(workItem.FilePath);
            return archiveInfo.IsZip && !archiveInfo.HasSubdirectories && (Program.Options.TagForce || !archiveInfo.HasComicInfo);
        }

        public void Process(WorkItem workItem)
        {
            var file = workItem.FilePath;

            var parsedFileName = FileNameParser.Parse(Path.GetFileNameWithoutExtension(file));
            var serie = parsedFileName.Serie;
            var volume = parsedFileName.Volume;

            ///TODO
            //Matching by GetOsCompliantName => what if many edition for same serie (...)

            var serieInfo = CacheMetadatas.Series.SingleOrDefault(s => s.Alias == serie.ToLowerInvariant() /*||  FileHelper.GetOsCompliantName(s.Name) == serie*/);
            if (serieInfo == null)
            {
                Program.View.Error($"Missing metadata: {Path.GetFileName(file)}");
                return;
            }

            var volumeInfo = serieInfo.Volumes?.SingleOrDefault(v => v.Number == volume);
            var lastVolume = serieInfo.LastVolume.HasValue ? serieInfo.LastVolume : -1;
            var title = !string.IsNullOrEmpty(volumeInfo?.Name) ? $"{serieInfo.Name} - {volumeInfo.Name}" : $"{serieInfo.Name} - Tome {volume}";
            if (serieInfo.LastVolume == 0 && volume == 1)
            {
                //One-Shot serie
                volumeInfo = serieInfo?.Volumes?.SingleOrDefault(v => v.Number == 0);
                lastVolume = 1;
                title = serieInfo.Name;
            }

            var keywords = serieInfo?.Keywords.SelectMany(keyword => CacheMetadatas.Keywords.Where(k => k.Title == keyword)).ToList();
            var genres = (keywords?.Where(k => k.Type == KeywordType.Genre).Select(k => k.Title).ToList()) ?? [];
            var tags = (keywords?.Where(k => k.Type == KeywordType.Tag).Select(k => k.Title).ToList()) ?? [];

            var ageRating = (keywords?.Select(k => k.Rating).DefaultIfEmpty(0).Max() ?? 0) switch
            {
                12 => "PG",
                14 => "M",
                16 => "MA15+",
                18 => "R18+",
                _ => "G",
            };

            var serieGroup = CacheMetadatas.Groups.FirstOrDefault(g => g.Title == serieInfo.Name || g.RelatedTitles.Contains(serieInfo.Name))?.Title ?? string.Empty;

            var comicInfo = new ComicInfo
            {
                Series = HtmlEncoder.Default.Encode(serieInfo.Name ?? string.Empty),
                Title = HtmlEncoder.Default.Encode(title ?? string.Empty),
                Number = volume.ToString(),
                SeriesGroup = HtmlEncoder.Default.Encode(serieGroup ?? string.Empty),
                Imprint = serieInfo.Edition ?? string.Empty,
                Count = lastVolume.ToString(),
                Writer = serieInfo.Writer ?? string.Empty,
                Penciller = serieInfo.Penciler ?? string.Empty,
                Publisher = serieInfo.Publisher ?? string.Empty,
                AgeRating = ageRating,
                Genre = string.Join(',', genres),
                Tags = string.Join(',', tags),
                Summary = string.Empty,
                Year = "0",
                Month = "0",
                Day = "0",
            };

            if (volumeInfo != null)
            {
                comicInfo.Summary = HtmlEncoder.Default.Encode(volumeInfo.Summary ?? string.Empty);
                comicInfo.Year = volumeInfo.ReleaseDate.Year.ToString();
                comicInfo.Month = volumeInfo.ReleaseDate.Month.ToString();
                comicInfo.Day = volumeInfo.ReleaseDate.Day.ToString();
            }

            ArchiveHelper.UpdateZipWithArchiveItemStreams(file, createdItems: new[] { new ArchiveItemStream { FileName = ComicInfo.NAME, Stream = comicInfo.ToXmlStream() } });
        }
    }
}