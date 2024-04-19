namespace MangaManager.Tasks.Scrap
{
    public class MangaCollecScrapper : IFileProcessor
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