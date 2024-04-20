using System;
using System.Collections.Generic;
using MangaManager.Models;

namespace MangaManager.Tasks
{
    public static class CacheArchiveInfos
    {
        private static Dictionary<string, ArchiveInfo> s_ArchiveInfoCache = new Dictionary<string, ArchiveInfo>();

        public static int Hits { get; private set; }
        public static int Misses { get; private set; }

        public static bool Exists(string path)
        {
            var result = s_ArchiveInfoCache.ContainsKey(path);
            if (result) { Hits++; }
            else { Misses++; }
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
                s_ArchiveInfoCache.Remove(path);
            }
        }

        public static void UpdatePath(string oldPath, string newPath)
        {
            if (!Exists(oldPath)) { return; }
            if (Exists(newPath)) { throw new InvalidOperationException("ArchiveInfo already cached"); } else { Misses--; /*Do Not count this miss has it is expected one*/ }

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