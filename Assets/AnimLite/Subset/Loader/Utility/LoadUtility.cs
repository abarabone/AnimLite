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
                Debug.LogWarning(e);
                return default;
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                Debug.LogWarning(e);
                return default;
            }
            catch (System.InvalidOperationException e)
            {
                Debug.LogWarning(e);
                return default;
            }
        }
    }




    static public class StreamOpenUtility
    {

        // ReadAsync() などの非同期メソッドは、Task.Run() の非同期と同じらしい（ＧＵＩスレッドをブロックさせないなどの意味しかない）
        // だがこちらの方が圧倒的にはやい、なぜだろう…（ドキュメントには小さいファイルでは不利とはあったが）
        public static Stream OpenReadFileStream(this PathUnit path) =>
            new FileStream(path, FileMode.Open, FileAccess.Read);


        // ちゃんとした I/O の非同期になるが、ものによってはかなり遅くなるようだ
        public static Stream OpenAsyncReadFileStream(this PathUnit path)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"open file with async i/o mode: {path.Value}");
#endif
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        }


        public static async ValueTask<Stream> OpenStreamFileOrWebAsync(this PathUnit path, CancellationToken ct) =>
            path switch
            {
                var x when x.IsHttp() =>
                    await path.LoadFromWebAsync(ct),
                //var x when x.Value.EndsWith(".vrm", StringComparison.InvariantCultureIgnoreCase) || x.Value.EndsWith(".vmd", StringComparison.InvariantCultureIgnoreCase) =>
                var x when new FileInfo(x).Length >= 3 * 1024 * 1024 =>// サイズに根拠はないが 3MB とした
                    path.OpenAsyncReadFileStream(),
                _ =>
                    path.OpenReadFileStream(),
            };



        public static async ValueTask<Stream> OpenStreamFileOrWebOrAssetAsync<TAsset>(
            this PathUnit path, Func<TAsset, byte[]> toBytesAction, CancellationToken ct)
                where TAsset : UnityEngine.Object
        =>
            path.ToResourceName() switch
            {
                var resname when resname != "" =>   // リソースでは .zip をサポートしない
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


}
