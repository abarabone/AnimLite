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
using UnityEngine.Scripting;// [Preserve] のため
using System.Net.Http;
using System.IO.Compression;
using AnimLite.Vmd;
using System.Text;
using AnimLite.Utility;

namespace AnimLite.Utility
{



    public class ZipDummyArchive : IArchive
    {
        public ZipDummyArchive(PathUnit archiveCachePath) => this.archiveCachePath = archiveCachePath;

        PathUnit archiveCachePath;


        public void Dispose() { }


        public T Extract<T>(PathUnit entryPath, Func<Stream, T> convertAction) =>
            this.archiveCachePath.OpenReadStream().Unzip(entryPath, convertAction);
        
        public ValueTask<T> ExtractAsync<T>(PathUnit entryPath, Func<Stream, ValueTask<T>> convertAction) =>
            this.archiveCachePath.OpenReadStream().UnzipAsync(entryPath, convertAction);
        

        public T ExtractFirstEntry<T>(string extension, Func<Stream, T> convertAction) =>
            this.archiveCachePath.OpenReadStream().UnzipFirstEntry(extension, convertAction);
        
        public ValueTask<T> ExtractFirstEntryAsync<T>(string extension, Func<Stream, ValueTask<T>> convertAction) =>
            this.archiveCachePath.OpenReadStream().UnzipFirstEntryAsync(extension, convertAction);
        
    }

    public static class DummyArchiveUtility
    {
        public static async ValueTask<ZipDummyArchive> OpenDummyArchiveAsync(this PathUnit fullpath, CancellationToken ct)
        {
            var cachePath = await fullpath.GetCachePathAsync(StreamOpenUtility.OpenStreamFileOrWebAsync, ct);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            $"dummy archive : {fullpath.Value} -> {cachePath.Value}".ShowDebugLog();
#endif
            return new ZipDummyArchive(cachePath);
        }
    }


    public class ZipWrapArchive : IArchive
    {
        public ZipWrapArchive(Stream stream)
        {
            this.stream = stream;
            this.zip = new ZipArchive(stream, ZipArchiveMode.Read, false, LocalEncoding.sjis);
        }

        ZipArchive zip;

        Stream stream;


        public void Dispose()
        {
            this.zip.Dispose();
            this.stream.Dispose();// archive を破棄しても破棄されないので（閉じられるらしいのに）
        }


        public T Extract<T>(PathUnit entryPath, Func<Stream, T> convertAction) =>
            this.zip.Unzip(entryPath, convertAction);

        public ValueTask<T> ExtractAsync<T>(PathUnit entryPath, Func<Stream, ValueTask<T>> convertAction) =>
            this.zip.UnzipAsync(entryPath, convertAction);


        public T ExtractFirstEntry<T>(string extension, Func<Stream, T> convertAction) =>
            this.zip.UnzipFirstEntry(extension, convertAction);

        public ValueTask<T> ExtractFirstEntryAsync<T>(string extension, Func<Stream, ValueTask<T>> convertAction) =>
            this.zip.UnzipFirstEntryAsync(extension, convertAction);
    }

    public static class ZipWrapArchiveUtility
    {
        public static ZipWrapArchive OpenZipArchive(this Stream stream) =>
            new ZipWrapArchive(stream);

        public static async ValueTask<ZipWrapArchive> OpenZipArchiveAwait(this ValueTask<Stream> stream) =>
            new ZipWrapArchive(await stream);
    }




    public static class ZipUtility
    {

        // Archive のほうにもっていけないだろうか


        public static async ValueTask<T> UnzipAwait<T>(
            this ValueTask<Stream> stream, PathUnit entryPath, Func<Stream, T> createAction)
        =>
            (await stream).Unzip(entryPath, createAction);


        public static T Unzip<T>(
            this Stream stream, PathUnit entryPath, Func<Stream, T> createAction)
        {
            using (stream)
            {
                using var zip = new ZipArchive(stream, ZipArchiveMode.Read, false, LocalEncoding.sjis);

                return zip.Unzip(entryPath, createAction);
            }
        }



        public static async ValueTask<T> UnzipAwait<T>(
            this ValueTask<Stream> stream, PathUnit entryPath, Func<Stream, ValueTask<T>> createAction)
        =>
            await (await stream).UnzipAsync(entryPath, createAction);


        public static async ValueTask<T> UnzipAsync<T>(
            this Stream stream, PathUnit entryPath, Func<Stream, ValueTask<T>> createAction)
        {
            using (stream)
            {
                using var zip = new ZipArchive(stream, ZipArchiveMode.Read, false, LocalEncoding.sjis);

                return await zip.UnzipAsync(entryPath, createAction);
            }
        }


        public static async ValueTask<T> UnzipFirstEntryAwait<T>(
            this ValueTask<Stream> stream, string extension, Func<Stream, string, T> createAction)
        =>
            (await stream).UnzipFirstEntry(extension, createAction);


        public static T UnzipFirstEntry<T>(this Stream stream, string extension, Func<Stream, string, T> createAction)
        {
            using (stream)
            {
                using var zip = new ZipArchive(stream, ZipArchiveMode.Read, false, LocalEncoding.sjis);

                return zip.UnzipFirstEntry(extension, createAction);
            }
        }


        public static async ValueTask<T> UnzipFirstEntryAwait<T>(
            this ValueTask<Stream> stream, string extension, Func<Stream, string, ValueTask<T>> createAction)
        =>
            await (await stream).UnzipFirstEntryAsync(extension, createAction);


        public static async ValueTask<T> UnzipFirstEntryAsync<T>(
            this Stream stream, string extension, Func<Stream, string, ValueTask<T>> createAction)
        {
            using (stream)
            {
                using var zip = new ZipArchive(stream, ZipArchiveMode.Read, false, LocalEncoding.sjis);

                return await zip.UnzipFirstEntryAsync(extension, createAction);
            }
        }


        // バリエーション書かないといけないのはきつな…
        // this Stream と this ValueTask<Stream> がめんどうだし、わかりづらい


        public static ValueTask<T> UnzipFirstEntryAwait<T>(this ValueTask<Stream> stream, string extension, Func<Stream, T> createAction) =>
            stream.UnzipFirstEntryAwait(extension, (s, _) => createAction(s));

        public static T UnzipFirstEntry<T>(this Stream stream, string extension, Func<Stream, T> createAction) =>
            stream.UnzipFirstEntry(extension, (s, _) => createAction(s));


        public static ValueTask<T> UnzipFirstEntryAwait<T>(this ValueTask<Stream> stream, string extension, Func<Stream, ValueTask<T>> createAction) =>
            stream.UnzipFirstEntryAwait(extension, (s, _) => createAction(s));

        public static ValueTask<T> UnzipFirstEntryAsync<T>(this Stream stream, string extension, Func<Stream, ValueTask<T>> createAction) =>
            stream.UnzipFirstEntryAsync(extension, (s, _) => createAction(s));
    }






    public static class ZipArchiveUtility
    {


        public static async ValueTask<T> UnzipAwait<T>(
            this ValueTask<ZipArchive> zip, PathUnit entryPath, Func<Stream, T> createAction)
        =>
            (await zip).Unzip(entryPath, createAction);


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



        public static async ValueTask<T> UnzipAwait<T>(
            this ValueTask<ZipArchive> zip, PathUnit entryPath, Func<Stream, ValueTask<T>> createAction)
        =>
            await (await zip).UnzipAsync(entryPath, createAction);


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


        //public static ValueTask<T> UnzipAsync<T>(
        //    this ValueTask<ZipArchive> zip, PathUnit entryPath, Func<Stream, T> createAction)
        //=>
        //    zip.UnzipAsync(entryPath, (s, _) => createAction(s));

        //public static T Unzip<T>(
        //    this ZipArchive zip, PathUnit entryPath, Func<Stream, T> createAction)
        //=>
        //    zip.Unzip(entryPath, (s, _) => createAction(s));


        //public static ValueTask<T> UnzipAsync<T>(
        //    this ValueTask<ZipArchive> zip, PathUnit entryPath, Func<Stream, ValueTask<T>> createAction)
        //=>
        //    zip.UnzipAsync(entryPath, (s, _) => createAction(s));

        //public static ValueTask<T> UnzipAsync<T>(
        //    this ZipArchive zip, PathUnit entryPath, Func<Stream, ValueTask<T>> createAction)
        //=>
        //    zip.UnzipAsync(entryPath, (s, _) => createAction(s));





        public static async ValueTask<T> UnzipFirstEntryAwait<T>(
            this ValueTask<ZipArchive> zip, string extension, Func<Stream, string, T> createAction)
        =>
            (await zip).UnzipFirstEntry(extension, createAction);


        public static T UnzipFirstEntry<T>(this ZipArchive zip, string extension, Func<Stream, string, T> createAction)
        {
            var extlist = extension.Split(";");
            var entry = zip.Entries.FirstOrDefault(x => extlist.Where(ext => x.Name.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase)).Any());
            if (entry == null) return default;

            using var s = entry.Open();

            return createAction(s, entry.Name);
        }


        public static async ValueTask<T> UnzipFirstEntryAwait<T>(
            this ValueTask<ZipArchive> zip, string extension, Func<Stream, string, ValueTask<T>> createAction)
        =>
            await (await zip).UnzipFirstEntryAsync(extension, createAction);


        public static async ValueTask<T> UnzipFirstEntryAsync<T>(
            this ZipArchive zip, string extension, Func<Stream, string, ValueTask<T>> createAction)
        {
            var extlist = extension.Split(";");
            var entry = zip.Entries.FirstOrDefault(x => extlist.Where(ext => x.Name.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase)).Any());
            if (entry == null) return default;

            using var s = entry.Open();

            return await createAction(s, entry.Name);
        }



        public static ValueTask<T> UnzipFirstEntryAwait<T>(this ValueTask<ZipArchive> zip, string extension, Func<Stream, T> createAction) =>
            zip.UnzipFirstEntryAwait(extension, (s, _) => createAction(s));

        public static T UnzipFirstEntry<T>(this ZipArchive zip, string extension, Func<Stream, T> createAction) =>
            zip.UnzipFirstEntry(extension, (s, _) => createAction(s));


        public static ValueTask<T> UnzipFirstEntryAwait<T>(this ValueTask<ZipArchive> zip, string extension, Func<Stream, ValueTask<T>> createAction) =>
            zip.UnzipFirstEntryAwait(extension, (s, _) => createAction(s));

        public static ValueTask<T> UnzipFirstEntryAsync<T>(this ZipArchive zip, string extension, Func<Stream, ValueTask<T>> createAction) =>
            zip.UnzipFirstEntryAsync(extension, (s, _) => createAction(s));
    }

}
