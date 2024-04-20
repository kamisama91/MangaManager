using CommandLine;
using MangaManager.Tasks;
using MangaManager.View;
using SharpCompress;
using System;
using System.Collections.Generic;
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
                .ParseArguments<Options>(args)
                .MapResult(
                    opts => Environment.ExitCode = Run(opts).Result,
                    errs => Environment.ExitCode = -2);
        }
        
        public static Options Options { get; private set; }
        public static View.View View { get; private set; }

        private static async Task<int> Run(Options options)
        {
            Options = options;
            View = new View.View();

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

                var startTime = DateTime.Now;

                if (Features.UseMultiThreading)
                    await RunMultiThreads(producer, consumers);
                else
                    RunSingleThread(producer, consumers);

                var duration = DateTime.Now - startTime;
                View.Info($"Finished:");
                View.Info($"   Time:  {Math.Floor(duration.TotalSeconds / 60)} min {Math.Floor(duration.TotalSeconds % 60)} sec");
                View.Info($"   Items: {WorkItem.InstancesCount}");
                View.Info($"   Cache: {CacheArchiveInfos.Hits} Hits / {CacheArchiveInfos.Misses} Misses");
            }
            catch (Exception)
            {
                return -1;
            }
            return 0;
        }

        private static void RunSingleThread(WorkItemsProducer producer, WorkItemsConsumer[] consumers)
        {
            var workItems = GetAllItems(producer.Providers).ToArray();
            foreach (var consumer in consumers)
            {
                var totalDuration = new TimeSpan();
                var numberOfProcessedItems = 0;
                foreach (var workItem in workItems)
                {
                    var stepDuration = ProcessItem(workItem, numberOfProcessedItems++, consumer.Processors, consumer.ProgressGui);
                    totalDuration = totalDuration.Add(stepDuration);
                }
                consumer.ProgressGui(numberOfProcessedItems, WorkItem.InstancesCount, $"{Math.Floor(totalDuration.TotalSeconds / 60)} min {Math.Floor(totalDuration.TotalSeconds % 60)} sec");
            }
        }
        private static async Task RunMultiThreads(WorkItemsProducer producer, WorkItemsConsumer[] consumers)
        {
            //Initialize Channels between producer and consumers
            var lastChannel = (Channel<WorkItem>)null;
            producer.WorkItemsWriter = (lastChannel = Channel.CreateUnbounded<WorkItem>()).Writer;
            foreach (var consumer in consumers)
            {
                consumer.WorkItemsReader = lastChannel.Reader;
                consumer.WorkItemsWriter = (lastChannel = Channel.CreateUnbounded<WorkItem>()).Writer;
            }

            //Producing messages and wait for end of consumming
            var consumerTasks = consumers.Select(c => c.ConsumeData()).ToArray();
            await producer.BeginProducing();
            await Task.WhenAll(consumerTasks);
        }
        
        private class WorkItemsProducer()
        {
            public ChannelWriter<WorkItem> WorkItemsWriter { get; set; }

            public IWorkItemProvider[] Providers { get; set; }

            public async Task BeginProducing()
            {
                var workItems = Program.GetAllItems(Providers);
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
                        var stepDuration = ProcessItem(workItem, numberOfProcessedItems++, Processors, ProgressGui);
                        totalDuration = totalDuration.Add(stepDuration);
                        await WorkItemsWriter.WriteAsync(workItem);
                    }
                }

                ProgressGui(numberOfProcessedItems, WorkItem.InstancesCount, $"{Math.Floor(totalDuration.TotalSeconds / 60)} min {Math.Floor(totalDuration.TotalSeconds % 60)} sec");
                WorkItemsWriter.Complete();
            }
        }

        private static IEnumerable<WorkItem> GetAllItems(IWorkItemProvider[] provider)
        {
            return provider.SelectMany(convertor => convertor.GetItems())
                           .OrderBy(item => item.OriginalFilePath);
        }
        private static TimeSpan ProcessItem(WorkItem workItem, int workItemPosition, IWorkItemProcessor[] processors, Action<int, int, string> progressGui)
        {
            var startTime = DateTime.Now;

            var workingFilePath = workItem.FilePath.Replace(Options.SourceFolder, string.Empty).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (Features.UseProgressBar)
            {
                var description = Features.UseProgressBarWithColor ? $"{{DarkBlue}}DOING:  {{Default}}{workingFilePath}" : $"DOING:  {workingFilePath}";
                progressGui(workItemPosition, WorkItem.InstancesCount, description);
            }                

            try
            {
                processors.Where(processor => processor.Accept(workItem))
                          .ForEach(processor => { if (processor.Process(workItem)) { workItem.RestoreLastWriteTime(); } });
                GC.Collect();
            }
            catch (Exception ex)
            {
                View.Error($"{Path.GetFileName(workingFilePath)}: {ex.Message}");
            }

            if (Features.UseProgressBar)
            {
                var description = Features.UseProgressBarWithColor ? $"{{DarkBlue}}DONE:   {{Default}}{workingFilePath}" : $"DONE:   {workingFilePath}";
                progressGui(workItemPosition + 1, WorkItem.InstancesCount, description);
            }                

            return DateTime.Now - startTime;
        }
    }
}
