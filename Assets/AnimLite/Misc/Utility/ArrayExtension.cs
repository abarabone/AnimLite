using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

namespace AnimLite.Utility
{
    //public struct a
    //{
    //    //Func<Task<T>> func;
    //    CancellationToken ct;

    //    public a With(CancellationToken ct)
    //    {
    //        this.ct = ct;
    //        return this;
    //    }

    //    public static Task<T> Run<T>(Func<Task<T>> f) => Task.Run(async () => await f());

    //    ////public TaskAwaiter<T> GetAwaiter() => Task.Run(async () => await this.func(), this.ct).GetAwaiter();
    //    //public TaskAwaiter<T> GetAwaiter()
    //    //{
    //    //    var f = this.func;
    //    //    return Task.Run(async () => await f(), this.ct).GetAwaiter();
    //    //}
    //}

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



    public static class ArrayExtension
    {


        public static NativeArray<T> ToNativeArray<T>(this T[] src, Allocator allocator = Allocator.Persistent)
            where T : unmanaged
        =>
            new NativeArray<T>(src, allocator);

        public static NativeArray<T> ToNativeArray<T>(this IEnumerable<T> src, Allocator allocator = Allocator.Persistent)
            where T : unmanaged
        =>
            src.ToArray().ToNativeArray(allocator);

    }
}
