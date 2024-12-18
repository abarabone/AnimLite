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
    /// ・prototype にリソースを保持
    /// ・リソースもカウント対象
    /// ・リソースおよびインスタンス参照が 0 になったらリリース
    /// </summary>
    public class Resource<T> : IPrototype<T>
        where T : UnityEngine.Object
    {
        public Resource(T prototype)
        {
            this.prototype = prototype;
        }


        T prototype;

        int isReleased = 0;

        int refCount = 1;
        

        public ValueTask<Instance<T>> InstantiateAsync()
        {
            if (this.prototype.AsUnityNull() is null) return new ValueTask<Instance<T>>();

            Interlocked.Increment(ref this.refCount);

            var i = new Instance<T>(this.prototype, this);
            return new ValueTask<Instance<T>>(i);
        }

        public async ValueTask ReleaseWithDestroyAsync(T instance)
        {
            if (instance.AsUnityNull() is null) return;

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
            var iDecremented = Interlocked.Decrement(ref this.refCount);
            if (iDecremented > 0) return;

            await this.prototype.ReleaseOnMainThreadAsync();
            this.prototype = null;

            "Dispose async Resource".ShowDebugLog();
        }
    }




}
