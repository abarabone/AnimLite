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




        public static async ValueTask<VmdMotionData> LoadVmdExAsync(
            this PathUnit path, ZipArchive archive, CancellationToken ct) =>
            await LoadErr.LoggingAsync(async () =>
        {

            if (archive != null && !path.IsFullPath())
            {
                var data = archive.Unzip(path, VmdParser.ParseVmd);

                if (data.bodyKeyStreams != null) return data;
            }

            return await path.LoadVmdExAsync(ct);
        });




        public static async ValueTask<VmdMotionData> LoadVmdExAsync(this PathUnit path, CancellationToken ct) =>
            await LoadErr.LoggingAsync(async () =>
        {
            ValueTask<Stream> openAsync_(PathUnit path) =>
                path.OpenStreamFileOrWebOrAssetAsync<BinaryAsset>(asset => asset.bytes, ct);

            var fullpath = path.ToFullPath();
            fullpath.ThrowIfAccessedOutsideOfParentFolder();

            return fullpath.DividZipAndEntry() switch
            {
                var (zippath, entrypath) when entrypath != "" =>
                    await openAsync_(zippath).UnzipAsync(entrypath, VmdParser.ParseVmd),
                var (zippath, _) when fullpath.IsZip() =>
                    await openAsync_(zippath).UnzipFirstEntryAsync(".vmd", VmdParser.ParseVmd),
                _ =>
                    await openAsync_(fullpath).UsingAsync(VmdParser.ParseVmd),
            };
        });




        /////// <summary>
        /////// zip などのストリームをシーク可能にするために、メモリーストリームを介してパースする
        /////// </summary>
        //static async ValueTask<VmdMotionData> parseVmdViaMemoryStreamAsync_(Stream s)
        //{
        //    using var m = new MemoryStream();
        //    await s.CopyToAsync(m);

        //    return VmdParser.ParseVmd(m);
        //}

    }
}
