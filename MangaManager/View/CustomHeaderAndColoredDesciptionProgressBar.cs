using Konsole;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MangaManager.View
{
    public class CustomHeaderAndColoredDesciptionProgressBar : CustomHeaderProgressBar
    {
        private readonly Dictionary<string, ConsoleColor> _colorTextMapping;

        public CustomHeaderAndColoredDesciptionProgressBar(IConsole console, int max, char character = '#')
            : base(console, max, character)
        {
            _colorTextMapping = Enum.GetNames<ConsoleColor>().ToDictionary(name => $"{{{name}}}", Enum.Parse<ConsoleColor>);
            _colorTextMapping["{Default}"] = _c;
        }

        protected override string RefreshLine2(string item)
        {
            var line2Text = string.Format(FORMAT_LINE2, item ?? string.Empty).PadRight(_console.WindowWidth).Substring(0, _console.WindowWidth);
            var line2EvaluatedText = string.Empty;

            _console.ForegroundColor = _c;
            _console.WriteLine(string.Empty);
            var searchForColors = true;
            do
            {
                var colorStartPositions = (_colorTextMapping ?? new Dictionary<string, ConsoleColor>())
                    .Select(kv => new { ForegroundColor = kv.Value, Position = line2Text.IndexOf(kv.Key), TextToken = kv.Key })
                    .Where(p => p.Position >= 0)
                    .OrderBy(p => p.Position)
                    .Take(2)
                    .ToArray();

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

            return line2EvaluatedText;
        }
    }
}
