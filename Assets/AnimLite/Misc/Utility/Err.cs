using System;
using System.Threading.Tasks;
using UnityEngine;

namespace AnimLite.Utility
{

    /// <summary>
    /// エラー処理簡略化用だが、いまいち汎用化できてない
    /// </summary>
    public static class Err
    {
        public static void Logging(Action action) => Err<Exception>.Logging(action);
        //public static Task LoggingAsync(Func<Task> action) => Err<Exception>.LoggingAsync(action);
        public static ValueTask LoggingAsync(Func<ValueTask> action) => Err<Exception>.LoggingAsync(action);
        //public static Awaitable LoggingAsync(Func<Awaitable> action) => Err<Exception>.LoggingAsync(action);

        public static ValueTask<T> OnErrToDefault<T>(Func<ValueTask<T>> f) => Err<Exception>.OnErrToDefault(f);        
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
        //public static async Awaitable LoggingAsync(Func<Awaitable> action)
        //{
        //    try
        //    {
        //        await action();
        //    }
        //    catch (TException e)
        //    {
        //        Debug.LogException(e);
        //        //Debug.LogError(e.ToSafeString());
        //    }
        //}


        public static async ValueTask<T> OnErrToDefault<T>(Func<ValueTask<T>> f)
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
}
