using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using MangaManager.Models;

namespace MangaManager.Tasks
{
    public static class CacheArchiveInfos
    {
        public struct CacheArchiveInfosCounters
        {
            public int Hits;
            public int Misses;
        }

        private static ConcurrentDictionary<string, ArchiveInfo> s_ArchiveInfoCache = new ConcurrentDictionary<string, ArchiveInfo>();
        private static CacheArchiveInfosCounters s_Counters = new CacheArchiveInfosCounters();

        public static int Hits => s_Counters.Hits;
        public static int Misses => s_Counters.Misses;

        public static bool Exists(string path)
        {
            var result = s_ArchiveInfoCache.ContainsKey(path);
            if (result) { Interlocked.Increment(ref s_Counters.Hits); }
            else { Interlocked.Increment(ref s_Counters.Misses); }
            return result;
        }

        public static ArchiveInfo Get(string path)
        {
            if (!Exists(path))
            {
                throw new InvalidOperationException("ArchiveInfo is not cached");
            }
            return s_ArchiveInfoCache[path];
        }

        public static void CreateOrUpdate(string path, ArchiveInfo info)
        {
            s_ArchiveInfoCache[path] = info;
        }

        public static void Delete(string path)
        {
            if (Exists(path))
            {
                s_ArchiveInfoCache.Remove(path, out _);
            }
        }

        public static void UpdatePath(string oldPath, string newPath)
        {
            if (!Exists(oldPath)) { return; }
            if (Exists(newPath)) { throw new InvalidOperationException("ArchiveInfo already cached"); } else { Interlocked.Decrement(ref s_Counters.Misses); ; /*Do Not count this miss has it is expected one*/ }

            var info = Get(oldPath);
            CreateOrUpdate(newPath, info);
            Delete(oldPath);
        }
    }

    public class ArchiveInfo
    {
        public bool IsZip { get; set; }
        public bool HasSubdirectories { get; set; }
        public bool HasComicInfo => ComicInfo != null;
        public ComicInfo ComicInfo { get; set; }
    }
}