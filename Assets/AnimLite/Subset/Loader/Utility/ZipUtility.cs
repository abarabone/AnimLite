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
using UnityEngine.Scripting;// [Preserve] �̂���
using System.Net.Http;
using System.IO.Compression;
using AnimLite.Vmd;
using System.Text;
using AnimLite.Utility;

namespace AnimLite.Utility
{


    public class NoArchive : IArchive
    {
        public NoArchive(PathUnit jsonpath, IArchive fallback = null)
        {
            this.FallbackArchive = fallback;
            this.jsonfullpath = jsonpath;
        }

        PathUnit jsonfullpath;

        public IArchive FallbackArchive { get; private set; }

        public void Dispose()
        {
            if (this.jsonfullpath.Value is null) return;
            Debug.Log("no disposing");

            this.jsonfullpath.Value = null;
            this.FallbackArchive?.Dispose();
        }


        public T Extract<T>(PathUnit entryPath, Func<Stream, T> convertAction) =>
            entryPath.ToFullPath().OpenReadFileStream().Using(convertAction);

        public ValueTask<T> ExtractAsync<T>(PathUnit entryPath, Func<Stream, ValueTask<T>> convertAction) =>
            entryPath.ToFullPath().OpenReadFileStream().UsingAsync(convertAction);


        public T ExtractFirstEntry<T>(string extension, Func<Stream, T> convertAction) =>
            this.jsonfullpath.OpenReadFileStream().Using(convertAction);

        public ValueTask<T> ExtractFirstEntryAsync<T>(string extension, Func<Stream, ValueTask<T>> convertAction) =>
            this.jsonfullpath.OpenReadFileStream().UsingAsync(convertAction);
    }



    public class ZipDummyArchive : IArchive
    {
        public ZipDummyArchive(PathUnit archiveCachePath, IArchive fallback = null)
        {
            this.archiveCachePath = archiveCachePath;
            this.FallbackArchive = fallback;
        }

        PathUnit archiveCachePath;
        public IArchive fallbackArchive { private get; set; }


        public IArchive FallbackArchive { get; private set; }

        public void Dispose()
        {
            if (this.archiveCachePath.Value is null) return;
            Debug.Log("dummy disposing");

            this.archiveCachePath.Value = null;
            this.FallbackArchive?.Dispose();
        }


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
        public static async ValueTask<ZipDummyArchive> OpenDummyArchiveAsync(this PathUnit fullpath, IArchive fallback, CancellationToken ct)
        {
            var cachePath = fullpath.IsHttp()
                ? await fullpath.GetCachePathAsync(StreamOpenUtility.OpenStreamFileOrWebAsync, ct)
                : fullpath;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            $"dummy archive : {fullpath.Value} -> {cachePath.Value}".ShowDebugLog();
#endif
            return new ZipDummyArchive(cachePath, fallback);
        }
    }


    /// <summary>
    /// �e�� .Dispose() ����ƁAFallbackArchive �� .Dispose() �����B
    /// ������ Fallback ����Ă��鑤�� .Dispose() ���Ă����Ȃ��B
    /// </summary>
    public class ZipWrapArchive : IArchive
    {
        public ZipWrapArchive(Stream stream, IArchive fallback = null)
        {
            this.stream = stream;
            this.zip = new ZipArchive(stream, ZipArchiveMode.Read, false, LocalEncoding.sjis);
            this.FallbackArchive = fallback;
        }

        ZipArchive zip;
        Stream stream;


        public IArchive FallbackArchive { get; private set; }

        public void Dispose()
        {
            if (this.zip is null) return;
            Debug.Log("zip disposing");

            this.zip.Dispose();
            this.stream.Dispose();// archive ��j�����Ă��j������Ȃ��̂Łi������炵���̂Ɂj
            this.zip = null;
            this.stream = null;

            this.FallbackArchive?.Dispose();
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
        public static ZipWrapArchive OpenZipArchive(this Stream stream, IArchive fallback) =>
            new ZipWrapArchive(stream, fallback);

        public static async ValueTask<ZipWrapArchive> OpenZipArchiveAwait(this ValueTask<Stream> stream, IArchive fallback) =>
            new ZipWrapArchive(await stream, fallback);
    }




    public static class ZipUtility
    {

        // Archive �̂ق��ɂ����Ă����Ȃ����낤��


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


        // �o���G�[�V���������Ȃ��Ƃ����Ȃ��̂͂��ȁc
        // this Stream �� this ValueTask<Stream> ���߂�ǂ������A�킩��Â炢


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
