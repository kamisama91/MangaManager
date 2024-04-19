using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MangaManager.Tasks
{
    public class WorkingFile
    {
        public static List<WorkingFile> GetAll()
        {
            //Only accept convertible files
            return FileProcessors.Converters
                    .OfType<IFileProvider>()
                    .SelectMany(convertor => convertor.GetFiles())
                    .Distinct()
                    .OrderBy(_ => _)
                    .Select (file => new WorkingFile { OriginalFilename = file })
                    .ToList();
        }


        public string OriginalFilename { get; private set; }
        public string CurrentFilename { get; set; }

        public string Filename => !string.IsNullOrEmpty(CurrentFilename) && File.Exists(CurrentFilename) ? CurrentFilename : OriginalFilename;
    }
}