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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace AnimLite.Loader
{
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;

    using AnimLite.Vmd;
    using AnimLite.Vrm;
    using Newtonsoft.Json.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using static AnimLite.DancePlayable.DanceGraphy;


    public static class JsonLoader
    {


        public static async ValueTask<T> LoadJsonAsync<T>(
            this IArchive archive, PathUnit entrypath, T prevjson, CancellationToken ct) where T : new()
        {
            if (prevjson is null) prevjson = new T();

            //if (entrypath.IsBlank()) return prevjson;

            if (archive is not null)
            {
                var json = await LoadErr.LoggingAsync(async () =>
                    entrypath switch
                    {
                        _ when entrypath != "" =>
                            await archive.GetEntryAsync(entrypath, s => DeserializeJsonAsync<T>(s, prevjson), ct),
                        _ =>
                            await archive.FindFirstEntryAsync(".json", s => DeserializeJsonAsync<T>(s, prevjson), ct),
                        // .json だけは .zip 自体の entry path を参照する。
                        // 他のメディアでは .json に記されたパスを entry path と解釈する。
                    });

                if (json is not null)
                    return json;

                if (archive.FallbackArchive is not null)
                    return await archive.FallbackArchive.LoadJsonAsync(entrypath, prevjson, ct);
            }

            return await entrypath.LoadJsonAsync<T>(prevjson, ct);
        }

        /// <summary>
        /// ds を渡さないバージョン 
        /// </summary>
        public static ValueTask<T> LoadJsonAsync<T>(
            this IArchive archive, PathUnit path, CancellationToken ct) where T:new() =>
                archive.LoadJsonAsync(path, new T(), ct);




        public static async ValueTask<T> LoadJsonAsync<T>(this PathUnit path, T prevjson, CancellationToken ct) where T : new() =>
            await LoadErr.LoggingAsync(async () =>
        {

            ValueTask<Stream> openAsync_(PathUnit path) =>
                path.OpenStreamFileOrWebOrAssetAsync<TextAsset>(asset => asset.bytes, ct);


            if (prevjson is null) prevjson = new T();

            if (path.IsBlank()) return prevjson;

            var (fullpath, queryString) = path.ToFullPath().DividToPathAndQueryString();
            fullpath.ThrowIfAccessedOutsideOfParentFolder();


            return fullpath.DividZipToArchiveAndEntry() switch
            {
                var (zippath, entrypath) when entrypath != "" =>
                    await openAsync_(zippath + queryString)
                        .UsingAwait(s => s.UnzipAsync(entrypath, s => DeserializeJsonAsync<T>(s, prevjson))),
                var (zippath, _) when zippath != "" =>
                    await openAsync_(zippath + queryString)
                        .UsingAwait(s => s.UnzipFirstEntryAsync(".json", s => DeserializeJsonAsync<T>(s, prevjson))),
                var (_, _) =>
                    await openAsync_(fullpath + queryString)
                        .UsingAwait(s => DeserializeJsonAsync<T>(s, prevjson)),
            };
        })
        ??
        new T();

        public static ValueTask<T> LoadJsonAsync<T>(this PathUnit path, CancellationToken ct) where T : new() =>
            path.LoadJsonAsync(new T(), ct);




        static async ValueTask<T> DeserializeJsonAsync<T>(this Stream s, T jsondata)
        {
            using var r = new StreamReader(s);
            var json = await r.ReadToEndAsync();

            JsonConvert.PopulateObject(json, jsondata, JsonSupplemetUtility.DeserializeOptions);
            return jsondata;
        }


    }


}