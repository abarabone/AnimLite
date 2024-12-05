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




    /// <summary>
    /// 並列のために zip を都度開くようにする
    /// sequential のように共用しない
    /// </summary>
    public class ZipDummyArchiveParallel : IArchive
    {
        public ZipDummyArchiveParallel(PathUnit archiveCachePath, PathUnit parentpath, IArchive fallback = null)
        {
            this.parentpath = parentpath;
            this.archiveCachePath = archiveCachePath;
            this.FallbackArchive = fallback;
        }

        PathUnit archiveCachePath;
        PathUnit parentpath;


        public IArchive FallbackArchive { get; private set; }

        public void Dispose()
        {
            if (this.archiveCachePath.Value is null) return;
            $"{this.GetType()} arch disposing".ShowDebugLog();

            this.archiveCachePath.Value = null;
            this.FallbackArchive?.Dispose();
        }


        public ValueTask<T> GetEntryAsync<T>(PathUnit entryPath, Func<Stream, T> convertAction, CancellationToken ct) =>
            this.archiveCachePath.OpenReadFileStreamEx()
                .UsingAsync(s => s.Unzip((this.parentpath + entryPath).NormalizeReativeWithSlash(), convertAction));

        public ValueTask<T> GetEntryAsync<T>(PathUnit entryPath, Func<Stream, ValueTask<T>> convertAction, CancellationToken ct) =>
            this.archiveCachePath.OpenReadFileStreamEx()
                .UsingAsync(s => s.UnzipAsync((this.parentpath + entryPath).NormalizeReativeWithSlash(), convertAction));


        public ValueTask<T> FindFirstEntryAsync<T>(string extensionlist, Func<Stream, T> convertAction, CancellationToken ct) =>
            this.archiveCachePath.OpenReadFileStreamEx()
                .UsingAsync(s => s.UnzipFirstEntry(this.parentpath + $"*{extensionlist}", convertAction));

        public ValueTask<T> FindFirstEntryAsync<T>(string extensionlist, Func<Stream, ValueTask<T>> convertAction, CancellationToken ct) =>
            this.archiveCachePath.OpenReadFileStreamEx()
                .UsingAsync(s => s.UnzipFirstEntryAsync(this.parentpath + $"*{extensionlist}", convertAction));
    }

    public static class DummyArchiveUtility
    {
        public static async ValueTask<ZipDummyArchiveParallel> OpenDummyArchiveParallelAsync(
            this PathUnit fullpath, QueryString queryString, IArchive fallback, CancellationToken ct)
        {
            var (zippath, entpath) = fullpath.DividZipToArchiveAndEntry();

            var loadpath = (zippath + queryString);
            var archivePath = loadpath.IsHttp()
                //? await fullpath.GetCachePathAsync(StreamOpenUtility.OpenStreamFileOrWebAsync, ct)
                ? await loadpath.GetCachePathAsync(WebLoaderUtility.LoadFromWebAsync, ct)
                : zippath;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            $"zip dummy archive : {fullpath.Value} -> {archivePath.Value}".ShowDebugLog();
#endif
            return new ZipDummyArchiveParallel(archivePath, entpath, fallback);
        }
    }



}
