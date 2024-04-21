namespace MangaManager
{
    public static class Features
    {
        public static bool UseMultiThreading { get; set; } = true;
        public static bool UseProgressBarWithColor { get; set; } = false;
        public static bool UseProgressBar { get; set; } = false;
        public static int ProgressBarStep { get; set; } = 200;
    }
}
