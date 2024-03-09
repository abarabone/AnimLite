using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.VisualScripting;


namespace AnimLite.Utility
{

    using AnimLite.Utility.Linq;



    public class DisposableBag : IDisposable, IEnumerable<IDisposable>
    {

        List<IDisposable> disposables = new List<IDisposable>();


        public IDisposable this[int i] => this.disposables[i];

        public void Add(IDisposable item) => this.disposables.Add(item);

        public void Dispose() => this.disposables.ForEach(x => x.Dispose());


        public IEnumerator<IDisposable> GetEnumerator() => this.disposables.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.disposables.GetEnumerator();
    }

    public struct DisposableWrap<T> : IDisposable
    {
        Action<T> action;
        public T Valule { get; }
        public DisposableWrap(T src, Action<T> action)
        {
            this.Valule = src;
            this.action = action;
        }
        public void Dispose() => this.action(this.Valule);

        public static implicit operator T(DisposableWrap<T> src) => src.Valule;
    }


    public static class MathUtilityExtenstion
    {

        //public static float3 As3(this float4 v) => (float3)v;
        public static float3 As3(this float4 v) => new float3(v.x, v.y, v.z);

        public static float3 AsXZ(this float3 v) => new float3(v.x, 0, v.z);

        public static float3 As_float3(this float4 v) => new float3(v.x, v.y, v.z);

        public static quaternion As_quaternion(this float4 v) => v;

        public static float3 As_float3(this Vector3 v) => (float3)v;
        public static Vector3 AsVector3(this float3 v) => (Vector3)v;
        public static quaternion As_quaternion(this Quaternion q) => (quaternion)q;
        public static Quaternion AsQuaternion(this quaternion q) => (Quaternion)q;

    }


    public static class ObjectUtilityExtension
    {

        public static SkinnedMeshRenderer FindFaceRendererIfNothing(this Animator anim, SkinnedMeshRenderer r) =>
            r.IsUnityNull()
                ? anim.FindFaceRenderer()
                : r
            ;

        public static SkinnedMeshRenderer FindFaceRenderer(this Animator anim) =>
            anim.GetComponentsInChildren<SkinnedMeshRenderer>()
                .FirstOrDefault(r => r.sharedMesh.bindposeCount > 0);
        //{
        //    var tf = anim.transform;

        //    for (var i = 0; i<tf.childCount; i++)
        //    {
        //        var tfc = tf.GetChild(i);

        //        var r = tfc.GetComponent<SkinnedMeshRenderer>();
        //        if (r == null) continue;

        //        if (r.sharedMesh.blendShapeCount > 0) return r;
        //    }

        //    return null;
        //}


        /// <summary>
        /// ç≈ìKâªÇ≈è¡Ç¶ÇÈÇ±Ç∆Çä˙ë“Ç∑ÇÈÇ™É_ÉÅÇ©Ç‡
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ShowDebugLog(this string text)
        {
#if UNITY_EDITOR
            Debug.Log(text);
#endif
        }

    }


    public static class UtilityExtension
    {

        public static DisposableWrap<T> AsDisposable<T>(this T src, Action<T> action) => new DisposableWrap<T>(src, action);


        public static NativeArray<T> ToNativeArray<T>(this T[] src, Allocator allocator = Allocator.Persistent)
            where T : unmanaged =>
            new NativeArray<T>(src, allocator);

        public static NativeArray<T> ToNativeArray<T>(this IEnumerable<T> src, Allocator allocator = Allocator.Persistent)
            where T : unmanaged =>
            src.ToArray().ToNativeArray(allocator);




        /// <summary>
        /// 
        /// </summary>
        public static IEnumerable<TElement> TryGetOrBlank<TKey, TElement>(
            this ILookup<TKey, TElement> src, TKey key)
        {
            if (!src.Contains(key)) return new BlankEnumerableStruct<TElement>();

            return src[key];
        }

        /// <summary>
        /// 
        /// </summary>
        public static IEnumerable<TElement> TryGetOrBlank<TKey, TElement>(
            //this IDictionary<TKey, IEnumerable<TElement>> src, TKey key)
            this Dictionary<TKey, TElement[]> src, TKey key)
        {
            return src.TryGetOrDefault(key, new BlankEnumerableStruct<TElement>());
        }

        /// <summary>
        /// 
        /// </summary>
        public static TValue TryGetOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> src, TKey key, TValue defaultValue = default)
        {
            var res = src.TryGet(key);

            return res.isExists
                ? res.value
                : defaultValue
                ;
        }
        public static IEnumerable<TValue> TryGetOrDefault<TKey, TValue>(
            this Dictionary<TKey, TValue[]> src, TKey key, IEnumerable<TValue> defaultValue = default)
        {
            var res = src.TryGet(key);

            return res.isExists
                ? res.value
                : defaultValue
                ;
        }

        public static (bool isExists, TValue value) TryGet<TKey, TValue>(
            this IDictionary<TKey, TValue> dict, TKey key)
        {
            var isExists = dict.TryGetValue(key, out var value);

            return (isExists, value);
        }

        public static TValue GetOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default)
        {
            var isExists = dict.TryGetValue(key, out var value);

            return isExists ? value : defaultValue;
        }



    }
}



namespace AnimLite.Utility.Linq
{


    /// <summary>
    /// 
    /// </summary>
    public struct BlankEnumerableStruct<T> : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator() => new Enumerator();

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator();

        struct Enumerator : IEnumerator<T>
        {
            public T Current => default;

            object IEnumerator.Current => default;

            public void Dispose() { }

            public bool MoveNext() { return false; }
            public void Reset() { }
        }
    }


    public static class LinqUtilityExtenstion
    {

        public static IEnumerable<T> Do<T>(this IEnumerable<T> e, Action<T, int> f) =>
            e.Select((x, i) => { f(x, i); return x; })
            ;


        public static Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> src) => Task.WhenAll(src);
        //public static Awaitable<T[]> WhenAll<T>(this IEnumerable<Awaitable<T>> src) => Task.WhenAll(src);

        public static async Awaitable<T> ToAwaitable<T>(this Task<T> t) => await t;


        public static IEnumerable<T> Zip<T1, T2, T>(this (IEnumerable<T1> src1, IEnumerable<T2> src2) x, Func<T1, T2, T> f) =>
            Enumerable.Zip(x.src1, x.src2, f);

        public static IEnumerable<(T1, T2)> Zip<T1, T2>(this (IEnumerable<T1> src1, IEnumerable<T2> src2) x) =>
            (x.src1, x.src2).Zip((x, y) => (x, y));

    }
}
