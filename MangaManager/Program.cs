using CommandLine;
using MangaManager.Tasks;
using MangaManager.View;
using SharpCompress;
using System;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MangaManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default
                .ParseArguments<ProgramOptions>(args)
                .MapResult(
                    opts => Environment.ExitCode = Run(opts).Result,
                    errs => Environment.ExitCode = -2);
        }
        
        public static ProgramOptions Options { get; private set; }
        public static ProgramView View { get; private set; }

        private static async Task<int> Run(ProgramOptions options)
        {
            Options = options;
            View = new ProgramView();

            try
            {
                //Create Workers producer
                var producer = new WorkItemsProducer { Providers = WorkItemProcessors.Converters.OfType<IWorkItemProvider>().ToArray() };

                //Create Workers consumers
                var consumers = new WorkItemsConsumer[]
                {
                    new() { Processors = WorkItemProcessors.Converters, ProgressGui = View.ConversionProgress },
                    new() { Processors = WorkItemProcessors.Renamers, ProgressGui = View.RenamingProgress },
                    new() { Processors = WorkItemProcessors.Movers, ProgressGui = View.MovingProgress },
                    new() { Processors = WorkItemProcessors.Scappers, ProgressGui = View.ScrappingProgress },
                    new() { Processors = WorkItemProcessors.Taggers, ProgressGui = View.TaggingProgress },
                    new() { Processors = WorkItemProcessors.OnlineLibraryUpdaters, ProgressGui = View.OnlineUpdatingProgress },
                    new() { Processors = WorkItemProcessors.Archivers, ProgressGui = View.ArchivingingProgress },
                };

                //Initialize Channels between producer and consumers
                var lastChannel = (Channel<WorkItem>)null;
                producer.WorkItemsWriter = (lastChannel = Channel.CreateUnbounded<WorkItem>()).Writer;
                foreach (var consumer in consumers)
                {
                    consumer.WorkItemsReader = lastChannel.Reader;
                    consumer.WorkItemsWriter = (lastChannel = Channel.CreateUnbounded<WorkItem>()).Writer;
                }

                var startTime = DateTime.Now;

                //Producing messages and wait for end of consumming
                var consumerTasks = consumers.Select(c => c.ConsumeData()).ToArray();
                await producer.BeginProducing();
                await Task.WhenAll(consumerTasks);

                var duration = DateTime.Now - startTime;
                View.Info($"Finished: {Math.Floor(duration.TotalSeconds / 60)} min {Math.Floor(duration.TotalSeconds % 60)} sec");
            }
            catch (Exception)
            {
                return -1;
            }
            return 0;
        }

        private class WorkItemsProducer()
        {
            public ChannelWriter<WorkItem> WorkItemsWriter { get; set; }

            public IWorkItemProvider[] Providers { get; set; }

            public async Task BeginProducing()
            {
                var workItems = Providers.SelectMany(convertor => convertor.GetItems())
                                         .OrderBy(item => item.OriginalFilePath);

                foreach (var workItem in workItems)
                {
                    await WorkItemsWriter.WriteAsync(workItem);
                }
                WorkItemsWriter.Complete();
            }
        }

        private class WorkItemsConsumer()
        {
            public ChannelReader<WorkItem> WorkItemsReader { get; set; }
            public ChannelWriter<WorkItem> WorkItemsWriter { get; set; }

            public IWorkItemProcessor[] Processors { get; set; }
            public Action<int, int, string> ProgressGui { get; set; }

            public async Task ConsumeData()
            {
                var totalDuration = new TimeSpan();
                var numberOfProcessedItems = 0;

                while (await WorkItemsReader.WaitToReadAsync())
                {
                    if (WorkItemsReader.TryRead(out var workItem))
                    {
                        var startTime = DateTime.Now;

                        var workingFilePath = workItem.FilePath.Replace(Options.SourceFolder, string.Empty).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        ProgressGui(numberOfProcessedItems++, WorkItem.InstancesCount, $"{{DarkBlue}}DOING:  {{Default}}{workingFilePath}");
                        try
                        {
                            Processors.Where(processor => processor.Accept(workItem))
                                      .ForEach(processor => { if (processor.Process(workItem)) { workItem.RestoreLastWriteTime(); } });
                            GC.Collect();
                        }
                        catch (Exception ex)
                        {
                            View.Error($"{Path.GetFileName(workingFilePath)}: {ex.Message}");
                        }
                        ProgressGui(numberOfProcessedItems, WorkItem.InstancesCount, $"{{Green}}DONE:   {{Default}}{workingFilePath}");

                        var stepDuration = DateTime.Now - startTime;
                        totalDuration = totalDuration.Add(stepDuration);

                        await WorkItemsWriter.WriteAsync(workItem);
                    }
                }

                ProgressGui(numberOfProcessedItems, WorkItem.InstancesCount, $"{Math.Floor(totalDuration.TotalSeconds / 60)} min {Math.Floor(totalDuration.TotalSeconds % 60)} sec");
                WorkItemsWriter.Complete();
            }
        }
    }
}
