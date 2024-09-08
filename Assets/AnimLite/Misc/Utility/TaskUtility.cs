using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace AnimLite.Utility
{


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



    public static class TaskUtility
    {



        public static async Awaitable<T> ToAwaitable<T>(this Task<T> t) =>
            await t;

        //public static async Awaitable<T[]> AwaitAllAsync<T>(
        //    this IEnumerable<Awaitable<T>> src, Func<T, bool> criteria = default)
        //{
        //    var dst = new List<T>();
        //    foreach (var e in src)
        //    {
        //        var t = await e;
        //        if ((!criteria?.Invoke(t)) ?? false) continue;
        //        dst.Add(t);
        //    }
        //    return dst.ToArray();
        //}




        public static ValueTask<T> AsValueTask<T>(this Task<T> src) => new ValueTask<T>(src);





        public static Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> src) =>
            Task.WhenAll(src);
        //public static Awaitable<T[]> WhenAll<T>(this IEnumerable<Awaitable<T>> src) =>
        //    Task.WhenAll(src);

        //public static Task<Task<T>> WhenAny<T>(this IEnumerable<Task<T>> src) =>
        //    Task.WhenAny(src);

        public static async ValueTask<T[]> WhenAll<T>(this IEnumerable<ValueTask<T>> src)
        {
            return await src.Select(x => x.AsTask()).WhenAll();
        }






        public static async ValueTask<Tdst> Await<Tsrc, Tdst>(
            this Task<Tsrc> src, Func<Tsrc, CancellationToken, ValueTask<Tdst>> act, CancellationToken ct)
        =>
            await act(await src, ct);

        public static async ValueTask<Tdst> Await<Tsrc, Tdst>(
            this ValueTask<Tsrc> src, Func<Tsrc, CancellationToken, ValueTask<Tdst>> act, CancellationToken ct)
        =>
            await act(await src, ct);


        public static async ValueTask<Tdst> Await<Tsrc, Tdst>(
            this Task<Tsrc> src, Func<Tsrc, Tdst> act)
        =>
            act(await src);

        public static async ValueTask<Tdst> Await<Tsrc, Tdst>(
            this ValueTask<Tsrc> src, Func<Tsrc, Tdst> act)
        =>
            act(await src);





        public static async Awaitable OnMainThreadAsync(this Action action)
        {
            await Awaitable.MainThreadAsync();

            action();
        }

        public static async ValueTask OnMainThreadAsync(this Func<ValueTask> action)
        {
            await Awaitable.MainThreadAsync();

            await action();
        }


        public static async Awaitable<T> OnMainThreadAsync<T>(this Func<T> action)
        {
            await Awaitable.MainThreadAsync();

            return action();
        }

        public static async ValueTask<T> OnMainThreadAsync<T>(this Func<ValueTask<T>> action)
        {
            await Awaitable.MainThreadAsync();

            return await action();
        }
    }
}
