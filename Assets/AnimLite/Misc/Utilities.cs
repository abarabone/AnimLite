using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Data.Common;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.VisualScripting;

namespace AnimLite.Utility
{

    using AnimLite.Utility.Linq;



    public static class LocalEncoding
    {
        public static Encoding sjis = Encoding.GetEncoding("shift_jis");
    }

    public static class EncodeUtility
    {

        public static string ToUtf8(this string src)
        {
            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var sjis = LocalEncoding.sjis;
            var utf8 = Encoding.UTF8;

            var srcbytes = sjis.GetBytes(src);
            var dstbytes = Encoding.Convert(sjis, utf8, srcbytes);

            var res = utf8.GetString(dstbytes);
            return res;
        }

    }




    public enum FullPathMode
    {
        ForceDirectPath,
        DataPath,
        PersistentDataPath,
    }

    [Serializable]
    public struct PathUnit : IEquatable<PathUnit>
    {

        public string Value;// { get; private set; }


        public PathUnit(string path) => this.Value = path;

        public static implicit operator string(PathUnit path) => path.Value;
        public static implicit operator PathUnit(string path) => new PathUnit(path);

        /// <summary>
        /// .ToPath() で付加される親パスを指定する。
        /// デフォルトは Application.dataPath 
        /// </summary>
        static public string ParentPath { get; private set; } = Application.dataPath;
        /// <summary>
        /// フルパスモードをセットすると、ParentPath が変化する。
        /// </summary>
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


        // dictionary 用 boxing 回避 ------------------------------------
        public override bool Equals(object obj)
        {
            return obj is PathUnit unit && Equals(unit);
        }

        public bool Equals(PathUnit other)
        {
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }
        // dictionary 用 boxing 回避 ------------------------------------
    }

    public struct ResourceName
    {
        public string Value { get; private set; }

        public ResourceName(string name) => this.Value = name;

        public static implicit operator string(ResourceName res) => res.Value;
    }


    public static class PathUtilityExtension
    {

        /// <summary>
        /// 単純に文字列を PathUnit で包んで返す。
        /// </summary>
        public static PathUnit ToPath(this string path) =>
            new PathUnit(path);



        /// <summary>
        /// フルパスでなければ parentpath を付加して返す。null は "" を返す。
        /// </summary>
        public static PathUnit ToFullPath(this PathUnit path, string parentpath) =>
            path.IsFullPath() || path.IsBlank()
                ? (path.Value ?? "")
                : $"{parentpath}/{path.Value}"
            ;

        /// <summary>
        /// フルパスでなければ PathUnit.ParentPath を付加して返す。null は "" を返す。
        /// </summary>
        public static PathUnit ToFullPath(this PathUnit path) =>
            path.ToFullPath(PathUnit.ParentPath)
            .show_(path)
            ;

        //public static PathUnit ToFullPath(this PathUnit path, ZipArchive archive) =>
        //    archive == null
        //        ? path.ToFullPath(PathUnit.ParentPath)
        //        : path
        //    .show_(path)
        //    ;


        /// <summary>
        /// フルパスなら true を返す。
        /// フルパスは ドライブレター/ＵＲＬスキームから始まる（１～７文字目までに : を含む）, / から始まる, as resource で終わる
        /// </summary>
        public static bool IsFullPath(this PathUnit path) =>
            //path.IsBlank()
            //||
            path.Value[1..6].Contains(':')// ドライブレター、ＵＲＩスキーム
            ||
            path.Value[0] == '/'
            ||
            path.IsResource()
            ;




        public static bool IsHttp(this PathUnit path) =>
            path.Value.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase)
            ||
            path.Value.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase);



        public static bool IsResource(this PathUnit path) =>
            path.Value.EndsWith("as resource", StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        /// as resource が付加されていれば除去した名前を、そうでなければ "" を返す。
        /// </summary>
        public static ResourceName ToResourceName(this PathUnit path) =>
            path.IsResource()
                ? new ResourceName(path.Value[0..^("as resource".Length)].TrimEnd())
                : new ResourceName("");


        public static bool IsZip(this PathUnit path) =>
            path.Value.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase);

        public static bool IsZipEntry(this PathUnit path) =>
            path.Value.Contains(".zip/", StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        /// パスを .zip/ で分割し、.zip までと / より後ろを返す。
        /// zip でなければ ("", "") を返す。
        /// </summary>
        public static (PathUnit zipPath, PathUnit entryPath) DividZipAndEntry(this PathUnit path)
        {
            var i = path.Value.IndexOf(".zip/", StringComparison.InvariantCultureIgnoreCase);
            return (i >= 0) switch
            {
                true =>
                    divpath(),
                false when path.IsZip() => 
                    (path, ""),
                false =>
                    ("", ""),
            };
            
            (PathUnit, PathUnit) divpath()
            {
                var zippath = path.Value[0..(i + 4)];
                var entrypath = path.Value[(i + 5)..];
                return (zippath, entrypath);
            }
        }

        /// <summary>
        /// パスを .zip/ で分割し、/ より後ろを返す。
        /// zip でなければ "" を返す。
        /// </summary>
        public static PathUnit ToZipEntryPath(this PathUnit path)
        {
            var i = path.Value.IndexOf(".zip/", StringComparison.InvariantCultureIgnoreCase);
            if (i == -1) return "".ToPath();

            return path.Value[(i + 5)..];
        }



        static PathUnit show_(this PathUnit fullpath, PathUnit path)
        {
//#if UNITY_EDITOR
//            Debug.Log($"{path.Value} => {fullpath.Value}");
//#endif
            return fullpath;
        }



        static public bool IsBlank(this PathUnit path) =>
            (path.Value ?? "") == "";
            //path.Value == default || path.Value == "";


        //public static PathType DetectPathType(this PathUnit path)
        //{
        //    var isResource = path.IsResource();
        //    var isWeb = path.IsHttp();
        //    var isZip = 
        //}

    }




    //public enum PathType
    //{
    //    File,
    //    Web,
    //    Resource,
        
    //    ZipAsFile,
    //    ZipOnWeb,
    //    ZipInResource,

    //    ZipEntryInFile,
    //    ZipEntryOnWeb,
    //    ZipEntryInResource,
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


        public static void DisposeAll(this IEnumerable<IDisposable> src) =>
            src.ForEach(x => x.Dispose());

    }


    public struct Disposable : IDisposable
    {
        public Action disposeAction;
        public void Dispose() => this.disposeAction();
        public Disposable(Action disposeAction) => this.disposeAction = disposeAction;
    }


    /// <summary>
    /// action を登録して、Dispose() 時に実行されるようにする。
    /// </summary>
    public struct DisposableWrap<T> : IDisposable
    {
        Action<T> disposeAction;
        
        public T Valule { get; }


        public DisposableWrap(T src, Action<T> action)
        {
            this.Valule = src;
            this.disposeAction = action;
        }
        public void Dispose() => this.disposeAction(this.Valule);


        public static implicit operator T(DisposableWrap<T> src) => src.Valule;
    }

    public static class DisposableWrapExtension
    {

        public static DisposableWrap<T> AsDisposable<T>(this T src, Action<T> disposeAction) =>
            new DisposableWrap<T>(src, disposeAction);


        // 負荷よりも書き味を優先する用
        public static async ValueTask<DisposableWrap<T>> AsDisposableAsync<T>(this Task<T> srcAsync, Action<T> disposeAction)
        {
            return (await srcAsync).AsDisposable(disposeAction);
        }
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

    /// <summary>
    /// Task は複数回 await しても問題ない様子
    /// ValueTask はダメみたい
    /// </summary>
    public class AsyncLazy<T> : Lazy<Task<T>>
    {
        public AsyncLazy(Func<T> valueFactory) :
            base(() => Task.Factory.StartNew(valueFactory))
        { }

        public AsyncLazy(Func<Task<T>> taskFactory) :
            base(() => Task.Factory.StartNew(() => taskFactory()).Unwrap())
        { }

        public TaskAwaiter<T> GetAwaiter() => this.Value.GetAwaiter();
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

        public static bool IsZero(this Vector3 v) => (v.x * v.x + v.y * v.y + v.z * v.z) == 0.0f;


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

        public static async ValueTask DestroyOnMainThreadAsync(this UnityEngine.Object obj)
        {
            await Awaitable.MainThreadAsync();
            obj.Destroy();
        }

        public static async ValueTask DestroyOnMainThreadAsync(this GameObject obj)
        {
            await Awaitable.MainThreadAsync();
            obj.Destroy();
        }


        //public static void Destroy(this UnityEngine.Object obj) =>
        //    UnityEngine.Object.Destroy(obj);
        public static void Destroy(this UnityEngine.Object obj)
        {
        #if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(obj);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        #else
            UnityEngine.Object.Destroy(obj);
        #endif
        }

        //public static void Destroy(this GameObject obj) =>
        //    GameObject.Destroy(obj);
        public static void Destroy(this GameObject obj)
        {
        #if UNITY_EDITOR
            if (Application.isPlaying)
            {
                GameObject.Destroy(obj);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        #else
            GameObject.Destroy(obj);
        #endif
        }


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

    public struct a
    {
        //Func<Task<T>> func;
        CancellationToken ct;

        public a With(CancellationToken ct)
        {
            this.ct = ct;
            return this;
        }

        public static Task<T> Run<T>(Func<Task<T>> f) => Task.Run(async () => await f());

        ////public TaskAwaiter<T> GetAwaiter() => Task.Run(async () => await this.func(), this.ct).GetAwaiter();
        //public TaskAwaiter<T> GetAwaiter()
        //{
        //    var f = this.func;
        //    return Task.Run(async () => await f(), this.ct).GetAwaiter();
        //}
    }

    //public static class BackTask
    //{
    //    public static Task<T> RunAsync<T>(Func<Task<T>> f, CancellationToken ct) where T : IDisposable =>
    //        Task.Run(async () =>
    //        {
    //            ct.ThrowIfCancellationRequested();

    //            var result = await f();
    //            if (ct.IsCancellationRequested) result.Dispose();
    //            ct.ThrowIfCancellationRequested();

    //            return result;
    //        }, ct);

    //    public static Task<T> RunAsync<T>(Func<T> f, CancellationToken ct) where T : IDisposable =>
    //        Task.Run(() =>
    //        {
    //            ct.ThrowIfCancellationRequested();

    //            var result = f();
    //            if (ct.IsCancellationRequested) result.Dispose();
    //            ct.ThrowIfCancellationRequested();

    //            return result;
    //        }, ct);
    //}

    public static class UtilityExtension
    {


        public static NativeArray<T> ToNativeArray<T>(this T[] src, Allocator allocator = Allocator.Persistent)
            where T : unmanaged
        =>
            new NativeArray<T>(src, allocator);

        public static NativeArray<T> ToNativeArray<T>(this IEnumerable<T> src, Allocator allocator = Allocator.Persistent)
            where T : unmanaged
        =>
            src.ToArray().ToNativeArray(allocator);



        /// <summary>
        /// キーと非同期生成関数を登録する。
        /// キャンセルが発生した場合は、AsyncLazy を辞書から消す。
        /// もしかすると、キャンセルした瞬間から削除までの間に、ほかのスレッドから取得がされることがあるかも？
        /// その場合は、.Value にアクセスしたとき、キャッシュされた例外が投げられるようだ。
        /// </summary>
        //public static AsyncLazy<TValue> GetOrAddLazyAaync<TKey, TValue>(
        //    this ConcurrentDictionary<TKey, AsyncLazy<TValue>> dict, TKey key, Func<Task<TValue>> f)
        public static async Task<TValue> GetOrAddLazyAaync<TKey, TValue>(
            this ConcurrentDictionary<TKey, AsyncLazy<TValue>> dict, TKey key, Func<Task<TValue>> f)
        {
            try
            {
                return await dict.GetOrAdd(key, new AsyncLazy<TValue>(f));
            }
            catch (OperationCanceledException)
            {
                // エラーとここの間に取得するスレッドがあったら、不完全な LazyAsync が返されるかもしれない
                // そういう場合、.Value はキャッシュした例外を投げるらしいので、キャンセルされた挙動をとればよい…？
                //dict[key] = new AsyncLazy<TValue>(f);
                dict.TryRemove(key, out var _);// すでに削除済の場合は失敗する

                throw;
            }
        }

        //public static AsyncLazy<TValue> GetOrAddLazyAaync<TKey, TValue>(
        //    this ConcurrentDictionary<TKey, AsyncLazy<TValue>> dict, TKey key, Func<Task<TValue>> f)
        //=>
        //    dict.GetOrAdd(key, new AsyncLazy<TValue>(f));




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


        public static void AddOrUpdateOrRemove<TKey, TValue>(
            this IDictionary<TKey, TValue> dict, TKey key, TValue value) where TValue:class
        {
            if (value == default)
                dict.Remove(key);
            else
                dict[key] = value;
        }

    }

    public static class Err
    {
        public static void Logging(Action action) => Err<Exception>.Logging(action);
        public static Task LoggingAsync(Func<Task> action) => Err<Exception>.LoggingAsync(action);
        public static ValueTask LoggingAsync(Func<ValueTask> action) => Err<Exception>.LoggingAsync(action);
        public static Awaitable LoggingAsync(Func<Awaitable> action) => Err<Exception>.LoggingAsync(action);

        public static Task<T> OnErrToDefault<T>(Func<Task<T>> f) => Err<Exception>.OnErrToDefault(f);        
        public static T OnErrToDefault<T>(Func<T> f) => Err<Exception>.OnErrToDefault(f);
    }

    public static class Err<TException>
        where TException : Exception
    {
        public static void Logging(Action action)
        {
            try
            {
                action();
            }
            catch (TException e)
            {
                Debug.LogException(e);
                //Debug.LogError(e.ToSafeString());
            }
        }
        public static async Task LoggingAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (TException e)
            {
                Debug.LogException(e);
                //Debug.LogError(e.ToSafeString());
            }
        }
        public static async ValueTask LoggingAsync(Func<ValueTask> action)
        {
            try
            {
                await action();
            }
            catch (TException e)
            {
                Debug.LogException(e);
                //Debug.LogError(e.ToSafeString());
            }
        }
        public static async Awaitable LoggingAsync(Func<Awaitable> action)
        {
            try
            {
                await action();
            }
            catch (TException e)
            {
                Debug.LogException(e);
                //Debug.LogError(e.ToSafeString());
            }
        }


        public static async Task<T> OnErrToDefault<T>(Func<Task<T>> f)
        {
            try
            {
                return await f();
            }
            catch (TException)
            {
                return default;
            }
        }
        public static T OnErrToDefault<T>(Func<T> f)
        {
            try
            {
                return f();
            }
            catch (TException)
            {
                return default;
            }
        }
    }

    public static class TaskExtension
    {

        public static async Awaitable<T> ToAwaitable<T>(this Task<T> t) =>
            await t;

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

        //public static Task<Task<T>> WhenAny<T>(this IEnumerable<Task<T>> src) =>
        //    Task.WhenAny(src);



        public static async ValueTask<T> AwaitAsync<T>(
            this ValueTask<Stream> stream, Func<Stream, ValueTask<T>> act)
        =>
            await act(await stream);

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




        public static IEnumerable<T> Zip<T1, T2, T>(this (IEnumerable<T1> src1, IEnumerable<T2> src2) x, Func<T1, T2, T> f) =>
            Enumerable.Zip(x.src1, x.src2, f);

        public static IEnumerable<(T1, T2)> Zip<T1, T2>(this (IEnumerable<T1> src1, IEnumerable<T2> src2) x) =>
            (x.src1, x.src2).Zip((x, y) => (x, y));

    }

}
