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


namespace AnimLite.Utility
{

    using AnimLite.Utility.Linq;


    public enum FullPathMode
    {
        ForceDirectPath,
        DataPath,
        PersistentDataPath,
    }

    [Serializable]
    public struct PathUnit
    {

        public string Value;


        public PathUnit(string path) => this.Value = path;

        public static implicit operator string(PathUnit path) => path.Value;
        public static implicit operator PathUnit(string path) => new PathUnit(path);

        static public string ParentPath { get; private set; } = Application.dataPath;
        static public FullPathMode mode
        {
            set
            {
                PathUnit.ParentPath = value switch
                {
                    FullPathMode.PersistentDataPath => Application.persistentDataPath,
                    FullPathMode.DataPath => Application.dataPath,
                    _ => "",
                };
            }
        }
    }


    public static class PathUtilityExtension
    {

        public static PathUnit ToFullPath(this PathUnit path, string parentpath) =>
            path.Value == default || path.Value == "" || path.Value[1..3].Contains(':') || path.Value[0] == '/'
                ? (path.Value ?? "")
                : $"{parentpath}/{path.Value}"
            ;

        public static PathUnit ToFullPath(this PathUnit path) =>
            path.ToFullPath(PathUnit.ParentPath).show_(path);


        public static PathUnit ToPath(this string path) =>
            new PathUnit(path);

        static PathUnit show_(this PathUnit fullpath, PathUnit path)
        {
            //#if UNITY_EDITOR
            //    Debug.Log($"{path.Value} => {fullpath.Value}");
            //#endif
            return fullpath;
        }
    }



    //public struct FullPath
    //{
    //    public string fullpath;

    //    static public FullPathMode mode = FullPathMode.DataPath;

    //    public FullPath(string path) =>
    //        this.fullpath = mode switch
    //        {
    //            FullPathMode.PersistentDataPath => path == default || path[1..3].Contains(':') || path[0] == '/'
    //                ? path
    //                : Application.persistentDataPath + "/" + path,

    //            FullPathMode.DataPath => path == default || path[1..3].Contains(':') || path[0] == '/'
    //                ? path
    //                : Application.dataPath + "/../" + path,

    //            _ => path,
    //        };

    //    public static implicit operator string(FullPath path) => path.fullpath;

    //    public static implicit operator FullPath(string path) => show_(path, new FullPath(path));

    //    static FullPath show_(string path, FullPath fullpath)
    //    {
    //        Debug.Log($"{path} => {fullpath.fullpath}");

    //        return fullpath;
    //    }
    //}


    public class DisposableBag : IDisposable, IEnumerable<IDisposable>
    {

        List<IDisposable> disposables = new List<IDisposable>();


        public IDisposable this[int i] =>
            this.disposables[i];

        public void Add(IDisposable item) =>
            this.disposables.Add(item);

        public void Dispose() =>
            this.disposables.ForEach(x => x.Dispose());


        public IEnumerator<IDisposable> GetEnumerator() =>
            this.disposables.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.disposables.GetEnumerator();
    }
    public static class DisposableExtension
    {
        public static T AddTo<T>(this T disposable, DisposableBag disposables)
            where T : IDisposable
        {
            disposables.Add(disposable);
            return disposable;
        }
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


    /// <summary>
    /// セマフォの解放を using で行う
    /// ・セマフォの破棄は別に行う
    /// </summary>
    public struct DisposableSemapho : IDisposable
    {
        SemaphoreSlim semapho;

        public DisposableSemapho(SemaphoreSlim semapho) =>
            this.semapho = semapho;

        public async Task WaitAsync(CancellationToken ct)
        {
            "semapho on".ShowDebugLog();
            await this.semapho.WaitAsync(ct);
        }

        public void Dispose()
        {
            this.semapho.Release();
            "semapho off".ShowDebugLog();
        }

        //public async Task WaitAsync() =>
        //    await this.semapho.WaitAsync();

        //public void Dispose() =>
        //    this.semapho.Release();
    }
    public static class SemaphoExtension
    {
        public static async Task<DisposableSemapho> WaitAsyncDisposable(this SemaphoreSlim ss, CancellationToken ct)
        {
            var ds = new DisposableSemapho(ss);

            await ds.WaitAsync(ct);
            
            return ds;
        }
    }

    //public class ProgressState : IDisposable
    //{
    //    TaskCompletionSource<bool> tcs;

    //    public bool IsInProgress => !this.tcs?.Task.IsCompleted ?? false;


    //    public ProgressState Start()
    //    {
    //        //Debug.Log("start");
    //        this.tcs = new TaskCompletionSource<bool>();

    //        return this;
    //    }

    //    public async Awaitable WaitForCompleteAsync()
    //    {
    //        //Debug.Log("wait on");
    //        await this.tcs?.Task;
    //        Debug.Log("wait off");
    //    }

    //    public void Dispose()
    //    {
    //        //Debug.Log("end");
    //        this.tcs.SetResult(true);
    //    }
    //}



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
        /// 最適化で消えることを期待するがダメかも
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ShowDebugLog(this string text)
        {
            #if UNITY_EDITOR
                Debug.Log(text);
            #endif
            return text;
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


        public static async Awaitable<T[]> AwaitAllAsync<T>(
            this IEnumerable<Awaitable<T>> src, Func<T, bool> criteria = default)
        {
            var dst = new List<T>();
            foreach (var e in src)
            {
                var t = await e;
                if (!criteria?.Invoke(t) ?? false) continue;
                dst.Add(t);
            }
            return dst.ToArray();
        }

        public static Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> src) =>
            Task.WhenAll(src);
        //public static Awaitable<T[]> WhenAll<T>(this IEnumerable<Awaitable<T>> src) =>
        //    Task.WhenAll(src);

        public static async Awaitable<T> ToAwaitable<T>(this Task<T> t) =>
            await t;


        public static IEnumerable<T> Zip<T1, T2, T>(this (IEnumerable<T1> src1, IEnumerable<T2> src2) x, Func<T1, T2, T> f) =>
            Enumerable.Zip(x.src1, x.src2, f);

        public static IEnumerable<(T1, T2)> Zip<T1, T2>(this (IEnumerable<T1> src1, IEnumerable<T2> src2) x) =>
            (x.src1, x.src2).Zip((x, y) => (x, y));

    }
}
