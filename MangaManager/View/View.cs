using Konsole;
using System;
using System.Threading;

namespace MangaManager.View
{
    public class View
    {
        private long RefreshProgressBarTotalMilliseconds;
        public decimal RefreshProgressBarTotalSeconds => (decimal)(RefreshProgressBarTotalMilliseconds / 1000);

        private IConsole _logs;
        private IConsole _forms;
        private IWithCurrentProgressBar _convertProgressBar;
        private IWithCurrentProgressBar _renameProgressBar;
        private IWithCurrentProgressBar _moveProgressBar;
        private IWithCurrentProgressBar _scrapProgressBar;
        private IWithCurrentProgressBar _tagProgressBar;
        private IWithCurrentProgressBar _onlineUpdateProgressBar;
        private IWithCurrentProgressBar _archiveProgressBar;

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

        private IWithCurrentProgressBar BuildProgressBar(IConsole console, string headerFormat, bool isNeeded)
        {
            if (!isNeeded)
                return default;

            var progressBar = !Features.UseProgressBarWithColor
                ? new CustomHeaderProgressBar(console, 1)
                : new CustomHeaderAndColoredDesciptionProgressBar(console, 1);
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
        
        private void RefreshProgressBar(IWithCurrentProgressBar progressBar, int current, int max, string description)
        {
            
            if (progressBar != null)
            {
                var startTime = DateTime.Now;
                var UpdateProgressBarTimers = () =>
                {
                    var stepTotalMilliseconds = (long)((DateTime.Now - startTime).TotalMilliseconds);
                    Interlocked.Add(ref RefreshProgressBarTotalMilliseconds, stepTotalMilliseconds);
                };

                lock (progressBar)
                {
                    var DoRefresh = () =>
                    {
                        if (!Features.UseProgressBarWithColor) { description = System.Text.RegularExpressions.Regex.Replace(description, @"\{[^\}]*\}", ""); }
                        progressBar.Refresh(current, description);
                        UpdateProgressBarTimers();
                    };

                    max = Math.Max(1, max); //avoid 0 division
                    current = Math.Max(0, Math.Min(current, max));

                    if (progressBar.Max != max)
                    {
                        //Always refresh when Max vakus change
                        progressBar.ForceMaxWithNoRefresh(max);
                        DoRefresh();
                        return;
                    }

                    if (current < progressBar.Current)
                    {
                        //When Max is same, do not update
                        return;
                    }

                    if (Features.ProgressBarPercentStep == 0)
                    {
                        //Refresh when step Features is Off
                        DoRefresh();
                        return;
                    }

                    if ((current == 0) || (current == progressBar.Max))
                    {
                        //When step Features is on: still referesh when first/last item
                        DoRefresh();
                        return;
                    }

                    var refreshPercent = Convert.ToInt32((decimal)Features.ProgressBarPercentStep * Math.Floor(Math.Floor(100m * (decimal)current / (decimal)progressBar.Max) / (decimal)Features.ProgressBarPercentStep));
                    var currentPercent = Convert.ToInt32((decimal)Features.ProgressBarPercentStep * Math.Floor(Math.Floor(100m * (decimal)progressBar.Current / (decimal)progressBar.Max) / (decimal)Features.ProgressBarPercentStep));
                    if ((current == progressBar.Current) || (currentPercent < refreshPercent))
                    {
                        //When step Features is on: when steps are raised, progress to latest
                        var currentBackup = current;
                        progressBar.ForceMaxWithNoRefresh(100);
                        current = refreshPercent;
                        DoRefresh();
                        progressBar.ForceMaxWithNoRefresh(max);
                        progressBar.ForceCurrentWithNoRefresh(currentBackup);
                        return;
                    }
                }

                //Here Nohing has been done in refreshed on screen but a lock has been waited
                UpdateProgressBarTimers();
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
