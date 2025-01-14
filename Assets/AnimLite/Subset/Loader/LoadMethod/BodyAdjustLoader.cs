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

namespace AnimLite.Loader
{
    using AnimLite.Utility;

    using AnimLite.Vmd;
    using AnimLite.Vrm;
    //using System.Runtime.InteropServices.ComTypes;


    public static class BodyAdjustLoader
    {

        /// <summary>
        /// デフォルトの BodyAdjust を返す。
        /// </summary>
        public static AsyncLazy<BodyAdjustData> DefaultBodyAdjustAsync { get; } =
            new AsyncLazy<BodyAdjustData>(async () =>
            {
                await Awaitable.MainThreadAsync();

                return await new ResourceName("default_body_adjust")
                    .LoadResourceToStreamAsync<TextAsset>(asset => asset.bytes, default)
                    .UsingAwait(s => s.ParseBodyAdjustAsync(default));
            });



        public static async ValueTask<BodyAdjustData> LoadBodyAdjustAsync(
            this IArchive archive, PathUnit path, CancellationToken ct)
            {
                if (!path.IsBlank() && archive is not null && !path.IsFullPath())
                {
                    var body_adjust = await LoadErr.LoggingAsync(() =>
                        archive.GetEntryAsync(path, s => s.ParseBodyAdjustAsync(ct), ct));

                    if (body_adjust is not null)
                        return body_adjust;

                    if (archive.FallbackArchive is not null)
                        return await archive.FallbackArchive.LoadBodyAdjustAsync(path, ct);
                }

                return await path.LoadBodyAdjustAsync(ct);
            }



        /// <summary>
        /// path がブランク … デフォルトリソース（存在しなければ default が返る）
        /// as resourse     … リソース
        /// その他          … ファイル or http
        /// </summary>
        public static async ValueTask<BodyAdjustData> LoadBodyAdjustAsync(this PathUnit path, CancellationToken ct) =>
            await LoadErr.LoggingAsync(async () =>
        {
            
            if (path.IsBlank()) return await BodyAdjustLoader.DefaultBodyAdjustAsync;


            ValueTask<Stream> openAsync_(PathUnit path) =>
                path.OpenStreamFileOrWebOrAssetAsync<TextAsset>(asset => asset.bytes, ct);


            var (fullpath, queryString) = path.ToFullPath().DividToPathAndQueryString();
            fullpath.ThrowIfAccessedOutsideOfParentFolder();


            return fullpath.DividZipToArchiveAndEntry() switch
            {
                var (zippath, entrypath) when entrypath != "" =>
                    await openAsync_(zippath + queryString)
                        .UsingAwait(s => s.UnzipAsync(entrypath, s => s.ParseBodyAdjustAsync(ct))),

                var (zippath, _) when fullpath.IsZipArchive() =>
                    await openAsync_(zippath + queryString)
                        .UsingAwait(s => s.UnzipFirstEntryAsync("body_adjust.txt", (s, _) => s.ParseBodyAdjustAsync(ct))),

                var (_, _) =>
                    await openAsync_(fullpath + queryString)
                        .UsingAwait(s => s.ParseBodyAdjustAsync(ct)),
            };
        });


    }
}