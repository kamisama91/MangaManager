using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace MangaManager.View
{
    public class MultiColorTextView : TextView
    {
        private static Dictionary<string, Terminal.Gui.Attribute> s_colorAttributes;
        static MultiColorTextView()
        {
            s_colorAttributes = Enum.GetValues<Color>().ToDictionary(c => c.ToString(), c => new Terminal.Gui.Attribute(c, Application.Top.ColorScheme.Normal.Background));
            s_colorAttributes[ConsoleColor.DarkBlue.ToString()] = s_colorAttributes[Color.Blue.ToString()];
            s_colorAttributes[ConsoleColor.DarkGreen.ToString()] = s_colorAttributes[Color.Green.ToString()];
            s_colorAttributes[ConsoleColor.DarkCyan.ToString()] = s_colorAttributes[Color.Cyan.ToString()];
            s_colorAttributes[ConsoleColor.DarkRed.ToString()] = s_colorAttributes[Color.Red.ToString()];
            s_colorAttributes[ConsoleColor.DarkMagenta.ToString()] = s_colorAttributes[Color.Brown.ToString()];
            s_colorAttributes[ConsoleColor.DarkYellow.ToString()] = s_colorAttributes[Color.Magenta.ToString()];
            s_colorAttributes[ConsoleColor.Blue.ToString()] = s_colorAttributes[Color.BrightBlue.ToString()];
            s_colorAttributes[ConsoleColor.Green.ToString()] = s_colorAttributes[Color.BrightGreen.ToString()];
            s_colorAttributes[ConsoleColor.Cyan.ToString()] = s_colorAttributes[Color.BrightCyan.ToString()];
            s_colorAttributes[ConsoleColor.Red.ToString()] = s_colorAttributes[Color.BrightRed.ToString()];
            s_colorAttributes[ConsoleColor.Magenta.ToString()] = s_colorAttributes[Color.BrightMagenta.ToString()];
            s_colorAttributes[ConsoleColor.Yellow.ToString()] = s_colorAttributes[Color.BrightYellow.ToString()];
            s_colorAttributes["Default"] = s_colorAttributes[Color.White.ToString()];
        }

        private ustring _text = string.Empty;

        public override ustring Text
        {
            get => _text;
            set
            {
                _text = value;
                var uncoloredText = BuildTextWithColor(_text.ToString(), out var colorIndexes);
                Data = colorIndexes;
                base.Text = uncoloredText;
            }
        }
        public ustring UncoloredText => base.Text;

        public MultiColorTextView()
        {
            ReadOnly = true; 
            DesiredCursorVisibility = CursorVisibility.Invisible;            
        }

        protected override void SetNormalColor()
        {
            if (s_colorAttributes != null)
                Driver.SetAttribute(s_colorAttributes["Default"]);
        }
        protected override void SetNormalColor(List<Rune> line, int idx)
        {
            if (s_colorAttributes != null && Data is Dictionary<int, string> colorIndexe)
            {
                var lineText = new string(line.Select(rune => (char)rune.Value).ToArray());
                var linePosition = UncoloredText.IndexOf(lineText);
                var runePosition = linePosition + idx;
                Driver.SetAttribute(s_colorAttributes[colorIndexe[colorIndexe.Keys.Where(i => i <= runePosition).Max()]]);
            }
        }
        protected override void SetReadOnlyColor(List<Rune> line, int idx)
        {
            SetNormalColor(line, idx);
        }

        private static string BuildTextWithColor(string coloredText, out Dictionary<int, string> colorIndexes)
        {
            colorIndexes = new Dictionary<int, string>();
            var finalText = new System.Text.StringBuilder();
            var searchForColors = true;
            do
            {
                var colorStartPositions = (s_colorAttributes ?? new Dictionary<string, Terminal.Gui.Attribute>())
                    .Select(kv => new { ForegroundColor = kv.Key.ToString(), Position = coloredText.IndexOf($"{{{kv.Key}}}"), TextToken = $"{{{kv.Key}}}" })
                    .Where(p => p.Position >= 0)
                    .OrderBy(p => p.Position)
                    .Take(2)
                    .ToArray();

                if (colorStartPositions.Length > 0)
                {
                    if (colorStartPositions[0].Position > 0)
                    {
                        colorIndexes[0] = "Default";
                        finalText.Append(coloredText.Substring(0, colorStartPositions[0].Position));
                    }
                    if (colorStartPositions.Length == 1)
                    {
                        var partialColoredText = coloredText.Substring(colorStartPositions[0].Position, coloredText.Length - colorStartPositions[0].Position).Replace(colorStartPositions[0].TextToken, string.Empty);
                        colorIndexes[finalText.Length] = colorStartPositions[0].ForegroundColor;
                        finalText.Append(partialColoredText);
                        searchForColors = false;
                    }
                    else
                    {
                        var partialColoredText = coloredText.Substring(colorStartPositions[0].Position, colorStartPositions[1].Position - colorStartPositions[0].Position).Replace(colorStartPositions[0].TextToken, string.Empty);
                        colorIndexes[finalText.Length] = colorStartPositions[0].ForegroundColor;
                        finalText.Append(partialColoredText);
                        coloredText = coloredText.Substring(colorStartPositions[1].Position, coloredText.Length - colorStartPositions[1].Position);
                    }
                }
                else
                {
                    colorIndexes[0] = "Default";
                    finalText.Append(coloredText);
                    searchForColors = false;
                }
            } while (searchForColors);
            return finalText.ToString();
        }
    }
}
