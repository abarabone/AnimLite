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

//}

//namespace AnimLite.Vrm
//{
    //using AnimLite.Utility;
    //using AnimLite.Vmd;

    // todo
    // リソース時、gameobject でロードできなかったら .vrm でロードする


    public static partial class VrmLoader
    {

        /// <summary>
        /// デフォルトの facemap を返す。
        /// </summary>
        public static AsyncLazy<VmdFaceMapping> DefaultFacemampAsync { get; } =
            new AsyncLazy<VmdFaceMapping>(async () =>
            {
                await Awaitable.MainThreadAsync();
                //return await (await new ResourceName("default_facemap")
                //    .LoadResourceToStreamAsync<TextAsset>(asset => asset.bytes, default))
                //    .ParseFaceMapAsync(default);
                return await new ResourceName("default_facemap")
                    .LoadResourceToStreamAsync<TextAsset>(asset => asset.bytes, default)
                    .UsingAwait(s => s.ParseFaceMapAsync(default));
                    //.Await(VrmParser.ParseFaceMapAsync, default);
            });
        //static VrmParser()
        //{
        //    DefaultFacemampAsync = new AsyncLazy<VmdFaceMapping>(async () =>
        //    {
        //        return await "face_map_default as resource".ToPath().ParseFaceMapFromResourceAsync(default);
        //    });
        //}



        public static async ValueTask<VmdFaceMapping> LoadFaceMapExAsync(
            this IArchive archive, PathUnit path, CancellationToken ct)
            {
                if (!path.IsBlank() && archive is not null && !path.IsFullPath())
                {
                    var facemap = await LoadErr.LoggingAsync(() =>
                        archive.GetEntryAsync(path, s => s.ParseFaceMapAsync(ct), ct));

                    if (facemap.VmdToVrmMaps is not null)
                        return facemap;

                    if (archive.FallbackArchive is not null)
                        return await archive.FallbackArchive.LoadFaceMapExAsync(path, ct);
                }

                return await path.LoadFaceMapExAsync(ct);
            }

        //public static async ValueTask<VmdFaceMapping> LoadFaceMapExAsync(
        //    this PathUnit entrypath, ZipArchive archive, CancellationToken ct)
        //=>
        //    archive == null || entrypath.IsBlank()
        //        ? await entrypath.LoadFaceMapExAsync(ct)
        //        : await entrypath.LoadFaceMapInArchiveExAsync(archive, ct);



        /// <summary>
        /// path がブランク … デフォルトリソース（存在しなければ default が返る）
        /// as resourse     … リソース
        /// その他          … ファイル or http
        /// </summary>
        public static async ValueTask<VmdFaceMapping> LoadFaceMapExAsync(this PathUnit path, CancellationToken ct) =>
            await LoadErr.LoggingAsync(async () =>
        {
            //if (path.IsBlank()) path = "face_map_default as resource";
            if (path.IsBlank()) return await VrmLoader.DefaultFacemampAsync;

            ValueTask<Stream> openAsync_(PathUnit path) =>
                path.OpenStreamFileOrWebOrAssetAsync<TextAsset>(asset => asset.bytes, ct);

            var (fullpath, queryString) = path.ToFullPath().DividToPathAndQueryString();
            fullpath.ThrowIfAccessedOutsideOfParentFolder();

            return fullpath.DividZipToArchiveAndEntry() switch
            {
                var (zippath, entrypath) when entrypath != "" =>
                    await openAsync_(zippath + queryString)
                        .UsingAwait(s => s.UnzipAsync(entrypath, s => s.ParseFaceMapAsync(ct))),
                var (zippath, _) when fullpath.IsZipArchive() =>
                    await openAsync_(zippath + queryString)
                        .UsingAwait(s => s.UnzipFirstEntryAsync("facemap.txt", (s, _) => s.ParseFaceMapAsync(ct))),
                var (_, _) =>
                    await openAsync_(fullpath + queryString)
                        .UsingAwait(s => s.ParseFaceMapAsync(ct)),
            };
        });


    }
}