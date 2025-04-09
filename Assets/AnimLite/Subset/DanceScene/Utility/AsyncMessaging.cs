using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Animations;
using Unity.VisualScripting;
using UnityEngine.Playables;
using System;
using System.Threading.Tasks;

namespace AnimLite.DancePlayable
{

    public class AsyncMessaging<T>
    {

        public static void Post(T message = default) => messaging.post(message);

        public static void Throw(Exception e) => messaging.throwErr(e);

        public static void Cancel() => messaging.cancel();

        public static Task<T> ReciveAsync() => messaging.reciveAsync();



        static AsyncMessaging()
        {
            messaging = new AsyncMessaging<T>();
        }
        static AsyncMessaging<T> messaging;



        TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

        object lockobject = new object();


        void post(T message)
        {
            lock (this.lockobject)
            {
                this.tcs.SetResult(message);

                this.tcs = new TaskCompletionSource<T>();
            }
        }

        void throwErr(Exception e)
        {
            lock (this.lockobject)
            {
                var pretcs = this.tcs;

                this.tcs = new TaskCompletionSource<T>();

                pretcs.SetException(e);
            }
        }

        void cancel()
        {
            lock (this.lockobject)
            {
                var pretcs = this.tcs;

                this.tcs = new TaskCompletionSource<T>();

                pretcs.SetCanceled();
            }
        }

        Task<T> reciveAsync()
        {
            lock (this.lockobject)
            {
                return this.tcs.Task;
            }
        }
    }

}
