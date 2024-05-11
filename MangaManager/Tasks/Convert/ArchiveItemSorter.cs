using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MangaManager.Tasks.Convert
{
    public static class ArchiveItemSorter
    {
        private static Regex s_ExtractLastDigitsGroupRegex = new Regex(@"^.*?(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static IEnumerable<T> OrderForArchiving<T>(this IEnumerable<T> enumerable, Func<T, string> getPath)
        {
            return enumerable
                .OrderBy(t => int.TryParse(Path.GetDirectoryName(getPath(t)), out var folderNum) ? folderNum : int.MaxValue)                                                        //Folder is a number
                .ThenBy(t => int.TryParse(s_ExtractLastDigitsGroupRegex.Replace(Path.GetDirectoryName(getPath(t)), "$1"), out var folderNum) ? folderNum : int.MaxValue)            //Then Folder ends with a number
                .ThenBy(t => Path.GetDirectoryName(getPath(t)).ToLowerInvariant())                                                                                                  //Then Folder alphabetical order
                .ThenBy(t => Path.GetFileNameWithoutExtension(getPath(t)).Equals("cover", StringComparison.InvariantCultureIgnoreCase) ? 0 : int.MaxValue)                          //Then "cover" file always first in its Folder
                .ThenBy(t => int.TryParse(Path.GetFileNameWithoutExtension(getPath(t)), out var fileNum) ? fileNum : int.MaxValue)                                                  //Then File is a number in its Folder
                .ThenBy(t => int.TryParse(s_ExtractLastDigitsGroupRegex.Replace(Path.GetFileNameWithoutExtension(getPath(t)), "$1"), out var fileNum) ? fileNum : int.MaxValue)     //Then File ends with a a number in its Folder
                .ThenBy(t => Path.GetFileNameWithoutExtension(getPath(t)).ToLowerInvariant());                                                                                      //Then File alphabetical order in its Folder
        }
    }
}