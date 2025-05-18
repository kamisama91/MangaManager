using System;
using System.IO;
using System.Linq;
using SharpCompress;

namespace MangaManager.Tasks
{
    public static class FileHelper
    {
        public static string CreateUniqueTempDirectory()
        {
            while (true)
            {
                var uniqueTempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
                if (!Directory.Exists(uniqueTempDir))
                {
                    Directory.CreateDirectory(uniqueTempDir);
                    return uniqueTempDir;
                }
            }
        }

        public static string GetAvailableFilename(string expectedFilename)
        {
            if (!File.Exists(expectedFilename))
            {
                return expectedFilename;
            }

            var folder = Path.GetDirectoryName(expectedFilename);
            var filename = Path.GetFileNameWithoutExtension(expectedFilename);
            var extension = Path.GetExtension(expectedFilename);

            var counter = 2;
            string newFilename;
            do
            {
                newFilename = Path.Combine(folder, $"{filename} ({counter}){extension}");
                counter++;
            } while (File.Exists(newFilename));


            Program.ViewController.Warning($"{Path.GetFileName(expectedFilename)} already used, using {Path.GetFileName(newFilename)}");
            return newFilename;
        }

        //Use Windows version of Path.GetInvalidFileNameChars, Unix is just a subset ('/' and '\0')
        private static char[] GetInvalidFileNameChars() => new char[]
        {
            '\"', '<', '>', '|', '\0',
            (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
            (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
            (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
            (char)31, ':', '*', '?', '\\', '/'
        };

        public static string GetOsCompliantName (string filename)
        {
            var osCompliantName = filename;
            GetInvalidFileNameChars().ForEach(ch => { osCompliantName = osCompliantName.Replace(ch, ' '); });
            while (osCompliantName.Contains("  ")) osCompliantName = osCompliantName.Replace("  ", " ");
            return osCompliantName;
        }

        public static void Move(string path, string newPath)
        {
            if (newPath == path)
            {
                return;
            }

            if (Directory.Exists(path))
            {
                if (Directory.Exists(newPath))
                {
                    throw new InvalidOperationException($"{newPath} already exists");
                }

                Directory.Move(path, newPath);
                Directory.EnumerateFiles(newPath)
                         .ForEach(file =>
                         {
                             var sourceFile = file.Replace(newPath, path);
                             CacheArchiveInfos.UpdatePath(sourceFile, file);
                             CacheWorkItems.Get(sourceFile)?.UpdatePath(file);
                         });
            }
            else if (File.Exists(path))
            {
                if (File.Exists(newPath))
                {
                    throw new InvalidOperationException($"{newPath} already exists");
                }

                File.Move(path, newPath);
                CacheArchiveInfos.UpdatePath(path, newPath);
                CacheWorkItems.Get(path)?.UpdatePath(newPath);

                var sourcefolder = Path.GetDirectoryName(path);
                while (sourcefolder.TrimEnd(Path.DirectorySeparatorChar) != Program.Options.SourceFolder.TrimEnd(Path.DirectorySeparatorChar)
                    && (string.IsNullOrEmpty(Program.Options.ArchiveFolder) || sourcefolder.TrimEnd(Path.DirectorySeparatorChar) != Program.Options.ArchiveFolder.TrimEnd(Path.DirectorySeparatorChar))
                    && (string.IsNullOrEmpty(Program.Options.QuarantineFolder) || sourcefolder.TrimEnd(Path.DirectorySeparatorChar) != Program.Options.QuarantineFolder.TrimEnd(Path.DirectorySeparatorChar)))
                {
                    if (Directory.EnumerateFiles(sourcefolder).Any() || Directory.EnumerateDirectories(sourcefolder).Any())
                    {
                        //Not empty, no need to continue
                        break;
                    }

                    //Cleanup Empty source folder
                    Directory.Delete(sourcefolder, false);
                    sourcefolder = Path.GetFullPath(Path.Combine(sourcefolder, ".."));
                }
            }
        }
    }
}