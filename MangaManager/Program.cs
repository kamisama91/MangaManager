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
                var startTime = DateTime.Now;

                var workingFiles = WorkItem.GetAll().ToList();

                if (options.Convert) 
                    RunProcessors(workingFiles, WorkItemProcessors.Converters, View.ConversionProgress);
                
                if (options.Rename) 
                    RunProcessors(workingFiles, WorkItemProcessors.Renamers, View.RenamingProgress);
                
                if (options.Move) 
                    RunProcessors(workingFiles, WorkItemProcessors.Movers, View.MovingProgress);

                if (options.Scrap)
                    RunProcessors(workingFiles, WorkItemProcessors.Scappers, View.ScrappingProgress);

                if (options.Tag)
                    RunProcessors(workingFiles, WorkItemProcessors.Taggers, View.TaggingProgress);

                if (options.OnlineUpdate)
                    RunProcessors(workingFiles, WorkItemProcessors.OnlineLibraryUpdaters, View.OnlineUpdatingProgress);

                if (options.Archive)
                    RunProcessors(workingFiles, WorkItemProcessors.Archivers, View.ArchivingingProgress);

                var duration = DateTime.Now - startTime;
                View.Info($"Finished: {Math.Floor(duration.TotalSeconds / 60)} min {Math.Floor(duration.TotalSeconds % 60)} sec");
            }
            catch (Exception)
            {
                return -1;
            }
            return 0;
        }

        private static void RunProcessors(List<WorkItem> workingFiles, IWorkItemProcessor[] processors, Action<int, int, string> progressGui)
        {
            var startTime = DateTime.Now;
            var currentFileIndex = 0;
            var totalFiles = workingFiles.Count;
            foreach (var workingFile in workingFiles)
            {
                var workingFilePath = workingFile.FilePath;
                progressGui(currentFileIndex++, totalFiles, workingFilePath.Replace(Options.SourceFolder, string.Empty).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                try
                {
                    processors.Where(processor => processor.Accept(workingFile))
                              .ForEach(processor => { if (processor.Process(workingFile)) { workingFile.RestoreLastWriteTime(); } });
                    GC.Collect();
                }
                catch (Exception ex)
                {
                    View.Error($"{Path.GetFileName(workingFilePath)}: {ex.Message}");
                }
            }
            var duration = DateTime.Now - startTime;
            progressGui(currentFileIndex, totalFiles, $"{Math.Floor(duration.TotalSeconds / 60)} min {Math.Floor(duration.TotalSeconds % 60)} sec");
        }
    }
}
