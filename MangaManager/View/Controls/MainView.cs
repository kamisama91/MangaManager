using Microsoft.DotNet.PlatformAbstractions;
using System.Linq;
using Terminal.Gui;

namespace MangaManager.View
{
    public class MainView : Window
    {
        static MainView()
        {
            Application.Init();
        }

        public FrameView _tasks { get; private set; }
        public ProgressBarWithDescriptionView _convertProgressBar { get; private set; }
        public ProgressBarWithDescriptionView _renameProgressBar { get; private set; }
        public ProgressBarWithDescriptionView _moveProgressBar { get; private set; }
        public ProgressBarWithDescriptionView _scrapProgressBar { get; private set; }
        public ProgressBarWithDescriptionView _tagProgressBar { get; private set; }
        public ProgressBarWithDescriptionView _onlineUpdateProgressBar { get; private set; }
        public ProgressBarWithDescriptionView _archiveProgressBar { get; private set; }
        public ScrollableMultiColorTextView _logTextView { get; private set; }
        public UserInputView _userInputView { get; private set; }

        public MainView()
        {
            Title = "Manga Manager";
            X = 0;
            Y = 0;
            Width = Dim.Fill();
            Height = Dim.Fill();
            ColorScheme = new ColorScheme { Normal = new Attribute(Color.White, Color.Black) };


            _tasks = new FrameView("")
            {
                X = 0,
                Y = 0,
                Height = 16,
                Width = Dim.Percent(60),
            };
            var prompt = new FrameView("")
            {
                X = Pos.Right(_tasks),
                Y = 0,
                Height = 16,
                Width = Dim.Fill(),
            };
            var logs = new FrameView("")
            {
                X = 0,
                Y = Pos.Bottom(_tasks),
                Height = Dim.Fill(1),
                Width = Dim.Fill(),
            };

            _convertProgressBar = BuildProgressBar("Convert", Program.Options.Convert);
            _renameProgressBar = BuildProgressBar("Rename", Program.Options.Rename);
            _moveProgressBar = BuildProgressBar("Move", Program.Options.Move);
            _scrapProgressBar = BuildProgressBar("Scrap", Program.Options.Scrap);
            _tagProgressBar = BuildProgressBar("Tag", Program.Options.Tag);
            _onlineUpdateProgressBar = BuildProgressBar("Online upd.", Program.Options.OnlineUpdate);
            _archiveProgressBar = BuildProgressBar("Archive", Program.Options.Archive);

            //prompt
            _userInputView = new UserInputView()
            {
                X = 1,
                Y = 0,
                Height = Dim.Fill(),
                Width = Dim.Fill(1),
            };
            prompt.Add(_userInputView);

            _logTextView = new ScrollableMultiColorTextView
            {
                X = 1,
                Y = 0,
                Height = Dim.Fill(),
                Width = Dim.Fill(1),
                Multiline = true,
                ScrollBarVisible = true,
            };
            logs.Add(_logTextView);

            var quit = new StatusItem(Key.Q | Key.CtrlMask, "~CTRL-Q~ Quit", () => Application.RequestStop());
            var version = new StatusItem(Key.CharMask, $"Version: {typeof(ViewController).Assembly.GetName().Version.Major}.{typeof(ViewController).Assembly.GetName().Version.Minor}", null);
            var os = new StatusItem(Key.CharMask, $"OS: {RuntimeEnvironment.OperatingSystem} {RuntimeEnvironment.OperatingSystemVersion.Split('.')[0]}", null);
            var statusBar = new StatusBar() { Visible = true, };
            statusBar.Items = new[] { quit, version, os };

            Add(_tasks, prompt, logs, statusBar);
        }

        private ProgressBarWithDescriptionView BuildProgressBar(string title, bool enabled)
        {
            if (!enabled)
                return null;


            var YPos = _tasks.Subviews[0].Subviews.Any()
                ? Pos.Bottom(_tasks.Subviews[0].Subviews.LastOrDefault())
                : 0;
            var progressBar = new ProgressBarWithDescriptionView(title) { X = 1, Y = YPos };
            _tasks.Add(progressBar);
            return progressBar;
        }

        public void Show()
        {
            Application.Top.Add(this);
            Application.Run();
            Application.Shutdown();
        }
    }
}
