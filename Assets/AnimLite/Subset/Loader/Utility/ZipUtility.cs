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
using UnityEngine.Scripting;// [Preserve] ‚Ì‚½‚ß
using System.Net.Http;
using System.IO.Compression;
using AnimLite.Vmd;
using System.Text;
using AnimLite.Utility;
using System.IO.MemoryMappedFiles;
using System.Data;

namespace AnimLite.Utility
{
    using AnimLite.Utility.Linq;


    public static class ZipUtility
    {

        // Archive ‚Ì‚Ù‚¤‚É‚à‚Á‚Ä‚¢‚¯‚È‚¢‚¾‚ë‚¤‚©

        /// <summary>
        /// 
        /// </summary>
        public static T Unzip<T>(
            this Stream stream, PathUnit entryPath, Func<Stream, T> createAction)
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, false, LocalEncoding.sjis);

            return zip.Unzip(entryPath, createAction);
        }


        /// <summary>
        /// 
        /// </summary>
        public static async ValueTask<T> UnzipAsync<T>(
            this Stream stream, PathUnit entryPath, Func<Stream, ValueTask<T>> createAction)
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, false, LocalEncoding.sjis);

            return await zip.UnzipAsync(entryPath, createAction);
        }


        /// <summary>
        /// 
        /// </summary>
        public static T UnzipFirstEntry<T>(this Stream stream, string extension, Func<Stream, string, T> createAction)
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, false, LocalEncoding.sjis);

            return zip.UnzipFirstEntry(extension, createAction);
        }


        /// <summary>
        /// 
        /// </summary>
        public static async ValueTask<T> UnzipFirstEntryAsync<T>(
            this Stream stream, string extension, Func<Stream, string, ValueTask<T>> createAction)
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, false, LocalEncoding.sjis);

            return await zip.UnzipFirstEntryAsync(extension, createAction);
        }


        /// <summary>
        /// 
        /// </summary>
        public static T UnzipFirstEntry<T>(this Stream stream, string extension, Func<Stream, T> createAction)
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, false, LocalEncoding.sjis);

            return zip.UnzipFirstEntry(extension, createAction);
        }


        /// <summary>
        /// 
        /// </summary>
        public static async ValueTask<T> UnzipFirstEntryAsync<T>(
            this Stream stream, string extension, Func<Stream, ValueTask<T>> createAction)
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, false, LocalEncoding.sjis);

            return await zip.UnzipFirstEntryAsync(extension, createAction);
        }
    }






    public static class ZipArchiveUtility
    {

        public static T Unzip<T>(
            this ZipArchive zip, PathUnit entryPath, Func<Stream, T> createAction)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            $"zip entry : {entryPath.Value}".ShowDebugLog();
            //zip.Entries.ForEach(x => Debug.Log($"{x.FullName} in zip"));
            zip.Entries.ForEach(x => $"{x.FullName} {x.FullName.ToUtf8()} in zip".ShowDebugLog());
#endif
            var entry = zip.GetEntry(entryPath);
            if (entry == null) return default;

            using var s = entry.Open();

            return createAction(s);
        }


        public static async ValueTask<T> UnzipAsync<T>(
            this ZipArchive zip, PathUnit entryPath, Func<Stream, ValueTask<T>> createAction)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            $"zip entry : {entryPath.Value}".ShowDebugLog();
            //zip.Entries.ForEach(x => Debug.Log($"{x.FullName} in zip"));
            zip.Entries.ForEach(x => $"{x.FullName} {x.FullName.ToUtf8()} in zip".ShowDebugLog());
#endif
            var entry = zip.GetEntry(entryPath);
            if (entry == null) return default;

            using var s = entry.Open();

            return await createAction(s);
        }



        public static T UnzipFirstEntry<T>(this ZipArchive zip, string extensions, Func<Stream, string, T> createAction)
        {
            var entry = zip.Entries
                .WhereWildIn(extensions, x => x.FullName)
                .FirstOrDefault();
            if (entry is null) return default;

            using var s = entry.Open();

            return createAction(s, entry.Name);
        }


        public static async ValueTask<T> UnzipFirstEntryAsync<T>(
            this ZipArchive zip, string extensions, Func<Stream, string, ValueTask<T>> createAction)
        {
            var entry = zip.Entries
                .WhereWildIn(extensions, x => x.FullName)
                .FirstOrDefault();
            if (entry is null) return default;

            using var s = entry.Open();

            return await createAction(s, entry.Name);
        }



        public static T UnzipFirstEntry<T>(this ZipArchive zip, string extensions, Func<Stream, T> createAction) =>
            zip.UnzipFirstEntry(extensions, (s, _) => createAction(s));

        public static ValueTask<T> UnzipFirstEntryAsync<T>(this ZipArchive zip, string extensions, Func<Stream, ValueTask<T>> createAction) =>
            zip.UnzipFirstEntryAsync(extensions, (s, _) => createAction(s));
    }

}
