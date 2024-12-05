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






}
