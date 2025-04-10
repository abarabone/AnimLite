﻿using System;
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


    public static partial class VmdLoader
    {



        public static async ValueTask<VmdMotionData> LoadVmdAsync(
            this IArchive archive, PathUnit path, CancellationToken ct)
        {
            if (path.IsBlank()) return default;

            if (archive is not null && !path.IsFullPath())
            {
                var data = await LoadErr.LoggingAsync(() =>
                    archive.GetEntryAsync(path, VmdParser.ParseVmd, ct));
                    
                if (!data.IsUnload())
                    return data;

                if (archive.FallbackArchive is not null)
                    return await archive.FallbackArchive.LoadVmdAsync(path, ct);
            }

            return await path.LoadVmdAsync(ct);
        }




        public static ValueTask<VmdMotionData> LoadVmdAsync(this PathUnit path, CancellationToken ct) =>
            LoadErr.LoggingAsync(async () =>
        {
            ValueTask<Stream> openAsync_(PathUnit path) =>
                path.OpenStreamFileOrWebOrAssetAsync<BinaryAsset>(asset => asset.bytes, ct);
                //path.OpenStreamFileOrWebAsync(ct);


            if (path.IsBlank()) return default;

            var (fullpath, queryString) = path.ToFullPath().DividToPathAndQueryString();
            fullpath.ThrowIfAccessedOutsideOfParentFolder();

            return fullpath.DividZipToArchiveAndEntry() switch
            {
                var (zippath, entrypath) when entrypath != "" =>
                    await openAsync_(zippath + queryString).UsingAwait(s => s.Unzip(entrypath, VmdParser.ParseVmd)),
                var (zippath, _) when fullpath.IsZipArchive() =>
                    await openAsync_(zippath + queryString).UsingAwait(s => s.UnzipFirstEntry(".vmd", VmdParser.ParseVmd)),
                _ =>
                    await openAsync_(fullpath + queryString).UsingAwait(VmdParser.ParseVmd),
            };
        });






        public static ValueTask<VmdMotionData> LoadVmdExAsync(
            this IArchive archive, PathList pathlist, CancellationToken ct)
        =>

            // いずれ、並列か直列か選択式にしたい
            pathlist.Paths
                .ToAsyncEnumerable()
                .SelectAwait(x => archive.LoadVmdAsync(x, ct))
                .Where(x => !x.IsUnload())
                .DefaultIfEmpty()
                .AggregateAsync((pre, cur) => pre.AppendOrOverwrite(cur));



        public static ValueTask<VmdMotionData> LoadVmdExAsync(
            this PathList pathlist, CancellationToken ct)
        =>

            // いずれ、並列か直列か選択式にしたい
            pathlist.Paths
                .ToAsyncEnumerable()
                .SelectAwait(x => x.LoadVmdAsync(ct))
                .Where(x => !x.IsUnload())
                .DefaultIfEmpty()
                .AggregateAsync((pre, cur) => pre.AppendOrOverwrite(cur));






    }
}
