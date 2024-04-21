using Konsole;

namespace MangaManager.View
{
    public interface IWithCurrentProgressBar : IProgressBar
    {
        int Current { get; }
        string Item { get; }

        void ForceCurrentWithNoRefresh(int current);
        void ForceMaxWithNoRefresh(int max);
    }
}
