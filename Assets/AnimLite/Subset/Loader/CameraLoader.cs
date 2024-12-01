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

namespace AnimLite.Utility
{
    using AnimLite.Vmd;
    using AnimLite.Vrm;

}

namespace AnimLite.Vmd
{
    using AnimLite.Utility;


    public static partial class VmdLoader
    {




        public static async ValueTask<VmdCameraData> LoadVmdCameraExAsync(
            this IArchive archive, PathUnit path, CancellationToken ct)
        {
            if (path.IsBlank()) return default;

            if (archive is not null && !path.IsFullPath())
            {
                var data = await LoadErr.LoggingAsync(() =>
                    archive.GetEntryAsync(path, VmdParser.ParseVmdCamera, ct));

                if (!data.IsUnload())
                    return data;

                if (archive.FallbackArchive is not null)
                    return await archive.FallbackArchive.LoadVmdCameraExAsync(path, ct);
            }

            return await path.LoadVmdCameraExAsync(ct);
        }




        public static async ValueTask<VmdCameraData> LoadVmdCameraExAsync(this PathUnit path, CancellationToken ct) =>
            await LoadErr.LoggingAsync(async () =>
        {
            ValueTask<Stream> openAsync_(PathUnit path) =>
                path.OpenStreamFileOrWebOrAssetAsync<BinaryAsset>(asset => asset.bytes, ct);


            if (path.IsBlank()) return default;

            var (fullpath, queryString) = path.ToFullPath().DividToPathAndQueryString();
            fullpath.ThrowIfAccessedOutsideOfParentFolder();

            return fullpath.DividZipToArchiveAndEntry() switch
            {
                var (zippath, entrypath) when entrypath != "" =>
                    await openAsync_(zippath + queryString)
                        .UsingAwait(s => s.Unzip(entrypath, VmdParser.ParseVmdCamera)),
                var (zippath, _) when fullpath.IsZipArchive() =>
                    await openAsync_(zippath + queryString)
                        .UsingAwait(s => s.UnzipFirstEntry(".vmd", VmdParser.ParseVmdCamera)),
                _ =>
                    await openAsync_(fullpath + queryString)
                        .UsingAwait(VmdParser.ParseVmdCamera),
            };
        });


    }
}
