using CommandLine;
using MangaManager.Tasks;
using MangaManager.View;
using SharpCompress;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
                    opts => Environment.ExitCode = RunWithView(opts).Result,
                    errs => Environment.ExitCode = -2);
        }

        public static Options Options { get; private set; }
        public static ViewController View { get; private set; }

        private static async Task<int> RunWithView(Options options)
        {
            Options = options;
            View = new ViewController();

            var exitCode = 0;            
            View.ViewLoaded += async (s, e) => { exitCode = await Run(options); };
            View.Show();
            return await Task.FromResult(exitCode);
        }

        private static async Task<int> Run(Options options)
        {
            Options ??= options;
            View ??= new ViewController();

            try
            {
                //Create Workers producer
                var producer = new WorkItemsProducer { Providers = WorkItemProcessors.Providers };

                //Create Workers consumers
                var consumers = new WorkItemsConsumer[]
                {
                    new() { Processors = WorkItemProcessors.Converters, ProgressGui = View.ConversionProgress, DegreeOfParallelism = 3 },
                    new() { Processors = WorkItemProcessors.Renamers, ProgressGui = View.RenamingProgress },
                    new() { Processors = WorkItemProcessors.Movers, ProgressGui = View.MovingProgress },
                    new() { Processors = WorkItemProcessors.Scappers, ProgressGui = View.ScrappingProgress },
                    new() { Processors = WorkItemProcessors.Taggers, ProgressGui = View.TaggingProgress, DegreeOfParallelism = 2 },
                    new() { Processors = WorkItemProcessors.OnlineLibraryUpdaters, ProgressGui = View.OnlineUpdatingProgress },
                    new() { Processors = WorkItemProcessors.Archivers, ProgressGui = View.ArchivingingProgress },
                };

                var startTime = DateTime.Now;

                if (Features.RunMultiThreads)
                    await RunMultiThreads(producer, consumers);
                else
                    RunSingleThread(producer, consumers);

                var duration = DateTime.Now - startTime;
                View.Info($"Finished:");
                View.Info($"   Total Time:  {Math.Floor(duration.TotalSeconds / 60)} min {Math.Floor(duration.TotalSeconds % 60)} sec");
                View.Info($"   View  Time:  {Math.Floor(View.RefreshProgressBarTotalSeconds / 60)} min {Math.Floor(View.RefreshProgressBarTotalSeconds % 60)} sec");
                View.Info($"   Items: {CacheWorkItems.InstancesCount}");
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
            var workItems = producer.ProduceData();
            consumers.ForEach(c => c.ConsumeData(workItems));
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
            consumers.Last().WorkItemsWriter = null;

            //Producing messages and wait for end of consumming
            await producer.ProduceDataAsync();
            await Task.WhenAll(consumers.Select(c => c.ConsumeDataAsync()));
        }

        private class WorkItemsProducer()
        {
            public IWorkItemProvider[] Providers { get; set; }

            public IEnumerable<WorkItem> ProduceData()
            {
                return GetAllWorkItems();
            }

            public ChannelWriter<WorkItem> WorkItemsWriter { get; set; }
            public async Task ProduceDataAsync()
            {
                foreach (var workItem in GetAllWorkItems())
                {
                    await WorkItemsWriter.WriteAsync(workItem);
                }
                WorkItemsWriter.Complete();
            }

            private WorkItem[] GetAllWorkItems()
            {
                return Providers.SelectMany(provider => provider.GetItems())
                                .ToArray();
            }
        }
        private class WorkItemsConsumer()
        {
            private long TotalMilliseconds;
            private decimal TotalSeconds => (decimal)(TotalMilliseconds / 1000);

            public IWorkItemProcessor[] Processors { get; set; }
            public Action<int, int, string> ProgressGui { get; set; }

            public void ConsumeData(IEnumerable<WorkItem> workItems)
            {
                workItems.ForEach(workItem => ProcessWorkItem(workItem));
                ProgressGui(CacheWorkItems.InstancesCount, CacheWorkItems.InstancesCount, $"{Math.Floor(TotalSeconds / 60)} min {Math.Floor(TotalSeconds % 60)} sec");
            }

            public ChannelReader<WorkItem> WorkItemsReader { get; set; }
            public ChannelWriter<WorkItem> WorkItemsWriter { get; set; }
            public int DegreeOfParallelism { get; set; } = 1;
            public async Task ConsumeDataAsync()
            {
                var parallelTasks = new List<Task>();
                for (int i = 0; i < DegreeOfParallelism; i++)
                {
                    parallelTasks.Add(
                        Task.Run(async () =>
                        {
                            while (await WorkItemsReader.WaitToReadAsync())
                            {
                                if (WorkItemsReader.TryRead(out var workItem))
                                {
                                    ProcessWorkItem(workItem);
                                    if (WorkItemsWriter != null) await WorkItemsWriter.WriteAsync(workItem);
                                }
                            }
                        })
                    );
                }

                await Task.WhenAll(parallelTasks);
                if (WorkItemsWriter != null) WorkItemsWriter.Complete();

                ProgressGui(CacheWorkItems.InstancesCount, CacheWorkItems.InstancesCount, $"{Math.Floor(TotalSeconds / 60)} min {Math.Floor(TotalSeconds % 60)} sec");
            }

            private void ProcessWorkItem(WorkItem workItem)
            {
                var workingFilePath = workItem.FilePath.Replace(Options.SourceFolder, string.Empty).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                try
                {
                    ProgressGui(workItem.InstanceId, CacheWorkItems.InstancesCount, $"{{Blue}}DOING:  {{Default}}{workingFilePath}");

                    var startTime = DateTime.Now;
                    Processors.Where(processor => processor.Accept(workItem))
                              .ForEach(processor => processor.Process(workItem));
                    GC.Collect();
                    Interlocked.Add(ref TotalMilliseconds, (long)((DateTime.Now - startTime).TotalMilliseconds));

                    ProgressGui(workItem.InstanceId, CacheWorkItems.InstancesCount, $"{{Green}}DONE:   {{Default}}{workingFilePath}");
                }
                catch (Exception ex)
                {
                    ProgressGui(workItem.InstanceId, CacheWorkItems.InstancesCount, $"{{DarkRed}}FAIL:   {{Default}}{workingFilePath}");
                    View.Error($"{Path.GetFileName(workingFilePath)}: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}");
                }
            }
        }
    }
}
