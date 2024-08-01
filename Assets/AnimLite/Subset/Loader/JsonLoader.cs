using AnimLite.DancePlayable;
using AnimLite.Vmd;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UniVRM10;
using Unity.VisualScripting;
using UnityEngine.AddressableAssets;

namespace AnimLite.Utility
{
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;

    using AnimLite.Vmd;
    using AnimLite.Vrm;
    using System.IO.Compression;
    using UnityEditor.VersionControl;
    using static AnimLite.DancePlayable.DanceGraphy2;


    // todo
    // ・http と zip は共存するので対応する
    // ・リソース、http を汎用化できないか
    // ・アセットをロードは可能か？（モデルやアニメーション他を単体、また zip のように入れ子でもいい）

    // リソースは zip をサポートしない

    // zip だったら archive を開く
    // zip はフルパスのみ、相対パスでは zip を考慮しない
    // archive かつ相対パスなら zip entry として中身をロードする
    // archive でもフルパスなら、zip entry ではない
    // フルパスの zip でないものは、普通のロード

    // json は、zip first entry, zip entry, なら zip entry として archive を開いてから、その中の json を開く
    // 

    public static class JsonLoader
    {


        public static async ValueTask<ZipArchive> OpenZipAsync(this PathUnit path, CancellationToken ct)
        {
            ValueTask<Stream> openAsync_(PathUnit path) => path.OpenStreamFileOrWebAsync(ct);

            return path.ToFullPath().DividZipAndEntry() switch
            {
                var (zippath, _) when zippath != "" =>
                    await openAsync_(zippath).OpenZipAsync(),
                _ =>
                    null,
            };
        }



        //public static async ValueTask<T> ReadJsonExAsync<T>(
        //    this PathUnit entrypath, ZipArchive archive, CancellationToken ct)
        //=>
        //    archive == null
        //        ? await entrypath.ReadJsonExAsync<T>(ct)
        //        : await entrypath.ToZipEntryPath().ReadJsonInArchiveExAsync<T>(archive, ct);
        //// .json だけは .zip 自体の entry path を参照する。他のメディアでは .json に記されたパスを entry path と解釈する。

        public static async ValueTask<T> ReadJsonExAsync<T>(
            this PathUnit path, ZipArchive archive, CancellationToken ct)
        {
            if (archive != null)
            {
                var json = path.ToZipEntryPath().Value switch
                {
                    var entrypath when entrypath != "" =>
                        await archive.UnzipAsync(entrypath, DeserializeJsonAsync<T>),
                    _ =>
                        await archive.UnzipFirstEntryAsync(".json", DeserializeJsonAsync<T>),
                        // .json だけは .zip 自体の entry path を参照する。
                        // 他のメディアでは .json に記されたパスを entry path と解釈する。
                };

                if (json != null) return json;
            }

            return await path.ReadJsonExAsync<T>(ct);
        }



        public static async ValueTask<T> ReadJsonExAsync<T>(this PathUnit path, CancellationToken ct)
        {
            ValueTask<Stream> openAsync_(PathUnit path) =>
                path.OpenStreamFileOrWebOrAssetAsync<BinaryAsset>(asset => asset.bytes, ct);

            var fullpath = path.ToFullPath();
            fullpath.ThrowIfAccessedOutsideOfParentFolder();

            return fullpath.DividZipAndEntry() switch
            {
                var (zippath, entrypath) when entrypath != "" =>
                    await openAsync_(zippath).UnzipAsync(entrypath, DeserializeJsonAsync<T>),
                var (zippath, _) when zippath != "" =>
                    await openAsync_(zippath).UnzipFirstEntryAsync(".json", DeserializeJsonAsync<T>),
                var (_, _) =>
                    await openAsync_(fullpath).UsingAsync(DeserializeJsonAsync<T>),
            };
        }

        //public static async ValueTask<T> ReadJsonInArchiveExAsync<T>(
        //    this PathUnit zipentrypath, ZipArchive zip, CancellationToken ct)
        //{
        //    var json = zipentrypath switch
        //    {
        //        _ when zipentrypath != "" =>
        //            await zip.UnzipAsync(zipentrypath, DeserializeJsonAsync<T>),
        //        _ =>
        //            await zip.UnzipFirstEntryAsync(".json", DeserializeJsonAsync<T>),
        //    };

        //    return json ?? await zipentrypath.ToFullPath()
        //        .OpenStreamFileOrWebOrAssetAsync<BinaryAsset>(asset => asset.bytes, ct)
        //        .UsingAsync(DeserializeJsonAsync<T>);
        //}



        static async ValueTask<T> DeserializeJsonAsync<T>(this Stream s)
        {
            var json = await new StreamReader(s).ReadToEndAsync();

            return JsonUtility.FromJson<T>(json);
        }



    }


}