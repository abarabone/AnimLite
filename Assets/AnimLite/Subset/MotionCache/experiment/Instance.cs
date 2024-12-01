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



    public class Instance<T> : IAsyncDisposable
        where T : UnityEngine.Object
    {
        public Instance(T instance, IPrototype<T> prototype)
        {
            this.Value = instance;
            this.Prototype = prototype;
        }

        public T Value { private set; get; }

        public IPrototype<T> Prototype { set; private get; }


        public ValueTask DisposeAsync() =>
            this.Prototype.ReleaseWithDestroyAsync(this.Value);


        public static implicit operator T(Instance<T> src) => src.Value;
    }





    public static class InstanceExtension
    {

        public static bool IsUnityNull<T>(this Instance<T> obj)
            where T : UnityEngine.Object
        {
            return obj?.Value.IsUnityNull() ?? true;
        }

        public static T AsUnityNull<T>(this Instance<T> obj)
            where T : UnityEngine.Object
        {
            return obj?.Value.AsUnityNull();
        }

        public static T AsUnityNull<T>(this Instance<T> obj, Func<T, bool?> criteria)
            where T : UnityEngine.Object
        {
            var _obj = obj?.Value.AsUnityNull();
            return criteria(_obj) ?? false
                ? _obj
                : null;
        }
    }






}
