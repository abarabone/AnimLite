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
using UnityEngine.Scripting;// [Preserve] のため
using System.Net.Http;
using System.IO.Compression;
using System.Text;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices.ComTypes;
using AnimLite.Vmd;

namespace AnimLite.Utility
{
    // 

    /// <summary>
    /// 親を .Dispose() すると、FallbackArchive も .Dispose() される。
    /// ただし Fallback されている側で .Dispose() しても問題ない。
    /// </summary>
    public interface IArchive : IDisposable
    {
        ValueTask<T> GetEntryAsync<T>(PathUnit entryPath, Func<Stream, T> createAction, CancellationToken ct);
        ValueTask<T> GetEntryAsync<T>(PathUnit entryPath, Func<Stream, ValueTask<T>> createAction, CancellationToken ct);

        ValueTask<T> FindFirstEntryAsync<T>(string extension, Func<Stream, T> convertAction, CancellationToken ct);
        ValueTask<T> FindFirstEntryAsync<T>(string extension, Func<Stream, ValueTask<T>> convertAction, CancellationToken ct);


        IArchive FallbackArchive { get; }

    }

}

namespace AnimLite.Utility.experimental2
{


    //public interface IArchive : IDisposable
    //{
    //    T Unpack<T>(PathUnit entryPath, Func<Stream, T> loadAction);


    //    IArchive FallbackArchive { get; }

    //}

    //public class FileArchive : IArchive
    //{
    //    public FileArchive(IArchive fallback = null)
    //    {
    //        this.FallbackArchive = fallback;
    //    }

    //    public IArchive FallbackArchive { get; private set; }


    //    public void Dispose()
    //    {
    //        this.FallbackArchive?.Dispose();
    //        this.FallbackArchive = null;
    //    }

    //    public T Unpack<T>(PathUnit entryPath, Func<Stream, T> loadAction)
    //    {
    //        using var stream = entryPath.OpenReadFileStream();
    //        return loadAction(stream);
    //    }
    //}

    //public class FolderArchive : IArchive
    //{
    //    public FolderArchive(PathUnit archivepath, IArchive fallback = null)
    //    {
    //        this.parentpath = archivepath.Value;
    //        this.FallbackArchive = fallback;
    //    }

    //    string parentpath;
    //    public IArchive FallbackArchive { get; private set; }


    //    public void Dispose()
    //    {
    //        if (this.parentpath is null) return;
    //        Debug.Log("folder arch disposing");

    //        this.parentpath = null;
    //        this.FallbackArchive?.Dispose();
    //        this.FallbackArchive = null;
    //    }

    //    public T Unpack<T>(PathUnit entryPath, Func<Stream, T> loadAction)
    //    {
    //        using var stream = entryPath.ToFullPath(this.parentpath).OpenReadFileStream();
    //        return loadAction(stream);
    //    }
    //}

    //public class ZipArchiveParallel : IArchive
    //{
    //    public ZipArchiveParallel(PathUnit archivepath, IArchive fallback = null)
    //    {
    //        this.mmf = MemoryMappedFile.CreateFromFile(archivepath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
    //        this.FallbackArchive = fallback;
    //    }

    //    MemoryMappedFile mmf;
    //    public IArchive FallbackArchive { get; private set; }


    //    public void Dispose()
    //    {
    //        if (this.mmf is null) return;
    //        Debug.Log("zip parallel disposing");

    //        this.mmf.Dispose();
    //        this.mmf = null;
    //        this.FallbackArchive?.Dispose();
    //        this.FallbackArchive = null;
    //    }

    //    public T Unpack<T>(PathUnit entryPath, Func<Stream, T> loadAction)
    //    {
    //        using var view = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
    //        using var zip = new ZipArchive(view, ZipArchiveMode.Read, leaveOpen: true, LocalEncoding.sjis);

    //        return zip.Unzip(entryPath, loadAction);
    //    }
    //}

    //public class ZipArchiveSequencial : IArchive
    //{
    //    public ZipArchiveSequencial(PathUnit archivepath, IArchive fallback = null)
    //    {
    //        this.stream = archivepath.OpenReadFileStream();
    //        this.zip = new ZipArchive(this.stream, ZipArchiveMode.Read, leaveOpen: true, LocalEncoding.sjis);
    //        this.FallbackArchive = fallback;
    //    }

    //    Stream stream;
    //    ZipArchive zip;
    //    public IArchive FallbackArchive { get; private set; }


    //    public void Dispose()
    //    {
    //        if (this.zip is null) return;
    //        Debug.Log("zip sequencial disposing");

    //        this.zip.Dispose();
    //        this.zip = null;
    //        this.FallbackArchive?.Dispose();
    //        this.FallbackArchive = null;
    //    }

    //    public T Unpack<T>(PathUnit entryPath, Func<Stream, T> loadAction)
    //    {
    //        return this.zip.Unzip(entryPath, loadAction);
    //    }
    //}



    ////public static class Archive
    ////{


    ////    public static async IArchive Open(PathUnit archivepath, IArchive fallback)
    ////    {
    ////        if (archivepath.IsHttp())
    ////        {

    ////            var (fullpath, queryString) = archivepath.ToFullPath().DividToPathAndQueryString();

    ////            fullpath.ThrowIfAccessedOutsideOfParentFolder();


    ////            var stream = await archivepath.OpenStreamFileOrWebAsync
                
    ////            if (archivepath.IsZipArchive())
    ////            {
    ////                var cachepath = await LoadFileCache.Instance.GetCachePathAsync(fullpath, );

    ////                return new ZipArchiveParallel(archivepath + queryString);
    ////            }

    ////        }

    ////        IArchive open_(PathUnit fullpath)
    ////        {

    ////            return fullpath.DividZipToArchiveAndEntry() switch
    ////            {
    ////                var (zippath, entrypath) when entrypath != "" =>
    ////                    new ZipArchiveParallel(zippath, fallback),
    ////                    await openAsync_(zippath + queryString).UnzipAwait(entrypath, VmdParser.ParseVmd),
    ////                var (zippath, _) when fullpath.IsZipArchive() =>
    ////                    await openAsync_(zippath + queryString).UnzipFirstEntryAwait(".vmd", VmdParser.ParseVmd),
    ////                _ =>
    ////                    await openAsync_(fullpath + queryString).UsingAwait(VmdParser.ParseVmd),
    ////            };
    ////        }
    ////    }



    ////    public static async ValueTask<T> LoadAsync<T>(
    ////        this IArchive archive, PathUnit path, Func<Stream, T> loadAction, CancellationToken ct)
    ////    {
    ////        if (path.IsBlank()) return default;

    ////        if (archive is not null && !path.IsFullPath())
    ////        {
    ////            var data = LoadErr.Logging(() =>
    ////                archive.Unpack(path, loadAction));

    ////            if (!data.IsUnload())
    ////                return data;

    ////            if (archive.FallbackArchive is not null)
    ////                return await archive.FallbackArchive.LoadAsync(path, loadAction, ct);
    ////        }


    ////        return Archive.Open(path, archive.FallbackArchive)
    ////            .Unpack(path, loadAction);
    ////    }
    ////    public static async ValueTask<VmdMotionData> LoadAsync(
    ////        this IArchive archive, PathUnit path, CancellationToken ct)
    ////    {
    ////        if (path.IsBlank()) return default;

    ////        if (archive is not null && !path.IsFullPath())
    ////        {
    ////            var data = LoadErr.Logging(() =>
    ////                archive.Extract(path, VmdParser.ParseVmd));

    ////            if (!data.IsUnload())
    ////                return data;

    ////            if (archive.FallbackArchive is not null)
    ////                return await archive.FallbackArchive.LoadVmdExAsync(path, ct);
    ////        }

    ////        return await path.LoadVmdExAsync(ct);
    ////    }




    ////    public static ValueTask<VmdMotionData> LoadAsync(this PathUnit path, IArchive archive, CancellationToken ct) =>
    ////        LoadErr.LoggingAsync(async () =>
    ////        {
    ////            ValueTask<Stream> openAsync_(PathUnit path) =>
    ////                path.OpenStreamFileOrWebOrAssetAsync<BinaryAsset>(asset => asset.bytes, ct);
    ////            //path.OpenStreamFileOrWebAsync(ct);


    ////            if (path.IsBlank()) return default;

    ////            var (fullpath, queryString) = path.ToFullPath().DividToPathAndQueryString();
    ////            fullpath.ThrowIfAccessedOutsideOfParentFolder();

    ////            return fullpath.DividZipToArchiveAndEntry() switch
    ////            {
    ////                var (zippath, entrypath) when entrypath != "" =>
    ////                    await openAsync_(zippath + queryString).UnzipAwait(entrypath, VmdParser.ParseVmd),
    ////                var (zippath, _) when fullpath.IsZipArchive() =>
    ////                    await openAsync_(zippath + queryString).UnzipFirstEntryAwait(".vmd", VmdParser.ParseVmd),
    ////                _ =>
    ////                    await openAsync_(fullpath + queryString).UsingAwait(VmdParser.ParseVmd),
    ////            };
    ////        });



    ////}
}
