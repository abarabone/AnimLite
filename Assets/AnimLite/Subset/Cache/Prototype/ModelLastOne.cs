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
using System.Net.Http;
using System.IO.Compression;
using UnityEngine.Animations;

namespace AnimLite.Utility
{


    // 現状つかっていない
    // → 一番最初に作成された game object が破棄されたら、その派生のメッシュなど各種リソースが破棄されてしまうため
    // 　上記動作が prefab に対してなのか vrm load に対してなのか忘れた…


    ///// <summary>
    ///// 取得
    ///// ・最初にインスタンスを１つロードする
    ///// ・１つしかない場合は prototype を渡す
    ///// ・２つ目からは複製を渡す
    ///// ・prototype は list としてインスタンスをすべて保持
    ///// ・prototype の破棄されていないインスタンスを渡す/複製する
    ///// 破棄
    ///// ・インスタンスが常に１つ残るようにする
    ///// ・残りが１つなら destroy しない
    ///// </summary>
    //public class ModelLastOne : IPrototype<GameObject>
    //{
    //    public ModelLastOne(GameObject prototype)
    //    {
    //        this.instances.Add(prototype);
    //    }
    //    public ModelLastOne(GameObject prototype, PrototypeReleaseMode mode)
    //    {
    //        this.instances.Add(prototype);
    //        this.Mode = mode;
    //    }


    //    public PrototypeReleaseMode Mode { get; } = PrototypeReleaseMode.AutoRelease;

    //    int refCount = 0;

    //    ConcurrentBag<GameObject> instances = new();


    //    public async ValueTask<Instance<GameObject>> InstantiateAsync()
    //    {
    //        var inow = Interlocked.Increment(ref this.refCount);

    //        var go = inow > 1
    //            ? await instantiateAsync_(getPrototype_())
    //            : getPrototype_();

    //        //go.SetActive(true);

    //        return new Instance<GameObject>(go, this);


    //        async ValueTask<GameObject> instantiateAsync_(GameObject prototype)
    //        {
    //            await Awaitable.MainThreadAsync();
    //            var go = (await GameObject.InstantiateAsync(prototype))[0];

    //            this.instances.Add(go);

    //            return go;
    //        }

    //        GameObject getPrototype_()
    //        {
    //            foreach (var go in this.instances)
    //            {
    //                if (!go.IsUnityNull()) return go;
    //            }
    //            return null;
    //        }
    //    }

    //    public async ValueTask ReleaseWithDestroyAsync(GameObject go)
    //    {
    //        var inow = Interlocked.Decrement(ref this.refCount);

    //        if (inow > 0)
    //        {
    //            go.Destroy();
    //            return;
    //        }

    //        switch (this.Mode)
    //        {
    //            case PrototypeReleaseMode.AutoRelease:
    //                //go.Destroy();
    //                await this.DisposeAsync();
    //                break;

    //            case PrototypeReleaseMode.NoRelease:
    //                {
    //                    if (go.IsUnityNull()) break;

    //                    await Awaitable.MainThreadAsync();
    //                    go.SetActive(false);

    //                    var anim = go.GetComponent<Animator>().AsUnityNull();
    //                    anim?.UnbindAllStreamHandles();
    //                    anim?.ResetPose();
    //                }
    //                break;
    //        }
    //    }

    //    public async ValueTask DisposeAsync()
    //    {
    //        if (this.instances == null) return;

    //        await Awaitable.MainThreadAsync();
    //        this.instances.ForEach(x =>
    //        {
    //            x.AsUnityNull()?.Destroy();
    //        });
    //        this.instances = null;

    //        "Dispose async ModelLastOne".ShowDebugLog();
    //    }
    //    //public void Dispose()
    //    //{
    //    //    if (this.instances == null) return;

    //    //    this.instances.ForEach(x =>
    //    //    {
    //    //        x.AsUnityNull()?.Destroy();
    //    //    });
    //    //    this.instances = null;

    //    //    "Dispose ModelLastOne".ShowDebugLog();
    //    //}
    //}




}
