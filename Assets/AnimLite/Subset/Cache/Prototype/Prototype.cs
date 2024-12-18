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


    public static class PrototypeUtility
    {
        public static Prototype<T> ToPrototype<T>(this T src) where T : class => new Prototype<T>(src);

    }







    public class Prototype<T> : Prototype<T, T>
        where T : class
    {
        public Prototype(T prototype) : base(prototype)
        { }
    }




    /// <summary>
    /// ・prototype にリソースを保持
    /// ・リソースもカウント対象
    /// ・リソースおよびインスタンス参照が 0 になったらリリース
    /// </summary>
    public class Prototype<TOrigin, TInstance> : IPrototype<TInstance>
        //where T : UnityEngine.Object
        where TOrigin : class
        where TInstance : class
    {
        public Prototype(TOrigin prototype)
        {
            this.prototype = prototype;
        }


        TOrigin prototype;

        public Func<TOrigin, ValueTask<TInstance>> InstantiateActionAsync = x => new ValueTask<TInstance>(x as TInstance);
        public Func<TInstance, ValueTask> DisposeInstanceActionAsync = instance => new ValueTask();
        public Func<TOrigin, ValueTask> DisposeActionAsync = origin => new ValueTask();

        int isReleased = 0;
        int refCount = 1;
        

        public async ValueTask<Instance<TInstance>> InstantiateAsync()
        {
            if (this.prototype is null) return null;

            Interlocked.Increment(ref this.refCount);

            var instance = await this.InstantiateActionAsync(this.prototype);
            return new Instance<TInstance>(instance, this);
        }

        public async ValueTask ReleaseWithDestroyAsync(TInstance instance)
        {
            if (instance is null) return;

            await this.DisposeInstanceActionAsync(instance);

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

            await this.DisposeActionAsync(this.prototype);
            this.prototype = null;

            $"Dispose async Prototype {typeof(TOrigin)}, {typeof(TInstance)}".ShowDebugLog();
        }
    }




}
