using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace MangaManager.Tasks.Rename
{
    public class FromFileNameRenamer : IFileProcessor
    {
        public bool Accept(string file)
        {
            return Path.GetExtension(file) == ".cbz";
        }

        private const string NonCapturedVolumePattern = @"(?:T|Tome|V|Vol\.?|Volume|-)\s?";

        private static string DoRegexReplacement(string input, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern, string replacement)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var output = input;
            output = Regex.Replace(output, pattern, replacement, RegexOptions.IgnoreCase);
            output = Regex.Replace(output, @"\s+", " ", RegexOptions.IgnoreCase);
            output = output.Trim(new[] { ' ', '-' });
            return output;
        }

        private static string CleanupFileName(string filename)
        {
            //Specific release team tagging
            filename = DoRegexReplacement(filename, @"(\.\d{4})?\.FRENCH\.HYBRiD\.COMiC\.(CBZ|CBR|PDF)\.eBook-TONER$", "");         //Toner Release
            filename = DoRegexReplacement(filename, @"(\.\d{4})?\.FRENCH\.TRAD-OFFICIELLE\.(CBZ|CBR|PDF)\.eBook-NoTag$", "");       //NoTag Release

            //Replace dot and underscore by spaces
            filename = DoRegexReplacement(filename, @"_", " ");
            filename = DoRegexReplacement(filename, @"\.", " ");

            //"L'/Le/La/Les/The/Un/Une/A" After name moved before name
            filename = DoRegexReplacement(filename, @"^([\w\s-']+)\((L')\)", "$2$1");
            filename = DoRegexReplacement(filename, @"^([\w\s-']+)\((Le|La|Les|Un|Une|The|A)\)", "$2 $1");

            //Remove all between parenthesis, brackets
            filename = DoRegexReplacement(filename, @"\([^\)]*\)", "");
            filename = DoRegexReplacement(filename, @"\[[^\]]*\]", "");

            //Aggregation of volumes
            filename = DoRegexReplacement(filename, @$"(Intégrale|Integrale)(\s\d+\s{NonCapturedVolumePattern})?", "");
            filename = DoRegexReplacement(filename, @$"{NonCapturedVolumePattern}\d+\s?(a|à|et|&|-)\s?({NonCapturedVolumePattern})?\d+", "");
            filename = DoRegexReplacement(filename, @"\s\d+-\d+$", "");

            //Replace Edition labels
            filename = DoRegexReplacement(filename, @"(-\s*)?(Édition|Edition)\s(Originale|De\s?luxe|Perfect|Nouvelle|Double|Prestige)", "");
            filename = DoRegexReplacement(filename, @"(-\s*)?(Originale|De\s?luxe|Perfect|Nouvelle|Double|Prestige)\s(Édition|Edition)", "");

            //Replace Format/Langage at end
            filename = DoRegexReplacement(filename, @"(Pdf|Epub|Mobi|Azw\d?|C7z|Cbr|Cbz)$", "");
            filename = DoRegexReplacement(filename, @"(FR|French|VF)$", "");

            return filename;
        }

        public static string ExtractSerie(string filename)
        {
            filename = DoRegexReplacement(CleanupFileName(filename), @$"(.*){NonCapturedVolumePattern}(\d+)$", "$1 $2");
            return Regex.IsMatch(filename, @"(.*)\s(\d+)$", RegexOptions.IgnoreCase) ? DoRegexReplacement(filename, @"(.*)\s(\d+)$", "$1") : filename;
        }

        public static int ExtractVolume(string filename)
        {
            filename = DoRegexReplacement(CleanupFileName(filename), @$"(.*){NonCapturedVolumePattern}(\d+)$", "$1 $2");
            return Regex.IsMatch(filename, @"(.*)\s(\d+)$", RegexOptions.IgnoreCase) ? int.Parse(DoRegexReplacement(filename, @"(.*)\s(\d+)$", "$2")) : 1;
        }

        public bool ProcessFile(string file, out string newFile)
        {
            var folder = Path.GetDirectoryName(file);
            var filename = Path.GetFileNameWithoutExtension(file);
            var extension = Path.GetExtension(file);

            //Check file is flagged as Tagged
            var tagSuffix = Regex.IsMatch(filename, @"\[tag\]|\(tag\)", RegexOptions.IgnoreCase) ? " (tag)" : string.Empty;

            //Enrich filename whith prent folders names
            var enrichedFilename = filename;
            var parentFolder = folder;
            while (parentFolder.TrimEnd(Path.DirectorySeparatorChar) != Program.Options.SourceFolder.TrimEnd(Path.DirectorySeparatorChar)
                && (string.IsNullOrEmpty(Program.Options.ArchiveFolder) || parentFolder.TrimEnd(Path.DirectorySeparatorChar) != Program.Options.ArchiveFolder.TrimEnd(Path.DirectorySeparatorChar))
                && (string.IsNullOrEmpty(Program.Options.QuarantineFolder) || parentFolder.TrimEnd(Path.DirectorySeparatorChar) != Program.Options.QuarantineFolder.TrimEnd(Path.DirectorySeparatorChar)))
            {
                var parentFolderSerie = ExtractSerie(Path.GetFileNameWithoutExtension(parentFolder));
                if (!enrichedFilename.Contains(parentFolderSerie, StringComparison.InvariantCultureIgnoreCase))
                {
                    enrichedFilename = $"{parentFolderSerie} {enrichedFilename}";
                }
                parentFolder = Path.GetFullPath(Path.Combine(parentFolder, ".."));
            }

            //Extract Serie and Volume from name
            var serie = ExtractSerie(enrichedFilename);
            var volume = ExtractVolume(enrichedFilename);

            //Rename file
            var renamedPath = Path.Combine(folder, $"{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(serie)} T{volume:D2}{tagSuffix}{extension}");
            if (!file.Equals(renamedPath, StringComparison.InvariantCultureIgnoreCase))
            {
                var workingPath = file.Replace($".cbz", ".cbz.tmp");
                FileHelper.Move(file, workingPath);
                renamedPath = FileHelper.GetAvailableFilename(renamedPath);
                FileHelper.Move(workingPath, renamedPath);
            }
            newFile = renamedPath;
            return true;
        }
    }
}