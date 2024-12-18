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


    /// <summary>
    /// インスタンスをひな型にして複製する
    /// vrm だとひな型を destroy するとリソースも消えるので、半分プレハブのように扱う必要がある
    /// 取得
    /// ・最初にインスタンスを１つロードし、非アクティブ化する
    /// ・そのインスタンスを本体とし、prototype とする
    /// ・prototype が空いていればインスタンスとして使用する
    /// ・prototype が使用中なら複製を渡す
    /// 破棄
    /// ・複製なら destory
    /// ・prototype なら非アクティブ化
    /// </summary>
    public class ModelOrigin : IPrototype<GameObject>
    {
        public ModelOrigin(GameObject prototype)
        {
            this.prototype = prototype;
        }


        int refCount = 1;
        int prototypeUsed = 0;
        int isReleased = 0;

        GameObject prototype = null;


        public async ValueTask<Instance<GameObject>> InstantiateAsync()
        {
            if (this.prototype is null) return null;

            Interlocked.Increment(ref this.refCount);

            var prevProtoUsed = Interlocked.CompareExchange(ref this.prototypeUsed, 1, 0);
            var go = prevProtoUsed == 0
                ? this.prototype
                : await instantateAsync_();

            return new Instance<GameObject>(go, this);

            async ValueTask<GameObject> instantateAsync_()
            {
                await Awaitable.MainThreadAsync();
                //return(await GameObject.InstantiateAsync(this.prototype))[0];// vrm の場合、非同期だと問題あるみたい
                return GameObject.Instantiate(this.prototype);
            }
        }

        public async ValueTask ReleaseWithDestroyAsync(GameObject go)
        {
            if (go.IsUnityNull()) return;
            await releaseObjectAsync_();

            await this._disposeAsync();

            return;


            ValueTask releaseObjectAsync_()
            {
                if (go == this.prototype)
                {
                    return deactivateAsync_();
                }
                else
                {
                    return destroyAsync_();
                }

                async ValueTask deactivateAsync_()
                {
                    var prevProtoUsed = Interlocked.CompareExchange(ref this.prototypeUsed, 0, 1);
                    if (prevProtoUsed == 0) return;

                    if (go.IsUnityNull()) return;
                    await Awaitable.MainThreadAsync();

                    go.SetActive(false);

                    var anim = go.GetComponent<Animator>().AsUnityNull();
                    anim?.UnbindAllStreamHandles();
                    anim?.ResetPose();
                }
                async ValueTask destroyAsync_()
                {
                    if (go.IsUnityNull()) return;

                    await Awaitable.MainThreadAsync();

                    go.Destroy();
                }
            }
        }

        public ValueTask DisposeAsync()
        {
            var isReleasedPrev = Interlocked.CompareExchange(ref this.isReleased, 1, 0);
            if (isReleasedPrev > 0) return new ValueTask();

            return this._disposeAsync();
        }

        async ValueTask _disposeAsync()
        {
            var inow = Interlocked.Decrement(ref this.refCount);
            //$"model origin {this.refCount}".ShowDebugLog();
            if (inow > 0) return;

            await this.prototype.DestroyOnMainThreadAsync();
            this.prototype = null;

            //$"Dispose async ModelOrigin {this.prototype.AsUnityNull()?.name}/{this.prototype?.GetType()?.Name}".ShowDebugLog();
            $"Dispose async ModelOrigin".ShowDebugLog();
        }
    }


}
