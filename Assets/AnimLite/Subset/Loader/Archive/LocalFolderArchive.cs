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
            $"{this.GetType()} arch disposing".ShowDebugLog();

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



}
