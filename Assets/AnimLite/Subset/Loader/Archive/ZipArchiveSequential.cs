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
    /// local zip archive を同期的に扱う
    /// http は local tmp にロードしてから扱う
    /// entry は zip のエントリ
    /// </summary>
    public class ZipArchiveSequential : IArchive
    {
        public ZipArchiveSequential(PathUnit archivepath, PathUnit parentpath, IArchive fallback = null)
        {
            this.parentpath = parentpath;
            this.stream = archivepath.OpenReadFileStream();
            this.zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true, LocalEncoding.sjis);
            this.FallbackArchive = fallback;
        }
        //public ZipArchiveSequential(Stream stream, PathUnit parentpath, IArchive fallback = null)
        //{
        //    this.parentpath = parentpath;
        //    this.stream = stream;
        //    this.zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true, LocalEncoding.sjis);
        //    this.FallbackArchive = fallback;
        //}

        ZipArchive zip;
        Stream stream;
        PathUnit parentpath;

        public IArchive FallbackArchive { get; private set; }

        public virtual void Dispose()
        {
            if (this.zip is null) return;
            $"{this.GetType()} arch disposing".ShowDebugLog();

            this.zip.Dispose();
            this.stream.Dispose();// archive を破棄しても破棄されないので（閉じられるらしいのに）
            this.zip = null;
            this.stream = null;

            this.FallbackArchive?.Dispose();
        }


        public ValueTask<T> GetEntryAsync<T>(PathUnit entryPath, Func<Stream, T> convertAction, CancellationToken ct) =>
            new ValueTask<T>(this.zip.Unzip((this.parentpath + entryPath).NormalizeReativeWithSlash(), convertAction));

        public ValueTask<T> GetEntryAsync<T>(PathUnit entryPath, Func<Stream, ValueTask<T>> convertAction, CancellationToken ct) =>
            this.zip.UnzipAsync((this.parentpath + entryPath).NormalizeReativeWithSlash(), convertAction);


        public ValueTask<T> FindFirstEntryAsync<T>(string extensionlist, Func<Stream, T> convertAction, CancellationToken ct) =>
            new ValueTask<T>(this.zip.UnzipFirstEntry(this.parentpath + $"*{extensionlist}", convertAction));

        public ValueTask<T> FindFirstEntryAsync<T>(string extensionlist, Func<Stream, ValueTask<T>> convertAction, CancellationToken ct) =>
            this.zip.UnzipFirstEntryAsync(this.parentpath + $"*{extensionlist}", convertAction);
    }

    public static class ZipArchiveSequentialUtility
    {
        public static async ValueTask<ZipArchiveSequential> OpenZipArchiveSequentialAsync(
            this PathUnit fullpath, QueryString queryString, IArchive fallback, CancellationToken ct)
        {
            var (zippath, entpath) = fullpath.DividZipToArchiveAndEntry();

            var loadpath = (zippath + queryString);
            var archivePath = loadpath.IsHttp()
                //? await fullpath.GetCachePathAsync(StreamOpenUtility.OpenStreamFileOrWebAsync, ct)
                ? await loadpath.GetCachePathAsync(WebLoaderUtility.LoadFromWebAsync, ct)
                : zippath;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            $"zip sequential archive : {fullpath.Value} -> {archivePath.Value}".ShowDebugLog();
#endif
            return new ZipArchiveSequential(archivePath, entpath, fallback);
        }


        //public static ZipArchiveSequential OpenZipArchiveSequential(this Stream stream, IArchive fallback) =>
        //    new ZipArchiveSequential(stream, fallback);

        //public static async ValueTask<ZipArchiveSequential> OpenZipArchiveSequentialAwait(this ValueTask<Stream> stream, IArchive fallback) =>
        //    new ZipArchiveSequential(await stream, fallback);
    }



}
