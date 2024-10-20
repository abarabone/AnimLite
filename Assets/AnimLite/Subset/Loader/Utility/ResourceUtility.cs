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
            var asset = await name.LoadAssetAsync<TAsset>();//.AsDisposableAsync(x => x?.Release());
            if (asset.IsUnityNull()) return default;

            using var wrapped = asset.AsDisposable(x => x.Release());
            var content = createAction(asset);

            ct.ThrowIfCancellationRequested();
            await Awaitable.MainThreadAsync();
            return content;
        }



        public static async ValueTask<Stream> LoadResourceToStreamAsync<TAsset>(
            this ResourceName name, Func<TAsset, byte[]> getBytesAction, CancellationToken ct)
                where TAsset : UnityEngine.Object
        {
            ct.ThrowIfCancellationRequested();

            await Awaitable.MainThreadAsync();
            //var asset = await LoadErr.LoggingAsync<TAsset>(() => name.LoadAssetAsync<TAsset>().AsValueTask());
            var asset = await name.LoadAssetAsync<TAsset>();
            ////var asset = default(TAsset);
            if (asset.IsUnityNull()) return null;

            await Awaitable.MainThreadAsync();
            ct.ThrowIfCancellationRequested(() => asset.Release());

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
    ///// Addressable でロードしたリソースを .Dispose() で管理できるようにする
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

    //    // 負荷よりも書き味を優先する用
    //    public static async ValueTask<DisposbleWrapperForAddressables<T>> AsDisposableAsync<T>(this Task<T> assetAsync)
    //        where T : UnityEngine.Object
    //    {
    //        return (await assetAsync).AsDisposable();
    //    }
    //}


}
