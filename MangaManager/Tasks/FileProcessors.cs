using MangaManager.Tasks.Archive;
using MangaManager.Tasks.Convert.Converter;
using MangaManager.Tasks.Move;
using MangaManager.Tasks.Rename;
using MangaManager.Tasks.Tag;

namespace MangaManager.Tasks
{
    public class FileProcessors
    {
        public static IFileProcessor[] Converters =
        [
            new ArchiveConverter(),
            new PdfConverter(),
            new EpubConverter(),
            new AmazonConverter(),
            new FolderConverter(),
        ];

        public static IFileProcessor[] Renamers =
        [
            new FromFileNameRenamer(),
        ];

        public static IFileProcessor[] Movers =
        [
            new ToRootFolderMover(),
        ];

        public static IFileProcessor[] Taggers =
        [
            new AddComicInfoTagger(),
        ];

        public static IFileProcessor[] Archivers =
        [
            new ToLibraryFolderMover()
        ];
    }

    public interface IFileProvider
    {
        public string[] GetFiles();
    }

    public interface IFileProcessor
    {
        public bool Accept(string file);
        public bool ProcessFile(string file, out string newFile);
    }
}