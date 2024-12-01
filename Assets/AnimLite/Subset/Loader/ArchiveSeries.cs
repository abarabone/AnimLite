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


    // ↓これはとりあえず使わない、あとで調整する
    /// <summary>
    /// archive はパスなど持たない
    /// entry は local または http の絶対パスの対象ファイル
    /// アーカイブではないものはこれを使う
    /// http であればクエリストリングも含んでよい
    /// </summary>
    public class NoArchive : IArchive
    {
        public IArchive FallbackArchive => null;

        public void Dispose() { }


        public async ValueTask<T> GetEntryAsync<T>(PathUnit entryPath, Func<Stream, T> convertAction, CancellationToken ct)
        {
            using var s = await entryPath.OpenStreamFileOrWebAsync(ct);
            return convertAction(s);
        }
        public async ValueTask<T> GetEntryAsync<T>(PathUnit entryPath, Func<Stream, ValueTask<T>> convertAction, CancellationToken ct)
        {
            using var s = await entryPath.OpenStreamFileOrWebAsync(ct);
            return await convertAction(s);
        }

        public ValueTask<T> FindFirstEntryAsync<T>(string extension, Func<Stream, T> convertAction, CancellationToken ct)
        {
            var extlist = extension.Split(";");
            var path = Directory.EnumerateDirectories(PathUnit.ParentPath)
                .FirstOrDefault(x =>
                    extlist
                        .Where(ext => x.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase))
                        .Any()
                );
            return this.GetEntryAsync(path, convertAction, ct);
        }
        public ValueTask<T> FindFirstEntryAsync<T>(string extension, Func<Stream, ValueTask<T>> convertAction, CancellationToken ct)
        {
            var extlist = extension.Split(";");
            var path = Directory.EnumerateDirectories(PathUnit.ParentPath)
                .FirstOrDefault(x =>
                    extlist
                        .Where(ext => x.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase))
                        .Any()
                );
            return this.GetEntryAsync(path, convertAction, ct);
        }
    }


    /// <summary>
    /// local のフォルダ
    /// entry はフォルダ内の対象ファイル
    /// </summary>
    public class LocalFolderArchive : IArchive
    {
        public LocalFolderArchive(PathUnit archivepath, IArchive fallback = null)
        {
            //if (Directory.Exists(archivepath)) throw new Exception();
            this.archivepath = archivepath;
            this.FallbackArchive = fallback;
        }

        protected string archivepath;
        public IArchive FallbackArchive { get; private set; }


        public void Dispose()
        {
            if (this.archivepath is null) return;
            Debug.Log("folder arch disposing");

            this.archivepath = null;
            this.FallbackArchive?.Dispose();
            this.FallbackArchive = null;
        }

        public ValueTask<T> GetEntryAsync<T>(PathUnit entryPath, Func<Stream, T> loadAction, CancellationToken ct)
        {
            using var stream = entryPath.ToFullPath(this.archivepath).OpenReadFileStream();
            return new ValueTask<T>(loadAction(stream));
        }
        public async ValueTask<T> GetEntryAsync<T>(PathUnit entryPath, Func<Stream, ValueTask<T>> loadAction, CancellationToken ct)
        {
            using var stream = entryPath.ToFullPath(this.archivepath).OpenReadFileStream();
            return await loadAction(stream);
        }

        public ValueTask<T> FindFirstEntryAsync<T>(string extensions, Func<Stream, T> loadAction, CancellationToken ct)
        {
            var fullpath = Directory.EnumerateFiles(this.archivepath)
                .WhereExtIn(extensions)
                .FirstOrDefault() ?? "";

            using var stream = fullpath.ToPath().OpenReadFileStream();
            return new ValueTask<T>(loadAction(stream));
        }
        public async ValueTask<T> FindFirstEntryAsync<T>(string extensions, Func<Stream, ValueTask<T>> loadAction, CancellationToken ct)
        {
            var fullpath = Directory.EnumerateFiles(this.archivepath)
                .WhereExtIn(extensions)
                .FirstOrDefault() ?? "";

            using var stream = fullpath.ToPath().OpenReadFileStream();
            return await loadAction(stream);
        }
    }

    public static class LocalFolderArchiveUtility
    {
        public static LocalFolderArchive OpenLocalFolderArchive(this PathUnit fullpath, IArchive fallback, CancellationToken ct)
        {
            var archivePath = fullpath;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            $"folder archive : {fullpath.Value} -> {archivePath.Value}".ShowDebugLog();
#endif
            return new LocalFolderArchive(archivePath, fallback);
        }
    }



    /// <summary>
    /// local または http のフォルダ
    /// entry はフォルダ内の対象ファイル
    /// ただしスキャンできないので、拡張子で検索しても default しか返らない
    /// クエリストリングは entry につけること
    /// </summary>
    public class WebFolderArchive : LocalFolderArchive, IArchive
    {
        public WebFolderArchive(PathUnit archivepath, IArchive fallback = null) : base(archivepath, fallback) { }


        public new async ValueTask<T> GetEntryAsync<T>(PathUnit entryPath, Func<Stream, T> loadAction, CancellationToken ct)
        {
            var httppath = this.archivepath + entryPath;
            var cachePath = await httppath.GetCachePathAsync(WebLoaderUtility.LoadFromWebAsync, ct);
            return await base.GetEntryAsync(cachePath, loadAction, ct);
        }
        public new async ValueTask<T> GetEntryAsync<T>(PathUnit entryPath, Func<Stream, ValueTask<T>> loadAction, CancellationToken ct)
        {
            var httppath = this.archivepath + entryPath;
            var cachePath = await httppath.GetCachePathAsync(WebLoaderUtility.LoadFromWebAsync, ct);
            return await base.GetEntryAsync(cachePath, loadAction, ct);
        }

        public new ValueTask<T> FindFirstEntryAsync<T>(string extension, Func<Stream, T> loadAction, CancellationToken ct) => new ValueTask<T>();
        public new ValueTask<T> FindFirstEntryAsync<T>(string extension, Func<Stream, ValueTask<T>> loadAction, CancellationToken ct) => new ValueTask<T>();
    }

    public static class HttpFolderArchiveUtility
    {
        public static WebFolderArchive OpenWebFolderArchive(this PathUnit fullpath, IArchive fallback, CancellationToken ct)
        {
            var archivePath = fullpath;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            $"web folder archive : {fullpath.Value} -> {archivePath.Value}".ShowDebugLog();
#endif
            return new WebFolderArchive(archivePath, fallback);
        }
    }


    public static class FolderArchiveUtility
    {
        public static IArchive OpenFolderArchive(this PathUnit fullpath, IArchive fallback, CancellationToken ct) =>
            fullpath.IsHttp()
                ? fullpath.OpenWebFolderArchive(fallback, ct)
                : fullpath.OpenLocalFolderArchive(fallback, ct);
    }




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
            Debug.Log("dummy disposing");

            this.archiveCachePath.Value = null;
            this.FallbackArchive?.Dispose();
        }


        public ValueTask<T> GetEntryAsync<T>(PathUnit entryPath, Func<Stream, T> convertAction, CancellationToken ct) =>
            this.archiveCachePath.OpenReadStream()
                .UsingAsync(s => s.Unzip((this.parentpath + entryPath).NormalizeReativeWithSlash(), convertAction));

        public ValueTask<T> GetEntryAsync<T>(PathUnit entryPath, Func<Stream, ValueTask<T>> convertAction, CancellationToken ct) =>
            this.archiveCachePath.OpenReadStream()
                .UsingAsync(s => s.UnzipAsync((this.parentpath + entryPath).NormalizeReativeWithSlash(), convertAction));


        public ValueTask<T> FindFirstEntryAsync<T>(string extensionlist, Func<Stream, T> convertAction, CancellationToken ct) =>
            this.archiveCachePath.OpenReadStream()
                .UsingAsync(s => s.UnzipFirstEntry(this.parentpath + $"*{extensionlist}", convertAction));

        public ValueTask<T> FindFirstEntryAsync<T>(string extensionlist, Func<Stream, ValueTask<T>> convertAction, CancellationToken ct) =>
            this.archiveCachePath.OpenReadStream()
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
            this.zip = new ZipArchive(stream, ZipArchiveMode.Read, false, LocalEncoding.sjis);
            this.FallbackArchive = fallback;
        }
        //public ZipArchiveSequential(Stream stream, PathUnit parentpath, IArchive fallback = null)
        //{
        //    this.parentpath = parentpath;
        //    this.stream = stream;
        //    this.zip = new ZipArchive(stream, ZipArchiveMode.Read, false, LocalEncoding.sjis);
        //    this.FallbackArchive = fallback;
        //}

        ZipArchive zip;
        Stream stream;
        PathUnit parentpath;

        public IArchive FallbackArchive { get; private set; }

        public void Dispose()
        {
            if (this.zip is null) return;
            Debug.Log("zip seq disposing");

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
            Debug.Log("zip parallel disposing");

            this.mmf.Dispose();
            this.mmf = null;
            this.FallbackArchive?.Dispose();
            this.FallbackArchive = null;
        }


        public ValueTask<T> GetEntryAsync<T>(PathUnit entryPath, Func<Stream, T> convertAction, CancellationToken ct)
        {
            using var view = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
            using var zip = new ZipArchive(view, ZipArchiveMode.Read, leaveOpen: false, LocalEncoding.sjis);

            var path = (this.parentpath + entryPath).NormalizeReativeWithSlash();
            return new ValueTask<T>(zip.Unzip(path, convertAction));

        }
        public async ValueTask<T> GetEntryAsync<T>(PathUnit entryPath, Func<Stream, ValueTask<T>> convertAction, CancellationToken ct)
        {
            using var view = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
            using var zip = new ZipArchive(view, ZipArchiveMode.Read, leaveOpen: false, LocalEncoding.sjis);

            var path = (this.parentpath + entryPath).NormalizeReativeWithSlash();
            return await zip.UnzipAsync(path, convertAction);
        }

        public ValueTask<T> FindFirstEntryAsync<T>(string extensionlist, Func<Stream, T> convertAction, CancellationToken ct)
        {
            using var view = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
            using var zip = new ZipArchive(view, ZipArchiveMode.Read, leaveOpen: false, LocalEncoding.sjis);

            return new ValueTask<T>(zip.UnzipFirstEntry(this.parentpath + $"*{extensionlist}", convertAction));
        }
        public async ValueTask<T> FindFirstEntryAsync<T>(string extensionlist, Func<Stream, ValueTask<T>> convertAction, CancellationToken ct)
        {
            using var view = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
            using var zip = new ZipArchive(view, ZipArchiveMode.Read, leaveOpen: false, LocalEncoding.sjis);

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
