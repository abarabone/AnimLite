

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

    static public class LoadErr
    {
        public static async ValueTask<T> LoggingAsync<T>(Func<ValueTask<T>> action)
        {
            try
            {
                return await action();
            }
            catch (System.IO.FileNotFoundException e)
            {
                e.showWarning();
                return default;
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                e.showWarning();
                return default;
            }
            catch (UnityEngine.AddressableAssets.InvalidKeyException e)
            // なぜかこれキャッチできたためしがない、コンソールに常にエラーで表示されてしまう
            // しかもそのまま先に進んでる気がする、ほんとに例外でてんのこれ？
            {
                e.showWarning();
                return default;
            }
            catch (System.InvalidOperationException e)
            {
                e.showWarning();
                return default;
            }
            catch (ArgumentNullException e)
            // リソースロード失敗時に stream が null で発生。stream を受ける関数ではじきたいけどまあいいか
            {
                e.showWarning();
                return default;
            }
            //catch (Exception e)
            //{
            //    Debug.LogWarning(e);
            //    return default;
            //}
        }
        public static T Logging<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            catch (System.IO.FileNotFoundException e)
            {
                e.showWarning();
                return default;
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                e.showWarning();
                return default;
            }
            catch (UnityEngine.AddressableAssets.InvalidKeyException e)
            // なぜかこれキャッチできたためしがない、コンソールに常にエラーで表示されてしまう
            // しかもそのまま先に進んでる気がする、ほんとに例外でてんのこれ？
            {
                e.showWarning();
                return default;
            }
            catch (System.InvalidOperationException e)
            {
                e.showWarning();
                return default;
            }
            catch (ArgumentNullException e)
            // リソースロード失敗時に stream が null で発生。stream を受ける関数ではじきたいけどまあいいか
            {
                e.showWarning();
                return default;
            }
            //catch (Exception e)
            //{
            //    Debug.LogWarning(e);
            //    return default;
            //}
        }

        public static void showWarning(this Exception e)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning(e);
#endif
        }
    }






    static public class StreamOpenUtility
    {

        public static Stream OpenWriteFileStream(this PathUnit fullpath) =>
            new FileStream(fullpath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: false);




        // ReadAsync() などの非同期メソッドは、Task.Run() の非同期と同じらしい（ＧＵＩスレッドをブロックさせないなどの意味しかない）
        // だがこちらの方が圧倒的にはやい、なぜだろう…（ドキュメントには小さいファイルでは不利とはあったが）
        public static Stream OpenReadFileStream(this PathUnit fullpath) =>
                //new FileStream(path, FileMode.Open, FileAccess.Read);
                new FileStream(fullpath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: false);


        // ちゃんとした I/O の非同期になるが、ものによってはかなり遅くなるようだ
        public static Stream OpenAsyncReadFileStream(this PathUnit fullpath)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"open file with async i/o mode: {fullpath.Value}");
#endif
            return new FileStream(fullpath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        }


        public static Stream OpenReadStream(this PathUnit path) =>
            DanceSceneLoader.UseAsyncModeForFileStreamApi
            &&
            new FileInfo(path).Length >= 3 * 1024 * 1024// サイズに根拠はないが 3MB とした
                ? path.OpenAsyncReadFileStream()
                : path.OpenReadFileStream();
        //やっぱり非同期読み込みにすると超重くなる気がする



        public static async ValueTask<Stream> OpenStreamFileOrWebAsync(this PathUnit fullpath, CancellationToken ct) =>
            fullpath switch
            {
                var x when x.IsHttp() =>
                    //await fullpath.LoadFromWebAsync(ct),
                    await fullpath.GetCachePathAsync(WebLoaderUtility.LoadFromWebAsync, ct).Await(OpenReadStream),
                _ =>
                    fullpath.OpenReadStream(),
            };



        public static async ValueTask<Stream> OpenStreamFileOrWebOrAssetAsync<TAsset>(
            this PathUnit fullpath, Func<TAsset, byte[]> toBytesAction, CancellationToken ct)
                where TAsset : UnityEngine.Object
        =>
            fullpath.ToResourceName() switch
            {
                var resname when resname != "" =>   // リソースでは .zip をサポートしない
                    await resname.LoadResourceToStreamAsync<TAsset>(toBytesAction, ct),
                _ when fullpath.IsHttp() =>
                    //await fullpath.LoadFromWebAsync(ct),
                    await fullpath.GetCachePathAsync(WebLoaderUtility.LoadFromWebAsync, ct).Await(OpenReadStream),
                _ =>
                    fullpath.OpenReadStream(),
            };



    }


    static public class DisposeUtility
    {

        public static T Using<T>(this Stream s, Func<Stream, T> action)
        {
            using (s)
            {
                return action(s);
            }
        }


        public static async ValueTask<T> UsingAwait<T>(this ValueTask<Stream> s, Func<Stream, T> action) =>
            await (await s).UsingAsync(action);


        public static async ValueTask<T> UsingAsync<T>(this Stream s, Func<Stream, T> action)
        {
            await using (s)
            {
                return action(s);
            }
        }

        //public static async ValueTask<T> UsingAwait<T, U>(this ValueTask<Stream> s, U arg, Func<Stream, U, T> action) =>
        //    (await s).Using(arg, action);


        //public static T Using<T, U>(this Stream s, U arg, Func<Stream, U, T> action)
        //{
        //    using (s)
        //    {
        //        return action(s, arg);
        //    }
        //}


        public static async ValueTask<T> UsingAwait<T>(this ValueTask<Stream> s, Func<Stream, ValueTask<T>> action) =>
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


}
