namespace MangaManager.Tasks.OnlineLibraryUpdater
{
    public class MangaCollecLibraryUpdater : IFileProcessor
    {
        public bool Accept(string file)
        {
            return false;
        }

        public bool ProcessFile(string file, out string newFile)
        {
            newFile = file;
            return false;
        }
    }
}