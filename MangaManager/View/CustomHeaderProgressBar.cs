using Konsole;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MangaManager.View
{
    public class CustomHeaderProgressBar : IProgressBar
    {
        private static object _locker = new object();

        private int _max;
        private readonly char _character;
        private readonly IConsole _console;
        private readonly int _y;
        private readonly ConsoleColor _c;

        private readonly Dictionary<string, ConsoleColor> _colorTextMapping;

        private string FORMAT_LINE1_HEADER = "Item {0,-5} of {1,-5}. ";
        private string FORMAT_LINE1_PERCENT = "({0,-3}%) ";
        private string FORMAT_LINE2 = "    {0}";

        private int _current = 0;
        private string _item = string.Empty;

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
                Refresh(_current, _item);
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

                _colorTextMapping = Enum.GetNames<ConsoleColor>().ToDictionary(name => $"{{{name}}}", Enum.Parse<ConsoleColor>);
                _colorTextMapping["{Default}"] = _c;

                _console.WriteLine("");
                _console.WriteLine("");
                Refresh(0, "");
            }
        }

        public CustomHeaderProgressBar WithLine1HeaderFormat(string format)
        {
            FORMAT_LINE1_HEADER = format;
            Refresh(_current, _item);
            return this;
        }
        public CustomHeaderProgressBar WithLine1PercentFormat(string format)
        {
            FORMAT_LINE1_PERCENT = format;
            Refresh(_current, _item);
            return this;
        }
        public CustomHeaderProgressBar WithLine2Format(string format)
        {
            FORMAT_LINE2 = format;
            Refresh(_current, _item);
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
                _current = current;
                _item = item;

                var state = _console.State;
                try
                {
                    _console.CursorTop = Y;
                    _console.CursorLeft = 0;

                    var currentProgress = Max > 0 ? current / (decimal)_max : 0m;
                    var headerText = string.Format(FORMAT_LINE1_HEADER, current, _max);
                    var percentText = string.Format(FORMAT_LINE1_PERCENT, (int)(100 * currentProgress));
                    var line1ProgressWidth = _console.WindowWidth - (headerText.Length + percentText.Length);
                    var line1Progress = new string(_character, (int)(line1ProgressWidth * currentProgress)).PadRight(line1ProgressWidth);
                    _console.ForegroundColor = _c;
                    _console.Write(headerText + percentText);
                    _console.ForegroundColor = ConsoleColor.Green;
                    _console.Write(line1Progress);

                    var line2Text = string.Format(FORMAT_LINE2, item ?? string.Empty).PadRight(_console.WindowWidth).Substring(0, _console.WindowWidth);
                    var line2EvaluatedText = string.Empty;              
                    _console.ForegroundColor = _c;
                    _console.WriteLine(string.Empty);
                    var searchForColors = true;
                    do
                    {
                        var colorStartPositions = _colorTextMapping.Select(kv => new { ForegroundColor = kv.Value, Position = line2Text.IndexOf(kv.Key), TextToken = kv.Key }).Where(p => p.Position >= 0).OrderBy(p => p.Position).Take(2).ToArray();
                        if (colorStartPositions.Length > 0)
                        {
                            if (colorStartPositions[0].Position > 0)
                            {
                                var beforeFirstColorText = line2Text.Substring(0, colorStartPositions[0].Position);
                                _console.ForegroundColor = _c;
                                _console.Write(beforeFirstColorText);
                                line2EvaluatedText += beforeFirstColorText;
                            }
                            if (colorStartPositions.Length == 1)
                            {
                                var coloredText = line2Text.Substring(colorStartPositions[0].Position, line2Text.Length - colorStartPositions[0].Position).Replace(colorStartPositions[0].TextToken, string.Empty);
                                _console.ForegroundColor = colorStartPositions[0].ForegroundColor;
                                _console.Write(coloredText);
                                line2EvaluatedText += coloredText;
                                searchForColors = false;
                            }
                            else
                            {
                                var coloredText = line2Text.Substring(colorStartPositions[0].Position, colorStartPositions[1].Position - colorStartPositions[0].Position).Replace(colorStartPositions[0].TextToken, string.Empty);
                                _console.ForegroundColor = colorStartPositions[0].ForegroundColor;
                                _console.Write(coloredText);
                                line2EvaluatedText += coloredText;
                                line2Text = line2Text.Substring(colorStartPositions[1].Position, line2Text.Length - colorStartPositions[1].Position);
                            }
                        }
                        else
                        {
                            _console.ForegroundColor = _c;
                            _console.Write(line2Text);
                            line2EvaluatedText += line2Text;
                            searchForColors = false;
                        }
                    } while (searchForColors);
                    _console.ForegroundColor = _c;

                    Line1 = headerText + percentText + line1Progress;
                    Line2 = line2EvaluatedText;
                }
                finally
                {
                    _console.State = state;
                }
            }
        }
        public void Next(string item)
        {
            _current++;
            Refresh(_current, item);
        }
    }
}
