﻿namespace MangaManager.Tasks.Scrap
{
    public class MangaCollecScrapper : IWorkItemProcessor
    {
        public bool Accept(WorkItem workItem)
        {
            if (!Program.Options.Scrap)
            {
                return false;
            }

            return true;
        }

        public bool Process(WorkItem workItem)
        {
            return false;
        }
    }
}