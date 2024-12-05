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
    public class Resource<T> : IPrototype<T>
        where T : UnityEngine.Object
    {
        public Resource(T prototype)
        {
            this.prototype = prototype;
        }
        public Resource(T prototype, PrototypeReleaseMode mode)
        {
            this.prototype = prototype;
            this.Mode = mode;
        }


        public PrototypeReleaseMode Mode { get; } = PrototypeReleaseMode.AutoRelease;

        T prototype = null;

        int refCount = 0;


        public ValueTask<Instance<T>> InstantiateAsync()
        {
            Interlocked.Increment(ref this.refCount);
            return new ValueTask<Instance<T>>(new Instance<T>(this.prototype, this));
        }

        public async ValueTask ReleaseWithDestroyAsync(T instance)
        {
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

            "Dispose async Resource".ShowDebugLog();
        }
        //public void Dispose()
        //{
        //    if (this.prototype == null) return;

        //    this.prototype.Release();
        //    this.prototype = null;

        //    "Dispose Resource".ShowDebugLog();
        //}
    }




}
