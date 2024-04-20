using Konsole;

namespace MangaManager.View
{
    public class View
    {
        private IConsole _logs;
        private IConsole _forms;
        private IProgressBar _convertProgressBar;
        private IProgressBar _renameProgressBar;
        private IProgressBar _moveProgressBar;
        private IProgressBar _scrapProgressBar;
        private IProgressBar _tagProgressBar;
        private IProgressBar _onlineUpdateProgressBar;
        private IProgressBar _archiveProgressBar;

        public View() 
        {
            var windows = Window.OpenBox("Manga Manager v1.0", 120, 35);
            var tasks = windows.SplitTop("Tasks");
            var bottom = windows.SplitBottom();
            _logs = bottom.SplitRight("Logs");
            _forms = bottom.SplitLeft("User Inputs");
            _convertProgressBar = BuildProgressBar(tasks, "Convert ", Program.Options.Convert);
            _renameProgressBar = BuildProgressBar(tasks, "Rename ", Program.Options.Rename);
            _moveProgressBar = BuildProgressBar(tasks, "Move ", Program.Options.Move);
            _scrapProgressBar = BuildProgressBar(tasks, "Scrap ", Program.Options.Scrap);
            _tagProgressBar = BuildProgressBar(tasks, "Tag ", Program.Options.Tag);
            _onlineUpdateProgressBar = BuildProgressBar(tasks, "Online upd. ", Program.Options.OnlineUpdate);
            _archiveProgressBar = BuildProgressBar(tasks, "Archive ", Program.Options.Archive);
        }

        private IProgressBar BuildProgressBar(IConsole console, string headerFormat, bool isNeeded)
        {
            if (!isNeeded)
                return default;

            var progressBar = !Features.UseProgressBarWithColor
                ? new CustomHeaderProgressBar(console, 100)
                : new CustomHeaderAndColoredDesciptionProgressBar(console, 100);
            progressBar.WithLine1HeaderFormat(headerFormat.PadRight(15));
            return progressBar;
        }

        public void ConversionProgress(int current, int total, string description)
        {
            RefreshProgressBar(_convertProgressBar, current, total, description);
        }
        public void RenamingProgress(int current, int total, string description)
        {
            RefreshProgressBar(_renameProgressBar, current, total, description);
        }
        public void MovingProgress(int current, int total, string description)
        {
            RefreshProgressBar(_moveProgressBar, current, total, description);
        }
        public void ScrappingProgress(int current, int total, string description)
        {
            RefreshProgressBar(_scrapProgressBar, current, total, description);
        }
        public void TaggingProgress(int current, int total, string description)
        {
            RefreshProgressBar(_tagProgressBar, current, total, description);
        }
        public void OnlineUpdatingProgress(int current, int total, string description)
        {
            RefreshProgressBar(_onlineUpdateProgressBar, current, total, description);
        }
        public void ArchivingingProgress(int current, int total, string description)
        {
            RefreshProgressBar(_archiveProgressBar, current, total, description);
        }
        
        private void RefreshProgressBar(IProgressBar progressBar, int current, int total, string description)
        {
            if (progressBar != null)
            {
                var percent = 100m * (total == 0 ? 1m : current / (decimal)total);
                progressBar.Refresh((int)percent, description);
            }
        }

        public void Info(string message)
        {
            _logs.ForegroundColor = System.ConsoleColor.DarkBlue;
            _logs.WriteLine(message);
        }
        public void Warning(string message)
        {
            _logs.ForegroundColor = System.ConsoleColor.DarkYellow;
            _logs.WriteLine(message);
        }
        public void Error(string message)
        {
            _logs.ForegroundColor = System.ConsoleColor.DarkRed;
            _logs.WriteLine(message);
        }
    }
}
