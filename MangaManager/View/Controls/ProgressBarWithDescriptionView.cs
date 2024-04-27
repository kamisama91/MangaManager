using Terminal.Gui;

namespace MangaManager.View
{
    public class ProgressBarWithDescriptionView : Terminal.Gui.View
    {
        private Label Header;
        private Label Percentage;
        private MultiColorTextView Description;
        private ProgressBar ProgressBar;

        public int Current { get; set; }
        public int Max { get; set; }

        public ProgressBarWithDescriptionView(string title)
        {
            Height = 2;
            Width = Dim.Fill(1);

            Header = new Label(title)
            {
                X = 0,
                Y = 0,
                Height = 1,
                Width = 13
            };
            Percentage = new Label()
            {
                X = Pos.Right(Header) + 1,
                Y = 0,
                Height = 1,
                Width = 6
            };
            ProgressBar = new ProgressBar
            {
                X = Pos.Right(Percentage) + 1,
                Y = 0,
                Height = 1,
                Width = Dim.Fill(),
                ProgressBarFormat = ProgressBarFormat.Simple,
                ProgressBarStyle = ProgressBarStyle.Continuous,
                ColorScheme = new ColorScheme { Normal = new Attribute(Color.DarkGray, Color.Black) }
            };
            Description = new MultiColorTextView()
            {
                X = 3,
                Y = 1,
                Height = 1,
                Width = Dim.Fill(2),
                CanFocus = false
            };

            Add(Header, Percentage, ProgressBar, Description);
        }

        public void Refresh(int current, int max, string description)
        {
            Max = max;
            Current = current;
            var percent = 100 * current / max;
            Application.MainLoop.Invoke(() =>
            {
                ProgressBar.Fraction = percent / 100f;
                Percentage.Text = $"({percent.ToString().PadLeft(3)}%)";
                Description.Text = description;
            });
        }
    }
}
