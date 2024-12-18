using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace AnimLite.Utility
{

    using AnimLite.Utility.Linq;

    public static class DictionaryExtension
    {



        public static T AddChain<T, TKey, TValue>(this T dict, TKey key, TValue value)
            where T : Dictionary<TKey, TValue>
        {
            dict.Add(key, value);
            return dict;
        }








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





        public static TValue GetOrCreate<TKey, TValue>(
            this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> createAction)
        {
            var x = dict.TryGet(key);
            if (x.isExists)
            {
                return x.value;
            }

            var newvalue = createAction();
            dict.Add(key, newvalue);
            return newvalue;
        }
        public static async ValueTask<TValue> GetOrCreateAsync<TKey, TValue>(
            this IDictionary<TKey, TValue> dict, TKey key, Func<ValueTask<TValue>> createAction)
        {
            var x = dict.TryGet(key);
            if (x.isExists)
            {
                return x.value;
            }

            var newvalue = await createAction();
            dict.Add(key, newvalue);
            return newvalue;
        }




        /// <summary>
        /// 
        /// </summary>
        public static IEnumerable<TElement> TryGetOrBlank<TKey, TElement>(
            this ILookup<TKey, TElement> src, TKey key)
        {
            if (!src.Contains(key)) return new EmptyEnumerableStruct<TElement>();

            return src[key];
        }

        /// <summary>
        /// 
        /// </summary>
        public static IEnumerable<TElement> TryGetOrBlank<TKey, TElement>(
            //this IDictionary<TKey, IEnumerable<TElement>> src, TKey key)
            this Dictionary<TKey, TElement[]> src, TKey key)
        {
            return src.TryGetOrDefault(key, new EmptyEnumerableStruct<TElement>());
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
}
