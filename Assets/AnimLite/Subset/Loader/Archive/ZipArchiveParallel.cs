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
    /// 並列読取のために
    /// メモリマップを使うことで、ファイルストリームを共有する。
    /// zip archive は共有できないが、その元であるファイルは共有できる。
    /// </summary>
    public class ZipArchiveParallel : IArchive
    {
        public ZipArchiveParallel(PathUnit archivepath, PathUnit parentpath, IArchive fallback = null)
        {
            this.parentpath = parentpath;
            this.mmf = MemoryMappedFile.CreateFromFile(archivepath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            this.FallbackArchive = fallback;
        }

        MemoryMappedFile mmf;
        PathUnit parentpath;

        public IArchive FallbackArchive { get; private set; }


        public void Dispose()
        {
            if (this.mmf is null) return;
            $"{this.GetType()} arch disposing".ShowDebugLog();

            this.mmf.Dispose();
            this.mmf = null;
            this.FallbackArchive?.Dispose();
            this.FallbackArchive = null;
        }


        public ValueTask<T> GetEntryAsync<T>(PathUnit entryPath, Func<Stream, T> convertAction, CancellationToken ct)
        {
            using var view = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
            using var zip = new ZipArchive(view, ZipArchiveMode.Read, leaveOpen: true, LocalEncoding.sjis);

            var path = (this.parentpath + entryPath).NormalizeReativeWithSlash();
            return new ValueTask<T>(zip.Unzip(path, convertAction));

        }
        public async ValueTask<T> GetEntryAsync<T>(PathUnit entryPath, Func<Stream, ValueTask<T>> convertAction, CancellationToken ct)
        {
            using var view = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
            using var zip = new ZipArchive(view, ZipArchiveMode.Read, leaveOpen: true, LocalEncoding.sjis);

            var path = (this.parentpath + entryPath).NormalizeReativeWithSlash();
            return await zip.UnzipAsync(path, convertAction);
        }

        public ValueTask<T> FindFirstEntryAsync<T>(string extensionlist, Func<Stream, T> convertAction, CancellationToken ct)
        {
            using var view = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
            using var zip = new ZipArchive(view, ZipArchiveMode.Read, leaveOpen: true, LocalEncoding.sjis);

            return new ValueTask<T>(zip.UnzipFirstEntry(this.parentpath + $"*{extensionlist}", convertAction));
        }
        public async ValueTask<T> FindFirstEntryAsync<T>(string extensionlist, Func<Stream, ValueTask<T>> convertAction, CancellationToken ct)
        {
            using var view = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
            using var zip = new ZipArchive(view, ZipArchiveMode.Read, leaveOpen: true, LocalEncoding.sjis);

            return await zip.UnzipFirstEntryAsync(this.parentpath + $"*{extensionlist}", convertAction);
        }
    }

    public static class ZipArchiveParallelUtility
    {
        public static async ValueTask<ZipArchiveParallel> OpenZipArchiveParallelAsync(
            this PathUnit fullpath, QueryString queryString, IArchive fallback, CancellationToken ct)
        {
            var (zippath, entpath) = fullpath.DividZipToArchiveAndEntry();

            var loadpath = (zippath + queryString);
            var archivePath = loadpath.IsHttp()
                //? await fullpath.GetCachePathAsync(StreamOpenUtility.OpenStreamFileOrWebAsync, ct)
                ? await loadpath.GetCachePathAsync(WebLoaderUtility.LoadFromWebAsync, ct)
                : zippath;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            $"zip parallel archive : {fullpath.Value} -> {archivePath.Value}".ShowDebugLog();
#endif
            return new ZipArchiveParallel(archivePath, entpath, fallback);
        }
    }



}
