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
