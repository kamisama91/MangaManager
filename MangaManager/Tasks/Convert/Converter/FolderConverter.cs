using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using IronPython.Runtime;
using Microsoft.Scripting.Utils;
using MangaManager.Models;

namespace MangaManager.Tasks.Convert.Converter
{
    public class FolderConverter : IFileProvider, IFileProcessor
    {
        public string[] GetFiles()
        {
            return Directory.EnumerateDirectories(Program.Options.SourceFolder, "*", SearchOption.AllDirectories)
                .Where(folder => (string.IsNullOrEmpty(Program.Options.ArchiveFolder) || folder.TrimEnd(Path.DirectorySeparatorChar) != Program.Options.ArchiveFolder.TrimEnd(Path.DirectorySeparatorChar))
                              && (string.IsNullOrEmpty(Program.Options.QuarantineFolder) || folder.TrimEnd(Path.DirectorySeparatorChar) != Program.Options.QuarantineFolder.TrimEnd(Path.DirectorySeparatorChar)))
                .Where(folder => !Directory.EnumerateDirectories(folder, "*", SearchOption.TopDirectoryOnly).Any())
                .Where(folder => Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly).All(file => ImageDetection.TryGetImageExtensionFromFile(file, out var _) || Path.GetFileName(file).Equals(ComicInfo.NAME, StringComparison.InvariantCultureIgnoreCase)))
                .OrderBy(_ => _)
                .ToArray();
        }

        public bool Accept(string file)
        {
            return Directory.Exists(file);
        }

        private IEnumerable<ArchiveItemStream> GetArchiveItemStreams(string folder)
        {
            return Directory
                .EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly)
                .OrderBy(file => int.TryParse(Path.GetFileNameWithoutExtension(file), out var pageNumber) ? pageNumber : 0)
                .ThenBy(file => file)
                .Select(file =>
                {
                    using var inputStream = File.OpenRead(file);
                    var ms = new MemoryStream();
                    inputStream.CopyTo(ms);
                    if (ms.TryGetImageExtension(out var extension))
                    {
                        return new ArchiveItemStream { Stream = ms, Extension = extension };
                        
                    }
                    else if(Path.GetFileName(file).Equals(ComicInfo.NAME, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return new ArchiveItemStream { Stream = ms, FileName = ComicInfo.NAME };
                    }
                    else
                    {
                        throw new FormatException();
                    }
                });
        }

        public bool ProcessFile(string folder, out string newFile)
        {
            var outputPath = FileHelper.GetAvailableFilename($"{folder}.cbz");
            var isSuccess = ArchiveHelper.CreateZipFromArchiveItemStreams(folder, outputPath, GetArchiveItemStreams);
            if (isSuccess)
            {
                Directory.Delete(folder, true);
            }
            newFile = outputPath;
            return isSuccess;
        }
    }
}