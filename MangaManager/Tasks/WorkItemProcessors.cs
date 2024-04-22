using MangaManager.Tasks.Archive;
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
        public static IWorkItemProvider[] Providers => Converters.OfType<IWorkItemProvider>().ToArray();

        public static IWorkItemProcessor[] Converters =
        [
            new ArchiveConverter(),
            new PdfConverter(),
            new EpubConverter(),
            new AmazonConverter(),
            new FolderConverter(),
        ];

        public static IWorkItemProcessor[] Renamers =
        [
            new FromFileNameRenamer(),
        ];

        public static IWorkItemProcessor[] Movers =
        [
            new ToRootFolderMover(),
        ];

        public static IWorkItemProcessor[] Scappers =
        [
            new MangaCollecScrapper(),
        ];

        public static IWorkItemProcessor[] Taggers =
        [
            new AddComicInfoTagger(),
        ];

        public static IWorkItemProcessor[] OnlineLibraryUpdaters =
        [
            new MangaCollecLibraryUpdater(),
        ];

        public static IWorkItemProcessor[] Archivers =
        [
            new ToLibraryFolderMover()
        ];
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