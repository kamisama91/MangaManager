using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using MangaManager.Models;

namespace MangaManager.Tasks
{
    public class ArchiveInfo
    {
        public bool IsZip { get; set; }
        public bool HasSubdirectories { get; set; }
        public bool HasComicInfo => ComicInfo != null;
        public ComicInfo ComicInfo { get; set; }
    }

    public struct ArchiveInfosCounters
    {
        public int Hits;
        public int Misses;
    }

    public static class CacheArchiveInfos
    {
        private static ConcurrentDictionary<string, ArchiveInfo> s_ArchiveInfoCache = new ConcurrentDictionary<string, ArchiveInfo>();
        private static ArchiveInfosCounters s_Counters = new ArchiveInfosCounters();

        public static int Hits => s_Counters.Hits;
        public static int Misses => s_Counters.Misses;

        public static ArchiveInfo GetOrCreate(string path)
        {
            if (s_ArchiveInfoCache.ContainsKey(path))
            {
                Interlocked.Increment(ref s_Counters.Hits);
                return s_ArchiveInfoCache[path];
            }

            Interlocked.Increment(ref s_Counters.Misses);
            var archiveInfo = ArchiveHelper.BuildArchiveInfo(path);
            s_ArchiveInfoCache[path] = archiveInfo;
            return archiveInfo;
        }

        public static void UpdatePath(string oldPath, string newPath)
        {
            if (!s_ArchiveInfoCache.ContainsKey(oldPath))
            {
                return;
            }
            if (s_ArchiveInfoCache.ContainsKey(newPath))
            {
                throw new InvalidOperationException("ArchiveInfo already cached");
            }
            s_ArchiveInfoCache[newPath] = s_ArchiveInfoCache[oldPath];
            s_ArchiveInfoCache.Remove(oldPath, out var _);
        }
    }
}