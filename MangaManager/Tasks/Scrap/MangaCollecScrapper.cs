using MangaManager.Models;
using MangaManager.Models.ExternalModels.MangaCollec;
using MangaManager.Tasks.HttpClient;
using MangaManager.Tasks.Rename;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MangaManager.Tasks.Scrap
{
    public class MangaCollecScrapper : IWorkItemProcessor
    {
        private static ConcurrentBag<string> s_IgnoredAlias = new ConcurrentBag<string>();

        private MangaCollecHttpClient _httpClient;

        public MangaCollecScrapper()
        {
            _httpClient = new MangaCollecHttpClient();
        }

        public bool Accept(WorkItem workItem)
        {
            if (!Program.Options.Scrap)
            {
                return false;
            }

            return true;
        }

        public void Process(WorkItem workItem)
        {
            var file = workItem.FilePath;

            var parsedFileName = FileNameParser.Parse(Path.GetFileNameWithoutExtension(file));
            var serie = CacheMetadatas.Series.SingleOrDefault(s => s.Aliases.Contains(parsedFileName.Serie.ToLowerInvariant()));
            if (serie == null)
            {
                CreateSerie(parsedFileName.Serie, parsedFileName.Volume);
            }
            else
            {
                RefreshSerie(serie, parsedFileName.Volume, true);
            }
        }       
        private void CreateSerie(string name, int volume)
        {
            var alias = name.ToLowerInvariant();
            if(s_IgnoredAlias.Contains(alias))
            {
                //Serie has already been ignored
                return;
            }

            var serie = new Serie();

            var acceptedTypes = _httpClient.GetDataStore<MangaCollecType[]>("/v1/types")
                        .Where(t => !t.ToDisplay)
                        .Where(t => t.Title.ToLowerInvariant() != "roman")
                        .ToArray();

            var matchedSeries = _httpClient.GetDataStore<MangaCollecSerie[]>("/v1/series")
                        .Where(s => FormatName(s.Title) == FormatName(alias))
                        .Where(s => acceptedTypes.Any(t => t.Id == s.TypeId))
                        .ToArray();

            var serieId = string.Empty;
            if (matchedSeries.Length == 1)
            {
                serieId = matchedSeries[0].Id;
            }
            else
            {
                if (Program.Options.ScrapAutoIgnore)
                {
                    Program.View.Warning($"Automatically ignored alias from scrapping: {alias}");
                    s_IgnoredAlias.Add(alias);
                    return;
                }

                var message = $"{{{ConsoleColor.White}}}Serie: {{{ConsoleColor.DarkYellow}}}{alias}{Environment.NewLine}";
                for (var i = 0; i < matchedSeries.Length; i++) { message += $"{{{ConsoleColor.White}}}   {i} -> {matchedSeries[i].Title}{Environment.NewLine}"; }
                message += $"{{{ConsoleColor.DarkGray}}}   {matchedSeries.Length} -> MangaCollec ID{Environment.NewLine}";
                message += $"{{{ConsoleColor.DarkGray}}}   {matchedSeries.Length + 1} -> Ignore{Environment.NewLine}";

                var userChoiceStr = Program.View.AskUserInput(message);
                var userChoice = int.TryParse(userChoiceStr, out var _i) ? _i : matchedSeries.Length + 2;
                if(userChoice < matchedSeries.Length)
                {
                    serieId = matchedSeries[userChoice].Id;
                }
                else if(userChoice == matchedSeries.Length) 
                {
                    message = $"{{{ConsoleColor.White}}}Serie: {{{ConsoleColor.DarkYellow}}}{name}{Environment.NewLine}";
                    message += $"{{{ConsoleColor.White}}}   MangaCollec ID?{Environment.NewLine}";
                    serieId = Program.View.AskUserInput(message);
                }
                else
                {
                    s_IgnoredAlias.Add(alias);
                    return;
                }
            }

            var serieDetails = _httpClient.GetDataStore<MangaCollecSerieDetail>($"/v1/series/{serieId}");
            serie.Name = serieDetails.Title;
            serie.Writer = serieDetails.Tasks.Where(t => Regex.IsMatch(t.Job.Title.ToLowerInvariant(), "sc.nario|auteur")).Select(t => $"{t.Author.Name.Trim()} {t.Author.FirstName.Trim()}").FirstOrDefault();
            serie.Penciler = serieDetails.Tasks.Where(t => Regex.IsMatch(t.Job.Title.ToLowerInvariant(), "dessin|auteur")).Select(t => $"{t.Author.Name.Trim()} {t.Author.FirstName.Trim()}").FirstOrDefault();
            serie.Keywords = serieDetails.Kinds.Select(k => k.Title).OrderBy(k => k).ToList();
            serie.MangaCollecSerieId = serieDetails.Id;

            var matchedEditions = serieDetails.Editions
                                .Where(e => string.IsNullOrEmpty(e.Title) || !Regex.IsMatch(e.Title, @"^(Pack|Coffret)$|^(Pack|Coffret)\s+|\s+(Pack|Coffret)$"))
                                .ToArray();

            var editionId = string.Empty;
            if (matchedEditions.Length == 1)
            {
                editionId = matchedEditions[0].Id;
            }
            else
            {
                if (Program.Options.ScrapAutoIgnore)
                {
                    Program.View.Warning($"Automatically ignored alias from scrapping: {alias}");
                    s_IgnoredAlias.Add(alias);
                    return;
                }

                var nbPublishers = matchedEditions.Select(e => e.Publisher.Title).Distinct().Count();
                var message = $"{{{ConsoleColor.White}}}Edition: {{{ConsoleColor.DarkYellow}}}{serie.Name}{Environment.NewLine}";
                for (var i = 0; i < matchedEditions.Length; i++)
                {
                    message += $"{{{ConsoleColor.White}}}   {i} -> {matchedEditions[i].Title ?? "Edition Simple"}";
                    if (nbPublishers > 1) { message += $" ({matchedEditions[i].Publisher.Title})"; }
                    message += $" - {matchedEditions[i].VolumesCount} vol.";
                    if (matchedEditions[i].NotFinished) { message += $" (interrompu)"; }
                    else if ((matchedEditions[i].LastVolumeNumber > 0 && matchedEditions[i].VolumesCount == matchedEditions[i].LastVolumeNumber) || (matchedEditions[i].LastVolumeNumber == 0 && matchedEditions[i].VolumesCount == 1)) { message += $" (complet)"; }
                    message += Environment.NewLine;
                }
                var userChoiceStr = Program.View.AskUserInput(message);
                var userChoice = int.TryParse(userChoiceStr, out var _i) ? _i : matchedSeries.Length + 2;
                if (userChoice < matchedEditions.Length)
                {
                    editionId = matchedEditions[userChoice].Id;
                }
            }

            if (!string.IsNullOrEmpty(editionId))
            {
                var edition = matchedEditions.Single(e => e.Id == editionId);
                serie.Edition = edition.Title ?? "Edition Simple";
                serie.Publisher = edition.Publisher.Title;
                serie.MangaCollecEditionId = edition.Id;
            }
            else
            {
                s_IgnoredAlias.Add(alias);
                return;
            }

            var existingSerie = CacheMetadatas.Series.SingleOrDefault(s => s.MangaCollecEditionId == serie.MangaCollecEditionId && s.MangaCollecEditionId == serie.MangaCollecEditionId);
            if (existingSerie != null)
            {
                serie = existingSerie;
                serie.Aliases.Add(alias);
                serie.Aliases = serie.Aliases.OrderBy(_ => _).ToList();
            }
            else
            {
                serie.Aliases = new List<string> { alias };
                CacheMetadatas.Series.Add(serie);
            }

            RefreshSerie(serie, volume, false);            
            CacheMetadatas.SaveSeries();
        }
        private void RefreshSerie(Serie serie, int volume, bool withSave)
        {
            //Ignore serie without edition Id
            if (string.IsNullOrEmpty(serie.MangaCollecEditionId))
            {
                return;
            }

            //Last refesh is less than 15 days old and parsed volume is already fetched or it is one-shot
            if (DateTime.Today.AddDays(-15) < serie.LastUpdateDate
                && (serie.LastVolume == 0 || serie.Volumes.Any(v => v.Number == volume)))
            {
                return;
            }

            var edition = _httpClient.GetDataStore<MangaCollecEdition>($"/v1/editions/{serie.MangaCollecEditionId}");
            var volumes = edition.Volumes
                    .Where(v => v.ReleaseDate.HasValue && v.ReleaseDate <= DateTime.Today)
                    .Select(v => _httpClient.GetDataStore<MangaCollecVolumeDetail>($"/v1/volumes/{v.Id}"))
                    .Select(v => new Volume()
                    {
                        Number = v.Number,
                        Name = !string.IsNullOrEmpty(v.Title) ? v.Title : $"Tome {v.Number}",
                        Summary = v.Content,
                        ReleaseDate = v.ReleaseDate.Value,
                        ISBN = v.ISBN,
                        MangaCollecVolumeId = v.Id
                    })
                    .OrderBy(v => v.Number)
                    .ToList();
                        
            serie.LastVolume = edition.LastVolumeNumber;
            serie.IsInterrupted = edition.NotFinished;
            serie.Volumes = volumes;
            serie.LastUpdateDate = DateTime.Today;

            if (withSave)
            {
                CacheMetadatas.SaveSeries();
            }            
        }

        private static string RemoveDiacritics(string inputString)
        {
            var replaceTable = new Dictionary<string, string>() {
                { "ß", "ss" },
                { "à", "a" },
                { "á", "a" },
                { "â", "a" },
                { "ã", "a" },
                { "ä", "a" },
                { "å", "a" },
                { "æ", "ae" },
                { "ç", "c" },
                { "è", "e" },
                { "é", "e" },
                { "ê", "e" },
                { "ë", "e" },
                { "ì", "i" },
                { "í", "i" },
                { "î", "i" },
                { "ï", "i" },
                { "ð", "d" },
                { "ñ", "n" },
                { "ò", "o" },
                { "ó", "o" },
                { "ô", "o" },
                { "õ", "o" },
                { "ö", "o" },
                { "ø", "o" },
                { "ù", "u" },
                { "ú", "u" },
                { "û", "u" },
                { "ü", "u" },
                { "ý", "y" },
                { "þ", "p" },
                { "ÿ", "y" }
            };
            foreach (var key in replaceTable.Keys)
            {
                inputString.Replace(key, replaceTable[key]);
            }
            return inputString;
        }
        private static string FormatName(string name)
        {
            name = name.ToLowerInvariant();
            name = RemoveDiacritics(name);
            name = name.Replace("&", "and");
            name = Regex.Replace(name, @"[^a-z0-9]", "");
            return name;
        }
    }
}