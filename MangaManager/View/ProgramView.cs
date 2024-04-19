using Konsole;

namespace MangaManager.View
{
    public class ProgramView
    {
        private IConsole _logs;
        private IConsole _forms;

        public ProgramView() 
        {
            var windows = Window.OpenBox("Manga Manager v1.0", 120, 29);
            var tasks = windows.SplitTop("Tasks");
            var bottom = windows.SplitBottom();
            _logs = bottom.SplitRight("Logs");
            _forms = bottom.SplitLeft("User Inputs");
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
