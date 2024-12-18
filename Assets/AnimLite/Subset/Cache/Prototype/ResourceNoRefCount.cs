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
    public class ResourceNoRefCount<T> : IPrototype<T>
        where T : UnityEngine.Object
    {
        public ResourceNoRefCount(T prototype)
        {
            this.prototype = prototype;
        }


        T prototype;



        public ValueTask<Instance<T>> InstantiateAsync()
        {
            if (this.prototype is null) return new ValueTask<Instance<T>>();

            var i = new Instance<T>(this.prototype, this);
            return new ValueTask<Instance<T>>(i);
        }

        public ValueTask ReleaseWithDestroyAsync(T instance)
        {
            return new ValueTask();
        }


        public ValueTask DisposeAsync()
        {
            if (this.prototype is null) return new ValueTask();

            return this._disposeAsync();
        }

        async ValueTask _disposeAsync()
        {
            await this.prototype.ReleaseOnMainThreadAsync();
            this.prototype = null;

            "Dispose async ResourceDriven".ShowDebugLog();
        }
    }




}
