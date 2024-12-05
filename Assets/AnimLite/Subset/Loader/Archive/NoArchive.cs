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



}
