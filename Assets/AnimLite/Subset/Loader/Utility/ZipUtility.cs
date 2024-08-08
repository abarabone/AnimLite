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

namespace AnimLite.Utility
{



    public class ZipArchiveTakeStream : ZipArchive, IDisposable
    {
        public ZipArchiveTakeStream(Stream s)
            : base(s, ZipArchiveMode.Read, false, LocalEncoding.sjis) => this.stream = s;
        public Stream stream;

        public new void Dispose()
        {
            this.stream.Dispose();// archive を破棄しても破棄されないので（閉じられるらしいのに）
            base.Dispose();
        }
    }

    public static class ZipOpenUtility
    {

        public static async ValueTask<ZipArchive> OpenZipAsync(this ValueTask<Stream> stream)
        {
            return new ZipArchiveTakeStream(await stream);
        }

        public static ZipArchive OpenZip(this Stream stream)
        {
            return new ZipArchiveTakeStream(stream);
        }

    }



    public static class ZipUtility
    {




        public static async ValueTask<T> UnzipAsync<T>(
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



        public static async ValueTask<T> UnzipAsync<T>(
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


        public static async ValueTask<T> UnzipFirstEntryAsync<T>(
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


        public static async ValueTask<T> UnzipFirstEntryAsync<T>(
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


        public static ValueTask<T> UnzipFirstEntryAsync<T>(this ValueTask<Stream> stream, string extension, Func<Stream, T> createAction) =>
            stream.UnzipFirstEntryAsync(extension, (s, _) => createAction(s));

        public static T UnzipFirstEntry<T>(this Stream stream, string extension, Func<Stream, T> createAction) =>
            stream.UnzipFirstEntry(extension, (s, _) => createAction(s));


        public static ValueTask<T> UnzipFirstEntryAsync<T>(this ValueTask<Stream> stream, string extension, Func<Stream, ValueTask<T>> createAction) =>
            stream.UnzipFirstEntryAsync(extension, (s, _) => createAction(s));

        public static ValueTask<T> UnzipFirstEntryAsync<T>(this Stream stream, string extension, Func<Stream, ValueTask<T>> createAction) =>
            stream.UnzipFirstEntryAsync(extension, (s, _) => createAction(s));
    }





    public static class ZipArchiveUtility
    {


        public static async ValueTask<T> UnzipAsync<T>(
            this ValueTask<ZipArchive> zip, PathUnit entryPath, Func<Stream, T> createAction)
        =>
            (await zip).Unzip(entryPath, createAction);


        public static T Unzip<T>(
            this ZipArchive zip, PathUnit entryPath, Func<Stream, T> createAction)
        {
            //zip.Entries.ForEach(x => Debug.Log(x.FullName));
            //zip.Entries.ForEach(x => Debug.Log($"{x.FullName} {x.FullName.ToUtf8()}"));
            var entry = zip.GetEntry(entryPath);
            if (entry == null) return default;

            using var s = entry.Open();

            return createAction(s);
        }



        public static async ValueTask<T> UnzipAsync<T>(
            this ValueTask<ZipArchive> zip, PathUnit entryPath, Func<Stream, ValueTask<T>> createAction)
        =>
            await (await zip).UnzipAsync(entryPath, createAction);


        public static async ValueTask<T> UnzipAsync<T>(
            this ZipArchive zip, PathUnit entryPath, Func<Stream, ValueTask<T>> createAction)
        {
            //return await zip.Unzip(entryPath, createAction);
            //zip.Entries.ForEach(x => Debug.Log($"{x.FullName} {x.FullName.ToUtf8()}"));
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





        public static async ValueTask<T> UnzipFirstEntryAsync<T>(
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


        public static async ValueTask<T> UnzipFirstEntryAsync<T>(
            this ValueTask<ZipArchive> zip, string extension, Func<Stream, string, ValueTask<T>> createAction)
        =>
            await (await zip).UnzipFirstEntryAsync(extension, createAction);


        public static async ValueTask<T> UnzipFirstEntryAsync<T>(
            this ZipArchive zip, string extension, Func<Stream, string, ValueTask<T>> createAction)
        {
            var extlist = extension.Split(";");
            var entry = zip.Entries.FirstOrDefault(x => extlist.Where(ext => x.Name.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase)).Any());
            if (entry == null) return default;

            //Debug.Log(entry.Name);
            using var s = entry.Open();

            return await createAction(s, entry.Name);
        }



        public static ValueTask<T> UnzipFirstEntryAsync<T>(this ValueTask<ZipArchive> zip, string extension, Func<Stream, T> createAction) =>
            zip.UnzipFirstEntryAsync(extension, (s, _) => createAction(s));

        public static T UnzipFirstEntry<T>(this ZipArchive zip, string extension, Func<Stream, T> createAction) =>
            zip.UnzipFirstEntry(extension, (s, _) => createAction(s));


        public static ValueTask<T> UnzipFirstEntryAsync<T>(this ValueTask<ZipArchive> zip, string extension, Func<Stream, ValueTask<T>> createAction) =>
            zip.UnzipFirstEntryAsync(extension, (s, _) => createAction(s));

        public static ValueTask<T> UnzipFirstEntryAsync<T>(this ZipArchive zip, string extension, Func<Stream, ValueTask<T>> createAction) =>
            zip.UnzipFirstEntryAsync(extension, (s, _) => createAction(s));
    }

}
