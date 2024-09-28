
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using System.Linq;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.IO;

namespace AnimLite.Vmd
{
    using AnimLite.Vrm;
    using AnimLite.Utility;
    using AnimLite.DancePlayable;
    using AnimLite.Utility.Linq;
    

    public class LoadFileCache
    {

        public static LoadFileCache Instance { get; private set; } = null;

        //[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        //static void Init()
        //{
        //    Instance = new LoadFileCache();
        //    Instance.CleanupTmpFiles();

        //    "created load file cache".ShowDebugLog();
        //}

        //[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        //static void Cleanup() => Instance?.CleanupTmpFiles();
        
        static LoadFileCache()
        {
            LoadFileCache.Dispose();
            LoadFileCache.Instance = new LoadFileCache();

            "created load file cache".ShowDebugLog();
        }

        // 現状、どこからも呼ばれてないはず
        public static void Dispose()
        {
            if (LoadFileCache.Instance is null) return;

            LoadFileCache.Instance.CleanupTmpFiles();
            LoadFileCache.Instance = null;
            "load file cache disposed".ShowDebugLog();
        }


        ConcurrentDictionary<PathUnit, AsyncLazy<PathUnit>> cache = new();




        /// <summary>
        /// クエリストリングがついててもよい、アーカイブのエントリーでもよい
        /// </summary>
        public async Task<PathUnit> GetCachePathAsync(PathUnit srcpath, Stream stream, CancellationToken ct)
        {
            var dstpath = await this.cache.GetOrAddLazyAaync(srcpath, async () =>
            {
                var tmpfolderpath = $"{PathUnit.CacheFolderPath}/loadcache";
                var tmpfilepath = $"{tmpfolderpath}/{Path.GetRandomFileName()}{srcpath.GetExt().ToPath().TrimQueryString().Value}".ToPath();

                Directory.CreateDirectory(tmpfolderpath);

                using (stream)
                {
                    using var writer = tmpfilepath.OpenWriteFileStream();
                    await stream.CopyToAsync(writer, ct);
                }
                return tmpfilepath;
            });

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            $"load to cache from http : {srcpath.Value} -> {dstpath.Value}".ShowDebugLog();
#endif
            return dstpath;
        }



        /// <summary>
        /// 
        /// </summary>
        public void CleanupTmpFiles()
        {
            var tmpfolderpath = $"{PathUnit.CacheFolderPath}/loadcache";

            if (!Directory.Exists(tmpfolderpath)) return;

            Directory.Delete(tmpfolderpath, recursive: true);
        }


    }


    public static class LoadFileCacheUtility
    {

        public static async  Task<PathUnit> GetCachePathAsync(
            this PathUnit srcpath, Func<PathUnit, CancellationToken, ValueTask<Stream>> loadAction, CancellationToken ct)
        =>
            await LoadFileCache.Instance.GetCachePathAsync(srcpath, await loadAction(srcpath, ct), ct);


        public static Task<PathUnit> GetCachePathAsync(
            this PathUnit srcpath, Stream stream, CancellationToken ct)
        =>
            LoadFileCache.Instance.GetCachePathAsync(srcpath, stream, ct);

    }


}
