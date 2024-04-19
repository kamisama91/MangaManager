using System;
using System.Collections.Generic;
using MangaManager.Models;

namespace MangaManager.Tasks
{
    public static class CacheComicInfos
    {
        private static Dictionary<string, ComicInfo> s_ComicInfoCache = new Dictionary<string, ComicInfo>();

        public static bool Exists(string path)
        {
            return s_ComicInfoCache.ContainsKey(path);
        }

        public static ComicInfo Get(string path)
        {
            if (!Exists(path)) 
            { 
                throw new InvalidOperationException("ComicInfo is not cached"); 
            }
            return s_ComicInfoCache[path];
        }

        public static void CreateOrUpdate(string path, ComicInfo info)
        {
            s_ComicInfoCache[path] = info;
        }

        public static void Delete(string path)
        {
            if (Exists(path)) 
            {
                s_ComicInfoCache.Remove(path);
            }
        }

        public static void UpdatePath(string oldPath, string newPath)
        {
            if (!Exists(oldPath)) { return; }
            if (Exists(newPath)) { throw new InvalidOperationException("ComicInfo already cached"); }

            var info = Get(oldPath);
            CreateOrUpdate(newPath, info);
            Delete(oldPath);
        }
    }
}