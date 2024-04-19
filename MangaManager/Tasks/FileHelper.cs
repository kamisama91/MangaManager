using System;
using System.IO;
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


            Program.View.Warning($"{Path.GetFileName(expectedFilename)} already used, using {Path.GetFileName(newFilename)}");
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

        public static void Move(string filePath, string newFilePath)
        {
            if (newFilePath == filePath)
            {
                return;
            }

            if (File.Exists(newFilePath))
            {
                throw new InvalidOperationException($"{newFilePath} already exists");
            }

            File.Move(filePath, newFilePath);

            //Cleanup empty Source directory tree
            var sourcefolder = Path.GetDirectoryName(filePath);
            while (sourcefolder.TrimEnd(Path.DirectorySeparatorChar) != Program.Options.SourceFolder.TrimEnd(Path.DirectorySeparatorChar)
                && (string.IsNullOrEmpty(Program.Options.ArchiveFolder) || sourcefolder.TrimEnd(Path.DirectorySeparatorChar) != Program.Options.ArchiveFolder.TrimEnd(Path.DirectorySeparatorChar))
                && (string.IsNullOrEmpty(Program.Options.QuarantineFolder) || sourcefolder.TrimEnd(Path.DirectorySeparatorChar) != Program.Options.QuarantineFolder.TrimEnd(Path.DirectorySeparatorChar)))
            {
                //Cleanup Empty source folder
                if (Directory.GetFiles(sourcefolder).Length == 0
                 && Directory.GetDirectories(sourcefolder).Length == 0)
                {
                    Directory.Delete(sourcefolder, false);
                }
                sourcefolder = Path.GetFullPath(Path.Combine(sourcefolder, ".."));
            }

            CacheComicInfos.UpdatePath(filePath, newFilePath);
        }
    }
}