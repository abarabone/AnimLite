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


    public static partial class VmdParser
    {




        public static async ValueTask<VmdMotionData> ParseVmdExAsync(
            this PathUnit path, ZipArchive archive, CancellationToken ct)
        {

            if (archive != null && !path.IsFullPath())
            {
                var data = await archive.UnzipAsync(path, parseVmdViaMemoryStreamAsync_);

                if (data.bodyKeyStreams != null) return data;
            }

            return await path.ParseVmdExAsync(ct);
        }

        //public static async ValueTask<VmdMotionData> ParseVmdExAsync(
        //    this PathUnit path, ZipArchive archive, CancellationToken ct)
        //=>
        //    archive == null
        //        ? await path.ParseVmdExAsync(ct)
        //        : await path.ParseVmdInArchiveExAsync(archive, ct);





        public static async ValueTask<VmdMotionData> ParseVmdExAsync(this PathUnit path, CancellationToken ct)
        {
            ValueTask<Stream> openAsync_(PathUnit path) =>
                path.OpenStreamFileOrWebOrAssetAsync<BinaryAsset>(asset => asset.bytes, ct);

            var fullpath = path.ToFullPath();
            fullpath.ThrowIfAccessedOutsideOfParentFolder();

            return fullpath.DividZipAndEntry() switch
            {
                var (zippath, entrypath) when entrypath != "" =>
                    await openAsync_(zippath).UnzipAsync(entrypath, parseVmdViaMemoryStreamAsync_),
                var (zippath, _) when fullpath.IsZip() => 
                    await openAsync_(zippath).UnzipFirstEntryAsync(".vmd", parseVmdViaMemoryStreamAsync_),
                _ =>
                    await openAsync_(fullpath).UsingAsync(VmdParser.ParseVmd),
            };
        }


        //public static async ValueTask<VmdMotionData> ParseVmdInArchiveExAsync(
        //    this PathUnit zipentrypath, ZipArchive zip, CancellationToken ct)
        //{
        //    if (!zipentrypath.IsFullPath())
        //    {
        //        var data = await zip.UnzipAsync(zipentrypath, parseVmdViaMemoryStreamAsync_);

        //        if (data.bodyKeyStreams != null) return data;
        //    }
            
        //    return await zipentrypath.ToFullPath()
        //        .OpenStreamFileOrWebOrAssetAsync<BinaryAsset>(asset => asset.bytes, ct)
        //        .UsingAsync(VmdParser.ParseVmd);
        //}



        /// <summary>
        /// zip などのストリームをシーク可能にするために、メモリーストリームを介してパースする
        /// </summary>
        static async ValueTask<VmdMotionData> parseVmdViaMemoryStreamAsync_(Stream s)
        {
            using var m = new MemoryStream();
            await s.CopyToAsync(m);

            return VmdParser.ParseVmd(m);
        }

    }
}
