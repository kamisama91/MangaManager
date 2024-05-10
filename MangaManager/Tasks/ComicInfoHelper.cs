using System;
using System.Collections.Generic;
using System.Linq;
using MangaManager.Models;

namespace MangaManager.Tasks
{
    public static class ComicInfoHelper
    {
        public static ComicInfo BuildComicInfoFromSerieAndVolume(Serie serieInfo, Volume volumeInfo)
        {
            int volume = volumeInfo?.Number ?? 1;
            var lastVolume = serieInfo.LastVolume.HasValue ? serieInfo.LastVolume : -1;
            var title = !string.IsNullOrEmpty(volumeInfo?.Name) ? $"{serieInfo.Name} - {volumeInfo.Name}" : $"{serieInfo.Name} - Tome {volumeInfo.Number}";
            if (serieInfo.LastVolume == 0 && volumeInfo.Number == 0)
            {
                //One-Shot serie
                volume = 1; 
                lastVolume = 1;                
                title = serieInfo.Name;
            }

            var keywords = serieInfo?.Keywords.SelectMany(keyword => CacheMetadatas.Keywords.Where(k => k.Title == keyword)).ToList();
            var genres = (keywords?.Where(k => k.Type == KeywordType.Genre).Select(k => k.Title).ToList()) ?? new List<string>();
            var tags = (keywords?.Where(k => k.Type == KeywordType.Tag).Select(k => k.Title).ToList()) ?? new List<string>();

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
                Series = serieInfo.Name ?? string.Empty,
                Title = title ?? string.Empty,
                Number = volume.ToString(),
                SeriesGroup = serieGroup ?? string.Empty,
                Imprint = serieInfo.Edition ?? string.Empty,
                Count = lastVolume.ToString(),
                Writer = serieInfo.Writer ?? string.Empty,
                Penciller = serieInfo.Penciler ?? string.Empty,
                Publisher = serieInfo.Publisher ?? string.Empty,
                AgeRating = ageRating,
                Genre = string.Join(',', genres),
                Tags = string.Join(',', tags),
                Summary = volumeInfo?.Summary ?? string.Empty,
                Year = "0",
                Month = "0",
                Day = "0",
            };

            if (volumeInfo?.ReleaseDate != null)
            {
                comicInfo.Year = volumeInfo.ReleaseDate.Value.Year.ToString();
                comicInfo.Month = volumeInfo.ReleaseDate.Value.Month.ToString();
                comicInfo.Day = volumeInfo.ReleaseDate.Value.Day.ToString();
            }

            return comicInfo;
        }

        public static string BuildSerieNameFromComicInfo(this ComicInfo info)
        {
            ///TODO
            //Intermediate Folders

            var serie = info.Series;
            var authors = string.Join(" - ", new[] { info.Writer, info.Penciller }.Where(p => !string.IsNullOrEmpty(p)).Select(p => p.Split(' ').First()).Distinct());
            var publisher = info.Publisher;
            var edition = !string.IsNullOrEmpty(info.Imprint) && info.Imprint != "Edition Simple" ? $" - {info.Imprint}" : string.Empty;

            var name = serie;
            if (!string.IsNullOrEmpty(authors)) { name += $" ({authors})"; }
            if (!string.IsNullOrEmpty(publisher)) { name += $" ({publisher}{edition})"; }

            name = FileHelper.GetOsCompliantName(name);

            return name;
        }

        public static string BuildVolumeNameFromComicInfo(this ComicInfo info)
        {
            var volume = int.Parse(info.Number);
            var lastVolume = !string.IsNullOrEmpty(info.Count) ? int.Parse(info.Count) : 0;
            var length = lastVolume > 0
                ? 1 + (int)Math.Floor(Math.Log10(lastVolume))
                : 2;
            return BuildSerieNameFromComicInfo(info) + $" T{volume.ToString("N0").PadLeft(length, '0')}";
        }
    }
}