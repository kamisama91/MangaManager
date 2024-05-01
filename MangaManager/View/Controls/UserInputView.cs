using NStack;
using System;
using System.Threading;
using Terminal.Gui;

namespace MangaManager.View
{
    public class UserInputView : Terminal.Gui.View
    {
        private MultiColorTextView TextView;
        private TextView UserInputTextView;
        private ManualResetEvent WaitUserInputResetEvent = new ManualResetEvent(false);

        public UserInputView()
        {
            CanFocus = true;

            TextView = new MultiColorTextView()
            {
                X = 0,
                Y = 0,
                Height = 1,
                Width = Dim.Fill(),
                CanFocus = false
            };

            UserInputTextView = new TextView()
            {
                X = 0,
                Y = 1,
                Height = 1,
                Width = Dim.Fill(),
                ColorScheme = new ColorScheme() { Normal = new Terminal.Gui.Attribute(Color.White, Color.Black), Focus = new Terminal.Gui.Attribute(Color.White, Color.Black) },
                CanFocus = false,
                DesiredCursorVisibility = CursorVisibility.Invisible,
            };

            UserInputTextView.KeyPress += UserInputTextViewKeyPress;

            Add(TextView, UserInputTextView);
        }

        public string AskUserInput(string message)
        {
            WaitUserInputResetEvent.Reset();

            var nbLines = message.Split(Environment.NewLine).Length;
            Application.MainLoop.Invoke(() =>
            {
                TextView.Height = nbLines;
                UserInputTextView.Y = nbLines;
                TextView.Text = message;
                UserInputTextView.CanFocus = true;
                UserInputTextView.DesiredCursorVisibility = CursorVisibility.UnderlineFix;
                UserInputTextView.SetFocus();                
            });

            WaitUserInputResetEvent.WaitOne();

            var userInput = UserInputTextView.Text.ToString();
            Application.MainLoop.Invoke(() =>
            {
                TextView.Text = string.Empty;
                UserInputTextView.Text = string.Empty;
                UserInputTextView.CanFocus = false;
                UserInputTextView.DesiredCursorVisibility = CursorVisibility.Invisible;
            });

            return userInput;
        }

        private void UserInputTextViewKeyPress(KeyEventEventArgs obj)
        {
            if (obj.KeyEvent.Key == Key.Enter)
            {
                WaitUserInputResetEvent.Set();
            }
        }
    }
}
