using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.VisualScripting;
using System.IO;
using System.Collections.Concurrent;
using UnityEngine.Networking;
using UniVRM10;
using UnityEngine.AddressableAssets;
using System.Net.Http;
using System.IO.Compression;
using AnimLite.Vmd;
using System.IO.MemoryMappedFiles;

namespace AnimLite.Utility
{
    using AnimLite.Utility.Linq;





    public class ZipArchiveSequentialConcurrent : ZipArchiveSequential, IArchive
    {
        public ZipArchiveSequentialConcurrent(PathUnit archivepath, PathUnit parentpath, IArchive fallback = null)
            : base(archivepath, parentpath, fallback)
        { }

        public override void Dispose()
        {
            this.loadLimiter.Dispose();
            base.Dispose();
        }


        SemaphoreSlim loadLimiter = new (1);



        public new async ValueTask<T> GetEntryAsync<T>(PathUnit entryPath, Func<Stream, T> convertAction, CancellationToken ct)
        {
            using var _ = await this.loadLimiter.WaitAsyncDisposable(ct);
            return await base.GetEntryAsync(entryPath, convertAction, ct);
        }
        public new async  ValueTask<T> GetEntryAsync<T>(PathUnit entryPath, Func<Stream, ValueTask<T>> convertAction, CancellationToken ct)
        {
            using var _ = await this.loadLimiter.WaitAsyncDisposable(ct);
            return await base.GetEntryAsync(entryPath, convertAction, ct);
        }

        public new async ValueTask<T> FindFirstEntryAsync<T>(string extensionlist, Func<Stream, T> convertAction, CancellationToken ct)
        {
            using var _ = await this.loadLimiter.WaitAsyncDisposable(ct);
            return await base.FindFirstEntryAsync(extensionlist, convertAction, ct);
        }
        public new async ValueTask<T> FindFirstEntryAsync<T>(string extensionlist, Func<Stream, ValueTask<T>> convertAction, CancellationToken ct)
        {
            using var _ = await this.loadLimiter.WaitAsyncDisposable(ct);
            return await base.FindFirstEntryAsync(extensionlist, convertAction, ct);
        }
    }



    public static class ZipArchiveSequentialConcurrentUtility
    {
        public static async ValueTask<ZipArchiveSequential> OpenZipArchiveSequentialConcurrentAsync(
            this PathUnit fullpath, QueryString queryString, IArchive fallback, CancellationToken ct)
        {
            var (zippath, entpath) = fullpath.DividZipToArchiveAndEntry();

            var loadpath = (zippath + queryString);
            var archivePath = loadpath.IsHttp()
                //? await fullpath.GetCachePathAsync(StreamOpenUtility.OpenStreamFileOrWebAsync, ct)
                ? await loadpath.GetCachePathAsync(WebLoaderUtility.LoadFromWebAsync, ct)
                : zippath;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            $"zip sequential concurrent archive : {fullpath.Value} -> {archivePath.Value}".ShowDebugLog();
#endif
            return new ZipArchiveSequentialConcurrent(archivePath, entpath, fallback);
        }
    }



}
