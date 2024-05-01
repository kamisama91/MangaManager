using System;
using System.Threading;

namespace MangaManager.View
{
    public class ViewController
    {
        private long RefreshProgressBarTotalMilliseconds;
        public decimal RefreshProgressBarTotalSeconds => RefreshProgressBarTotalMilliseconds / 1000;

        private MainView _view;
        public EventHandler ViewLoaded;

        public void Show()
        {
            _view = new MainView();
            _view.Loaded += () => ViewLoaded?.Invoke(this, EventArgs.Empty);
            _view.Show();
        }

        public void ConversionProgress(int current, int total, string description)
        {
            RefreshProgressBar(_view?._convertProgressBar, current, total, description);
        }
        public void RenamingProgress(int current, int total, string description)
        {
            RefreshProgressBar(_view?._renameProgressBar, current, total, description);
        }
        public void MovingProgress(int current, int total, string description)
        {
            RefreshProgressBar(_view?._moveProgressBar, current, total, description);
        }
        public void ScrappingProgress(int current, int total, string description)
        {
            RefreshProgressBar(_view?._scrapProgressBar, current, total, description);
        }
        public void TaggingProgress(int current, int total, string description)
        {
            RefreshProgressBar(_view?._tagProgressBar, current, total, description);
        }
        public void OnlineUpdatingProgress(int current, int total, string description)
        {
            RefreshProgressBar(_view?._onlineUpdateProgressBar, current, total, description);
        }
        public void ArchivingingProgress(int current, int total, string description)
        {
            RefreshProgressBar(_view?._archiveProgressBar, current, total, description);
        }
        private void RefreshProgressBar(ProgressBarWithDescriptionView progressBar, int current, int max, string description)
        {
            if (progressBar != null)
            {
                var startTime = DateTime.Now;
                var UpdateProgressBarTimers = () =>
                {
                    var stepTotalMilliseconds = (long)(DateTime.Now - startTime).TotalMilliseconds;
                    Interlocked.Add(ref RefreshProgressBarTotalMilliseconds, stepTotalMilliseconds);
                };

                lock (progressBar)
                {
                    var DoRefresh = () =>
                    {
                        if (!Features.UseProgressBarWithColor) { description = System.Text.RegularExpressions.Regex.Replace(description, @"\{[^\}]*\}", ""); }
                        progressBar.Refresh(current, max, description);
                        UpdateProgressBarTimers();
                    };

                    max = Math.Max(1, max); //avoid 0 division
                    current = Math.Max(0, Math.Min(current, max));

                    if (progressBar.Max != max)
                    {
                        //Always refresh when Max vakus change
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

                    if (current == 0 || current == progressBar.Max)
                    {
                        //When step Features is on: still referesh when first/last item
                        DoRefresh();
                        return;
                    }

                    var refreshPercent = Convert.ToInt32(Features.ProgressBarPercentStep * Math.Floor(Math.Floor(100m * current / progressBar.Max) / Features.ProgressBarPercentStep));
                    var currentPercent = Convert.ToInt32(Features.ProgressBarPercentStep * Math.Floor(Math.Floor(100m * progressBar.Current / progressBar.Max) / Features.ProgressBarPercentStep));
                    if (current == progressBar.Current || currentPercent < refreshPercent)
                    {
                        //When step Features is on: when steps are raised, progress to latest
                        var maxBackup = max;
                        var currentBackup = current;
                        max = 100;
                        current = refreshPercent;
                        DoRefresh();
                        progressBar.Max = maxBackup;
                        progressBar.Current = currentBackup;
                        return;
                    }
                }

                //Here Nohing has been done in refreshed on screen but a lock has been waited
                UpdateProgressBarTimers();
            }
        }

        public string AskUserInput(string message)
        {
            if (_view?._userInputView != null)
            {
                lock (_view._userInputView)
                {
                    return _view._userInputView.AskUserInput(message);
                }
            }
            return null;
        }

        public void Info(string message)
        {
            Log(message, ConsoleColor.DarkBlue);
        }
        public void Warning(string message)
        {
            Log(message, ConsoleColor.DarkYellow);
        }
        public void Error(string message)
        {
            Log(message, ConsoleColor.DarkRed);
        }
        private void Log(string message, ConsoleColor color)
        {
            if (_view?._logTextView != null)
            {
                _view._logTextView.AddLine($"{{{color}}}{message}{{Default}}");
            }
            else
            {
                var backup = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ForegroundColor = backup;
            }
        }
    }
}
