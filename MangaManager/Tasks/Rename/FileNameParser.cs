using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace MangaManager.Tasks.Rename
{
    public static class FileNameParser
    {
        public class ParsedFileName
        {
            public string Serie { get; set; }
            public int Volume { get; set; }
            public bool IsTagged { get; set; }
        }

        private const string NonCapturedFileFormatPattern = @"(?:PDF|EPUB|MOBI|AZW\d?|C7Z|CBR|CBZ)";
        private const string NonCapturedLangagePattern = @"(?:FR|French|VF)";        
        private const string NonCapturedVolumePattern = @"(?:T|Tome|V|Vol\.?|Volume|-)\s?";
        private const string NonCapturedIntegralLabelPattern = @"(?:Intégrale|Integrale)";
        private const string NonCapturedEditionLabelPattern = @"(?:Édition|Edition)";
        private const string NonCapturedEditionNamePattern = @"(?:Originale|De\s?luxe|Perfect|Nouvelle|Double|Prestige)";

        private static Regex s_TonerReleaseRegex = new Regex(@$"(\.\d{{4}})?\.FRENCH\.HYBRiD\.COMiC\.{NonCapturedFileFormatPattern}\.eBook-TONER$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex s_NoTagReleaseRegex = new Regex(@$"(\.\d{{4}})?\.FRENCH\.TRAD-OFFICIELLE\.{NonCapturedFileFormatPattern}\.eBook-NoTag$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex s_UnderscoreRegex = new Regex(@"_", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex s_DotRegex = new Regex(@"\.", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex s_PronominousRegex = new Regex(@"^([\w\s-']+)\((Le|La|Les|Un|Une|The|A|An)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex s_QuotedPronominousRegex = new Regex(@"^([\w\s-']+)\((L')\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex s_ParenthesisSurroundedRegex = new Regex(@"\([^\)]*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex s_BracketSurroundedRegex = new Regex(@"\[[^\]]*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex s_TextualVolumeAggregationRegex = new Regex(@$"{NonCapturedVolumePattern}\d+\s?(a|à|et|&|-)\s?({NonCapturedVolumePattern})?\d+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex s_DashedVolumeAggregationAtEndRegex = new Regex(@"\s\d+-\d+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex s_IntegralEditionWithOptionalVolumeNumberRegex = new Regex(@$"{NonCapturedIntegralLabelPattern}(\s\d+\s{NonCapturedVolumePattern})?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex s_NamedEdition1Regex = new Regex(@$"(-\s*)?{NonCapturedEditionLabelPattern}\s{NonCapturedEditionNamePattern}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex s_NamedEdition2Regex = new Regex(@$"(-\s*)?{NonCapturedEditionNamePattern}\s{NonCapturedEditionLabelPattern}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex s_FormatAtEndRegex = new Regex(@$"{NonCapturedFileFormatPattern}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex s_LangageAtEndRegex = new Regex(@$"{NonCapturedLangagePattern}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex s_SerieWithVolumeVolumeIntermediateRegex = new Regex(@$"(.*){NonCapturedVolumePattern}(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex s_SerieWithVolumeVolumeFinalRegex = new Regex(@"(.*)\s(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex s_MultipleSpacesRegex = new Regex(@"\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static ParsedFileName Parse(string fileNameWithoutExtension)
        {
            var isTagged = Regex.IsMatch(fileNameWithoutExtension, @"\[tag\]|\(tag\)", RegexOptions.IgnoreCase);
            var cleanedName = CleanupFileName(fileNameWithoutExtension);

            var result = new ParsedFileName()
            {
                Serie = cleanedName,
                Volume = 1,
                IsTagged = isTagged
            };

            var serieWithVolumeName = DoRegexReplacement(CleanupFileName(fileNameWithoutExtension), s_SerieWithVolumeVolumeIntermediateRegex, "$1 $2");
            if (s_SerieWithVolumeVolumeFinalRegex.IsMatch(serieWithVolumeName))
            {
                result.Serie = DoRegexReplacement(serieWithVolumeName, s_SerieWithVolumeVolumeFinalRegex, "$1");
                result.Volume = int.Parse(DoRegexReplacement(serieWithVolumeName, s_SerieWithVolumeVolumeFinalRegex, "$2"));
            }

            return result;
        }

        private static string CleanupFileName(string filename)
        {
            //Specific release team tagging
            filename = DoRegexReplacement(filename, s_TonerReleaseRegex, "");
            filename = DoRegexReplacement(filename, s_NoTagReleaseRegex, "");

            //Replace dot and underscore by spaces
            filename = DoRegexReplacement(filename, s_UnderscoreRegex, " ");
            filename = DoRegexReplacement(filename, s_DotRegex, " ");

            //"L'/Le/La/Les/The/Un/Une/A" After name moved before name
            filename = DoRegexReplacement(filename, s_PronominousRegex, "$2 $1"); 
            filename = DoRegexReplacement(filename, s_QuotedPronominousRegex, "$2$1");

            //Remove all between parenthesis, brackets
            filename = DoRegexReplacement(filename, s_ParenthesisSurroundedRegex, "");
            filename = DoRegexReplacement(filename, s_BracketSurroundedRegex, "");

            //Aggregation of volumes
            filename = DoRegexReplacement(filename, s_TextualVolumeAggregationRegex, "");
            filename = DoRegexReplacement(filename, s_DashedVolumeAggregationAtEndRegex, "");

            //Replace Edition labels
            filename = DoRegexReplacement(filename, s_IntegralEditionWithOptionalVolumeNumberRegex, ""); 
            filename = DoRegexReplacement(filename, s_NamedEdition1Regex, "");
            filename = DoRegexReplacement(filename, s_NamedEdition2Regex, "");

            //Replace Format/Langage at end
            filename = DoRegexReplacement(filename, s_FormatAtEndRegex, "");
            filename = DoRegexReplacement(filename, s_LangageAtEndRegex, "");

            return filename;
        }

        private static string DoRegexReplacement(string input, Regex regex, string replacement)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var output = input;
            output = regex.Replace(output, replacement);
            output = s_MultipleSpacesRegex.Replace(output, " ");
            output = output.Trim(new[] { ' ', '-', '.' });
            return output;
        }
    }
}