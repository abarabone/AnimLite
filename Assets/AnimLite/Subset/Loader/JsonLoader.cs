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
    //using UnityEditor.VersionControl;
    using static AnimLite.DancePlayable.DanceGraphy2;


    public static class JsonLoader
    {


        public static async ValueTask<T> LoadJsonAsync<T>(
            this PathUnit path, IArchive archive, CancellationToken ct) =>
            await LoadErr.LoggingAsync(async () =>
        {
            if (archive != null)
            {
                var json = path.ToZipEntryPath() switch
                {
                    var entrypath when entrypath != "" =>
                        await archive.ExtractAsync(entrypath, DeserializeJsonAsync<T>),
                    _ =>
                        await archive.ExtractFirstEntryAsync(".json", DeserializeJsonAsync<T>),
                    // .json だけは .zip 自体の entry path を参照する。
                    // 他のメディアでは .json に記されたパスを entry path と解釈する。
                };

                if (json != null) return json;
            }

            return await path.LoadJsonAsync<T>(ct);
        });



        public static async ValueTask<T> LoadJsonAsync<T>(this PathUnit path, CancellationToken ct) =>
            await LoadErr.LoggingAsync(async () =>
        {
            ValueTask<Stream> openAsync_(PathUnit path) =>
                path.OpenStreamFileOrWebOrAssetAsync<TextAsset>(asset => asset.bytes, ct);

            var (fullpath, queryString) = path.ToFullPath().DividToPathAndQueryString();
            fullpath.ThrowIfAccessedOutsideOfParentFolder();


            return fullpath.DividZipToArchiveAndEntry() switch
            {
                var (zippath, entrypath) when entrypath != "" =>
                    await openAsync_(zippath + queryString).UnzipAwait(entrypath, DeserializeJsonAsync<T>),
                var (zippath, _) when zippath != "" =>
                    await openAsync_(zippath + queryString).UnzipFirstEntryAwait(".json", DeserializeJsonAsync<T>),
                var (_, _) =>
                    await openAsync_(fullpath + queryString).UsingAwait(DeserializeJsonAsync<T>),
            };
        });




        static async ValueTask<T> DeserializeJsonAsync<T>(this Stream s)
        {
            using var r = new StreamReader(s);
            var json = await r.ReadToEndAsync();

            //json.ShowDebugLog();

            return JsonUtility.FromJson<T>(json);
        }



    }


}