using MangaManager.Tasks.Archive;
using MangaManager.Tasks.Checker;
using MangaManager.Tasks.Convert.Converter;
using MangaManager.Tasks.Move;
using MangaManager.Tasks.OnlineLibraryUpdater;
using MangaManager.Tasks.Rename;
using MangaManager.Tasks.Scrap;
using MangaManager.Tasks.Tag;
using System.Collections.Generic;
using System.Linq;

namespace MangaManager.Tasks
{
    public class WorkItemProcessors
    {
        public static IWorkItemProvider[] Providers => Converters
                                                        .Union(Checkers)
                                                        .Union(Renamers)
                                                        .Union(Movers)
                                                        .Union(Scappers)
                                                        .Union(Taggers)
                                                        .Union(OnlineLibraryUpdaters)
                                                        .Union(Archivers)
                                                        .Union(Checkers)
                                                        .OfType<IWorkItemProvider>()
                                                        .ToArray();

        public static IWorkItemProcessor[] Converters = new IWorkItemProcessor[]
        {
            new ArchiveConverter(),
            new PdfConverter(),
            new EpubConverter(),
            new AmazonConverter(),
            new FolderConverter(),
        };

        public static IWorkItemProcessor[] Renamers = new IWorkItemProcessor[]
        {
            new FromFileNameRenamer(),
        };

        public static IWorkItemProcessor[] Movers = new IWorkItemProcessor[]
        {
            new ToRootFolderMover(),
        };

        public static IWorkItemProcessor[] Scappers = new IWorkItemProcessor[]
        {
            new MangaCollecScrapper(),
        };

        public static IWorkItemProcessor[] Taggers = new IWorkItemProcessor[]
        {
            new AddComicInfoTagger(),
        };

        public static IWorkItemProcessor[] OnlineLibraryUpdaters = new IWorkItemProcessor[]
        {
            new MangaCollecLibraryUpdater(),
        };

        public static IWorkItemProcessor[] Archivers = new IWorkItemProcessor[]
        {
            new ToLibraryFolderMover()
        };

        public static IWorkItemProcessor[] Checkers = new IWorkItemProcessor[]
        {
            new MangaCollecLibraryChecker()
        };
    }

    public interface IWorkItemProvider
    {
        public IEnumerable<WorkItem> GetItems();
    }

    public interface IWorkItemProcessor
    {
        public bool Accept(WorkItem workItem);
        public void Process(WorkItem workItem);
    }
}