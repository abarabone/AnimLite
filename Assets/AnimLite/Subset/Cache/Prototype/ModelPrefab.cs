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
    /// ・prototype にプレハブを保持
    /// ・プレハブもカウント対象
    /// ・プレハブおよびインスタンス参照が 0 になったらリリース
    /// </summary>
    public class ModelPrefab : IPrototype<GameObject>
    {
        public ModelPrefab(GameObject prototype)
        {
            this.prototype = prototype;
        }


        GameObject prototype = null;

        int isReleased = 0;

        int refCount = 1;


        // vrm の複製は非同期でやるとスプリングボーンが動かないみたいなので、現状は同期でやる
        //public async ValueTask<Instance<GameObject>> InstantiateAsync()
        //{
        //    await Awaitable.MainThreadAsync();
        //    var instance = await GameObject.InstantiateAsync(this.prototype);

        //    Interlocked.Increment(ref this.refCount);
        //    return new Instance<GameObject>(instance[0], this);
        //}
        public async ValueTask<Instance<GameObject>> InstantiateAsync()
        {
            if (this.prototype is null) return null;

            await Awaitable.MainThreadAsync();
            var instance = GameObject.Instantiate(this.prototype);

            Interlocked.Increment(ref this.refCount);
            return new Instance<GameObject>(instance, this);
        }

        public async ValueTask ReleaseWithDestroyAsync(GameObject instance)
        {
            if (instance.IsUnityNull()) return;
            await instance.DestroyOnMainThreadAsync();

            await this._disposeAsync();
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
            if (inow > 0) return;

            await this.prototype.ReleaseOnMainThreadAsync();
            this.prototype = null;

            "Dispose async ModelPrefab".ShowDebugLog();
        }
    }




}
