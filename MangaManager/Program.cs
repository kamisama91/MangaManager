using CommandLine;
using MangaManager.Tasks;
using MangaManager.View;
using SharpCompress;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MangaManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default
                .ParseArguments<ProgramOptions>(args)
                .MapResult(
                    opts => Environment.ExitCode = Run(opts),
                    errs => Environment.ExitCode = -2);
        }
        
        public static ProgramOptions Options { get; private set; }
        public static ProgramView View { get; private set; }

        private static int Run(ProgramOptions options)
        {
            Options = options;
            View = new ProgramView();

            try
            {
                var workingFiles = WorkingFile.GetAll();

                if (options.Convert) 
                    RunProcessors(workingFiles, FileProcessors.Converters, View.ConversionProgress);
                
                if (options.Rename) 
                    RunProcessors(workingFiles, FileProcessors.Renamers, View.RenamingProgress);
                
                if (options.Move) 
                    RunProcessors(workingFiles, FileProcessors.Movers, View.MovingProgress);

                if (options.Scrap)
                    RunProcessors(workingFiles, FileProcessors.Scappers, View.ScrappingProgress);

                if (options.Tag)
                    RunProcessors(workingFiles, FileProcessors.Taggers, View.TaggingProgress);

                if (options.OnlineUpdate)
                    RunProcessors(workingFiles, FileProcessors.OnlineLibraryUpdaters, View.OnlineUpdatingProgress);

                if (options.Archive)
                    RunProcessors(workingFiles, FileProcessors.Archivers, View.ArchivingingProgress);
            }
            catch (Exception)
            {
                return -1;
            }
            return 0;
        }

        private static void RunProcessors(List<WorkingFile> workingFiles, IFileProcessor[] processors, Action<int, int, string> progressGui)
        {
            var startTime = DateTime.Now;
            var currentFileIndex = 0;
            var totalFiles = workingFiles.Count;
            foreach (var workingFile in workingFiles)
            {
                progressGui(currentFileIndex++, totalFiles, workingFile.Filename.Replace(Options.SourceFolder, string.Empty).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                try
                {
                    var lastWriteTime = File.GetLastWriteTime(workingFile.Filename);
                    processors.Where(processor => processor.Accept(workingFile.Filename))
                              .ForEach(processor => { if (processor.ProcessFile(workingFile.Filename, out var newPath)) { workingFile.CurrentFilename = newPath; } });
                    File.SetLastWriteTime(workingFile.Filename, lastWriteTime);
                    GC.Collect();
                }
                catch (Exception ex)
                {
                    View.Error($"{Path.GetFileName(workingFile.Filename)}: {ex.Message}");
                }
            }
            var duration = DateTime.Now - startTime;
            progressGui(currentFileIndex, totalFiles, $"{Math.Floor(duration.TotalSeconds / 60)} min {Math.Floor(duration.TotalSeconds % 60)} sec");
        }
    }
}
