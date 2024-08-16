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

namespace AnimLite.Vrm
{
    using AnimLite.Utility;
    using AnimLite.Vmd;

    // todo
    // リソース時、gameobject でロードできなかったら .vrm でロードする


    public static partial class VrmParser
    {

        /// <summary>
        /// デフォルトの facemap を返す。
        /// </summary>
        public static AsyncLazy<VmdFaceMapping> DefaultFacemampAsync { get; } =
            new AsyncLazy<VmdFaceMapping>(async () =>
            {
                await Awaitable.MainThreadAsync();
                return await (await new ResourceName("default_facemap")
                    .LoadResourceToStreamAsync<TextAsset>(asset => asset.bytes, default))
                    .ParseFaceMapAsync(default);
            });
        //static VrmParser()
        //{
        //    DefaultFacemampAsync = new AsyncLazy<VmdFaceMapping>(async () =>
        //    {
        //        return await "face_map_default as resource".ToPath().ParseFaceMapFromResourceAsync(default);
        //    });
        //}



        public static async ValueTask<VmdFaceMapping> LoadFaceMapExAsync(
            this PathUnit path, ZipArchive archive, CancellationToken ct) =>
            await LoadErr.LoggingAsync(async () =>
        {
            if (!path.IsBlank() && archive != null && !path.IsFullPath())
            {
                var facemap = await archive.UnzipAsync(path, s => s.ParseFaceMapAsync(ct));

                if (facemap.VmdToVrmMaps != null) return facemap;
            }

            return await path.LoadFaceMapExAsync(ct);
        });

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
            if (path.IsBlank()) return await VrmParser.DefaultFacemampAsync;

            ValueTask<Stream> openAsync_(PathUnit path) =>
                path.OpenStreamFileOrWebOrAssetAsync<TextAsset>(asset => asset.bytes, ct);

            var fullpath = path.ToFullPath();
            fullpath.ThrowIfAccessedOutsideOfParentFolder();

            return fullpath.DividZipAndEntry() switch
            {
                var (zippath, entrypath) when entrypath != "" =>
                    await openAsync_(zippath).UnzipAsync(entrypath, s => s.ParseFaceMapAsync(ct)),
                var (zippath, _) when fullpath.IsZip() =>
                    await openAsync_(zippath).UnzipFirstEntryAsync("facemap.txt", (s, _) => s.ParseFaceMapAsync(ct)),
                var (_, _) =>
                    await openAsync_(fullpath).UsingAsync(s => s.ParseFaceMapAsync(ct)),
            };
        });


    }
}