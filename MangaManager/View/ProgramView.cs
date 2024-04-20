using Konsole;
using System;

namespace MangaManager.View
{
    public class ProgramView
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

        public ProgramView() 
        {
            var windows = Window.OpenBox("Manga Manager v1.0", 120, 35);
            var tasks = windows.SplitTop("Tasks");
            var bottom = windows.SplitBottom();
            _logs = bottom.SplitRight("Logs");
            _forms = bottom.SplitLeft("User Inputs");
            _convertProgressBar = Program.Options.Convert ? new CustomHeaderProgressBar(tasks, 100).WithLine1HeaderFormat("Convert ".PadRight(15)) : default;
            _renameProgressBar = Program.Options.Rename ? new CustomHeaderProgressBar(tasks, 100).WithLine1HeaderFormat("Rename ".PadRight(15)) : default;
            _moveProgressBar = Program.Options.Move ? new CustomHeaderProgressBar(tasks, 100).WithLine1HeaderFormat("Move ".PadRight(15)) : default;
            _scrapProgressBar = Program.Options.Scrap ? new CustomHeaderProgressBar(tasks, 100).WithLine1HeaderFormat("Scrap ".PadRight(15)) : default;
            _tagProgressBar = Program.Options.Tag ? new CustomHeaderProgressBar(tasks, 100).WithLine1HeaderFormat("Tag ".PadRight(15)) : default;
            _onlineUpdateProgressBar = Program.Options.OnlineUpdate ? new CustomHeaderProgressBar(tasks, 100).WithLine1HeaderFormat("Online upd. ".PadRight(15)) : default;
            _archiveProgressBar = Program.Options.Archive ? new CustomHeaderProgressBar(tasks, 100).WithLine1HeaderFormat("Archive ".PadRight(15)) : default;
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
