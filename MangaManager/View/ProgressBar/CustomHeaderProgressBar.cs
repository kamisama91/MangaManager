using Konsole;
using System;

namespace MangaManager.View
{
    public class CustomHeaderProgressBar : IWithCurrentProgressBar
    {
        private static object _locker = new object();

        private int _max;
        private readonly char _character;
        protected readonly IConsole _console;
        protected readonly ConsoleColor _c;
        private readonly int _y;

        private string FORMAT_LINE1_HEADER = "Item {0,-5} of {1,-5}. ";
        private string FORMAT_LINE1_PERCENT = "({0,-3}%) ";
        protected string FORMAT_LINE2 = "    {0}";

        public int Current { get; private set; } = 0;
        public string Item { get; private set; } = string.Empty;

        public int Y => _y;
        public string Line1 { get; private set; }
        public string Line2 { get; private set; }
        public int Max
        {
            get
            {
                return _max;
            }
            set
            {
                _max = value;
                Refresh(Current, Item);
            }
        }

        public CustomHeaderProgressBar(IConsole console, int max, char character = '#')
        {
            lock (_locker)
            {
                _console = console;
                _y = _console.CursorTop;
                _c = _console.ForegroundColor;
                _max = max;
                _character = character;
                _console.WriteLine("");
                _console.WriteLine("");
                Refresh(0, "");
            }
        }

        public CustomHeaderProgressBar WithLine1HeaderFormat(string format)
        {
            FORMAT_LINE1_HEADER = format;
            Refresh(Current, Item);
            return this;
        }
        public CustomHeaderProgressBar WithLine1PercentFormat(string format)
        {
            FORMAT_LINE1_PERCENT = format;
            Refresh(Current, Item);
            return this;
        }
        public CustomHeaderProgressBar WithLine2Format(string format)
        {
            FORMAT_LINE2 = format;
            Refresh(Current, Item);
            return this;
        }

        public void Refresh(int current, string format, params object[] args)
        {
            string item = string.Format(format, args);
            Refresh(current, item);
        }
        public void Refresh(int current, string item)
        {
            lock (_locker)
            {
                Current = current;
                Item = item;

                var state = _console.State;
                try
                {
                    _console.CursorTop = Y;
                    _console.CursorLeft = 0;
                    Line1 = RefreshLine1(current);
                    Line2 = RefreshLine2(item);
                }
                finally
                {
                    _console.State = state;
                }
            }
        }

        private string RefreshLine1(int current)
        {
            var currentProgress = Max > 0 ? current / (decimal)_max : 0m;
            var headerText = string.Format(FORMAT_LINE1_HEADER, current, _max);
            var percentText = string.Format(FORMAT_LINE1_PERCENT, (int)(100 * currentProgress));
            var line1ProgressWidth = _console.WindowWidth - (headerText.Length + percentText.Length);
            var line1Progress = new string(_character, (int)(line1ProgressWidth * currentProgress)).PadRight(line1ProgressWidth);
            _console.ForegroundColor = _c;
            _console.Write(headerText + percentText);
            _console.ForegroundColor = ConsoleColor.Green;
            _console.Write(line1Progress);
            return headerText + percentText + line1Progress;
        }
        protected virtual string RefreshLine2(string item)
        {
            var line2Text = string.Format(FORMAT_LINE2, item ?? string.Empty).PadRight(_console.WindowWidth).Substring(0, _console.WindowWidth);
            _console.ForegroundColor = _c;
            _console.WriteLine(line2Text);
            return line2Text;
        }

        public void Next(string item)
        {
            Current++;
            Refresh(Current, item);
        }

        public void ForceCurrentWithNoRefresh(int current)
        {
            Current = current;
        }
        public void ForceMaxWithNoRefresh(int max)
        {
            _max = max;
        }
    }
}
