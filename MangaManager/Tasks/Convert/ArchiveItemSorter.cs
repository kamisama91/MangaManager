using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MangaManager.Tasks.Convert
{
    public static class ArchiveItemSorter
    {
        private static Regex s_IsCoverRegex = new Regex(@"^(cover|0+[a-zA-Z]?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex s_ExtractLastDigitsGroupRegex = new Regex(@"^.*?(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static IEnumerable<T> OrderForArchiving<T>(this IEnumerable<T> enumerable, Func<T, string> getPath)
        {
            var items = enumerable
                .Select(t => new
                {
                    Item = t,
                    FullPath = getPath(t),

                    DirectoryName = Path.GetDirectoryName(getPath(t)).ToLowerInvariant(),
                    DirectoryOrder =
                            int.TryParse(Path.GetDirectoryName(getPath(t)), out var folderNum)                                                                      //Folder is a number
                            ? folderNum
                            : int.TryParse(s_ExtractLastDigitsGroupRegex.Replace(Path.GetDirectoryName(getPath(t)), "$1"), out var regexpFolderNum)                 //Then Folder ends with a number
                                ? regexpFolderNum
                                : int.MaxValue,                                                                                                                     //Then Folder alphabetical order

                    FileNameWithoutExtension = Path.GetFileNameWithoutExtension(getPath(t)).ToLowerInvariant(),
                    FileNameWithoutExtensionOrder =
                            s_IsCoverRegex.IsMatch(Path.GetFileNameWithoutExtension(getPath(t)))                                                                    //Cover files always first in its Folder
                            ? 0
                            : int.TryParse(Path.GetFileNameWithoutExtension(getPath(t)), out var fileNum)                                                           //Then File is a number
                                ? fileNum
                                : int.TryParse(s_ExtractLastDigitsGroupRegex.Replace(Path.GetFileNameWithoutExtension(getPath(t)), "$1"), out var regexFileNum)     //Then File ends with a number
                                    ? regexFileNum
                                    : int.MaxValue,                                                                                                                 //Then File alphabetical order
                });

            return items
                .OrderBy(t => t.DirectoryOrder)
                .ThenBy(t => t.DirectoryName)
                .ThenBy(t => t.FileNameWithoutExtensionOrder)
                .ThenBy(t => t.FileNameWithoutExtension)
                .Select(t => t.Item);
        }
    }
}