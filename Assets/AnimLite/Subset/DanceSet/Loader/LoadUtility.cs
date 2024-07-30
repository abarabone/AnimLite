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

namespace AnimLite.Utility
{

    static public class StreamOpenUtility
    {


        public static Stream OpenReadFileStream(this PathUnit path) =>
            new FileStream(path, FileMode.Open, FileAccess.Read);



        public static async ValueTask<Stream> OpenStreamFileOrWebAsync(this PathUnit path, CancellationToken ct) =>
            path switch
            {
                var x when x.IsHttp() =>
                    await path.LoadFromWebAsync(ct),
                _ =>
                    path.OpenReadFileStream(),
            };


        public static async ValueTask<Stream> OpenStreamFileOrWebOrAssetAsync<TAsset>(
            this PathUnit path, Func<TAsset, byte[]> toBytesAction, CancellationToken ct)
                where TAsset : UnityEngine.Object
        =>
            path.ToResourceName() switch
            {
                var resname when resname != "" =>   // ���\�[�X�ł� .zip ���T�|�[�g���Ȃ�
                    await resname.LoadResourceToStreamAsync<TAsset>(toBytesAction, ct),
                _ when path.IsHttp() =>
                    await path.LoadFromWebAsync(ct),
                _ =>
                    path.OpenReadFileStream(),
            };

}


    static public class DisposeUtility
    {


        public static async ValueTask<T> UsingAsync<T>(this ValueTask<Stream> s, Func<Stream, T> action) =>
            await (await s).UsingAsync(action);


        public static async ValueTask<T> UsingAsync<T>(this Stream s, Func<Stream, T> action)
        {
            await using (s)
            {
                return action(s);
            }
        }


        public static async ValueTask<T> UsingAsync<T>(this ValueTask<Stream> s, Func<Stream, ValueTask<T>> action) =>
            await (await s).UsingAsync(action);


        public static async ValueTask<T> UsingAsync<T>(this Stream s, Func<Stream, ValueTask<T>> action)
        {
            await using (s)
            {
                return await action(s);
            }
        }

    }

    static public class ErrUtility
    {

        public static void ThrowIfCancellationRequested(this CancellationToken ct, Action disposeAction)
        {
            if (!ct.IsCancellationRequested) return;

            disposeAction();

            ct.ThrowIfCancellationRequested();
        }

        public static async ValueTask ThrowIfCancellationRequested(this CancellationToken ct, Func<ValueTask> disposeAction)
        {
            if (!ct.IsCancellationRequested) return;

            await disposeAction();

            ct.ThrowIfCancellationRequested();
        }

        public static async ValueTask ThrowIfCancellationRequested(this CancellationToken ct, Func<Task> disposeAction)
        {
            if (!ct.IsCancellationRequested) return;

            await disposeAction();

            ct.ThrowIfCancellationRequested();
        }

    }

    static public class ResourceUtility
    {

        public static Task<T> LoadAssetAsync<T>(this ResourceName name) where T : UnityEngine.Object =>
            Addressables.LoadAssetAsync<T>(name.Value).Task;

        public static void Release<T>(this T asset) where T : UnityEngine.Object =>
            Addressables.Release(asset);




        public static async ValueTask<TContent> LoadFromResourceAsync<TAsset, TContent>(
            this ResourceName name, Func<TAsset, TContent> createAction, CancellationToken ct)
                where TAsset : UnityEngine.Object
        {
            ct.ThrowIfCancellationRequested();

            await Awaitable.MainThreadAsync();
            using var asset = await name.LoadAssetAsync<TAsset>().AsDisposableAsync(x => x.Release());

            var content = createAction(asset);

            ct.ThrowIfCancellationRequested();
            return content;
        }



        public static async ValueTask<Stream> LoadResourceToStreamAsync<TAsset>(
            this ResourceName name, Func<TAsset, byte[]> getBytesAction, CancellationToken ct)
                where TAsset : UnityEngine.Object
        {
            ct.ThrowIfCancellationRequested();

            await Awaitable.MainThreadAsync();
            var asset = await name.LoadAssetAsync<TAsset>();

            await Awaitable.MainThreadAsync();
            ct.ThrowIfCancellationRequested(asset.Release);

            return new MemoryStreamTakenAsset<TAsset>(asset, getBytesAction(asset));
        }

        class MemoryStreamTakenAsset<TAsset> : MemoryStream, IDisposable, IAsyncDisposable
            where TAsset : UnityEngine.Object
        {
            public MemoryStreamTakenAsset(TAsset asset, byte[] bytes) : base(bytes)
            {
                this.asset = asset;
            }
            

            public TAsset asset;

            public new void Dispose()
            {
                base.Dispose();
                this.asset.Release();
            }
            public async new ValueTask DisposeAsync()
            {
                await Awaitable.MainThreadAsync();

                this.Dispose();
            }
        }

    }



    ///// <summary>
    ///// Addressable �Ń��[�h�������\�[�X�� .Dispose() �ŊǗ��ł���悤�ɂ���
    ///// </summary>
    ///// <typeparam name="T"></typeparam>
    //public struct DisposbleWrapperForAddressables<T> : IDisposable
    //    where T : UnityEngine.Object
    //{
    //    public T asset;

    //    public void Dispose() => Addressables.Release(this.asset);

    //    public static implicit operator T (DisposbleWrapperForAddressables<T> wrap) => wrap.asset;
    //}
    //public static class AddressableExtension
    //{
    //    public static DisposbleWrapperForAddressables<T> AsDisposable<T>(this T asset)
    //        where T : UnityEngine.Object
    //    =>
    //        new DisposbleWrapperForAddressables<T>
    //        {
    //            asset = asset,
    //        };

    //    // ���ׂ�����������D�悷��p
    //    public static async ValueTask<DisposbleWrapperForAddressables<T>> AsDisposableAsync<T>(this Task<T> assetAsync)
    //        where T : UnityEngine.Object
    //    {
    //        return (await assetAsync).AsDisposable();
    //    }
    //}






    // (Unity) Android �� HttpClient �ŒʐM����ƃC���^�[�l�b�g�����������ł��Ȃ����
    // https://ikorin2.hatenablog.jp/entry/2024/03/30/025946
    [Preserve]
    internal sealed class MarkerForInternet : UnityWebRequest { }
    // ��L�̃R�[�h���ǂ����ɏ����Ă����΁AUnityWebRequest ���g���Ă��锻��ɂȂ�A�����ŃC���^�[�l�b�g���������Ă����B
    // UnityWebRequest ���p�������N���X���`���Ă����āAPreserve �������g���� IL2CPP �ŏ����Ȃ��悤�ɂ����B



    public static class HttpLoader
    {
        static bool isCreated = false;

        readonly public static HttpClient Client;


        static HttpLoader()
        {
            Debug.Log("http client created");

            Client = new HttpClient();
            isCreated = true;
        }

        public static void Dispose()
        {
            if (isCreated) Client.Dispose();

            Debug.Log("http client disposed");
        }
    }

    public static class WebLoaderUtility
    {

        public static async ValueTask<Stream> LoadFromWebAsync(this PathUnit url, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var content = await HttpLoader.Client.GetByteArrayAsync(url);

            ct.ThrowIfCancellationRequested();

            return new MemoryStream(content);
        }

    }



    public class ZipArchiveTakeStream : ZipArchive, IDisposable
    {
        public ZipArchiveTakeStream(Stream s)
            : base(s, ZipArchiveMode.Read, false, LocalEncoding.sjis) => this.stream = s;
        public Stream stream;

        public new void Dispose()
        {
            this.stream.Dispose();// archive ��j�����Ă��j������Ȃ��̂Łi������炵���̂Ɂj
            base.Dispose();
        }
    }

    public static class ZipUtility
    {

        public static async ValueTask<ZipArchive> OpenZipAsync(this ValueTask<Stream> stream)
        {
            return new ZipArchiveTakeStream(await stream);
        }

        public static ZipArchive OpenZip(this Stream stream)
        {
            return new ZipArchiveTakeStream(stream);
        }





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


        // �o���G�[�V���������Ȃ��Ƃ����Ȃ��̂͂��ȁc
        // this Stream �� this ValueTask<Stream> ���߂�ǂ������A�킩��Â炢


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
            zip.Entries.ForEach(x => Debug.Log($"{x.FullName} {x.FullName.ToUtf8()}"));
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
            zip.Entries.ForEach(x => Debug.Log($"{x.FullName} {x.FullName.ToUtf8()}"));
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
            var entry = zip.Entries.FirstOrDefault(x => extlist.Where(ext => x.Name.EndsWith(ext)).Any());
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
            var entry = zip.Entries.FirstOrDefault(x => extlist.Where(ext => x.Name.EndsWith(ext)).Any());
            if (entry == null) return default;

            Debug.Log(entry.Name);
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
