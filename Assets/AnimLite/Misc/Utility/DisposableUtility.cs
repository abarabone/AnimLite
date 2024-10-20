using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AnimLite.Utility
{


    /// <summary>
    /// 汎用の即席 Disposable
    /// </summary>
    public struct Disposable : IDisposable
    {
        public Action disposeAction;
        public void Dispose() => this.disposeAction();
        public Disposable(Action disposeAction) => this.disposeAction = disposeAction;
    }


    
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

    public static class DisposablBagExtension
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





    /// <summary>
    /// 任意の型に対して action を登録し、Dispose() 時に実行されるようにする。
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
        public static async ValueTask<DisposableWrap<T>> AsDisposableAwait<T>(this Task<T> srcAsync, Action<T> disposeAction)
        {
            return (await srcAsync).AsDisposable(disposeAction);
        }
        public static async ValueTask<DisposableWrap<T>> AsDisposableAwait<T>(this ValueTask<T> srcAsync, Action<T> disposeAction)
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

}


