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
    /// </summary>
    public class ModelPrefab : IPrototype<GameObject>
    {
        public ModelPrefab(GameObject prototype)
        {
            this.prototype = prototype;
        }
        public ModelPrefab(GameObject prototype, PrototypeReleaseMode mode)
        {
            this.prototype = prototype;
            this.Mode = mode;
        }


        public PrototypeReleaseMode Mode { get; } = PrototypeReleaseMode.AutoRelease;

        GameObject prototype = null;

        int refCount = 0;


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
            await Awaitable.MainThreadAsync();
            var instance = GameObject.Instantiate(this.prototype);

            Interlocked.Increment(ref this.refCount);
            return new Instance<GameObject>(instance, this);
        }

        public async ValueTask ReleaseWithDestroyAsync(GameObject instance)
        {
            await instance.DestroyOnMainThreadAsync();

            var inow = Interlocked.Decrement(ref this.refCount);

            switch (this.Mode)
            {
                case PrototypeReleaseMode.AutoRelease:
                    if (inow > 0) break;
                    await this.DisposeAsync();
                    break;

                case PrototypeReleaseMode.NoRelease:
                    break;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (this.prototype == null) return;

            await this.prototype.ReleaseOnMainThreadAsync();
            this.prototype = null;

            "Dispose async ModelPrefab".ShowDebugLog();
        }
        //public void Dispose()
        //{
        //    if (this.prototype == null) return;

        //    this.prototype.Release();
        //    this.prototype = null;

        //    "Dispose ModelResource".ShowDebugLog();
        //}
    }




}
