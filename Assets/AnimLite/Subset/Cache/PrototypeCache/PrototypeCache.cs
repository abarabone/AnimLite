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
using AnimLite.Vrm;
using AnimLite.Vmd;

namespace AnimLite.Utility
{




    public class PrototypeCache<TKey, TValue>
        where TValue : class
    {

        //[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        //static void Init()
        //{
        //    if (Instance is not null) return;

        //    Instance = new CacheDictionary<T>();
        //}

        //static PrototypeCache()
        //{
        //    Instance = new PrototypeCache<TKey, TValue>();
        //}

        //public static PrototypeCache<TKey, TValue> Instance { get; private set; } = null;




        ConcurrentDictionary<TKey, AsyncLazy<IPrototype<TValue>>> cache { get; } = new();




        public Task<Instance<TValue>> GetOrLoadAsync(TKey key, Func<Task<IPrototype<TValue>>> loadMethod) =>
                GetOrLoadAsync(key, null, loadMethod);


        /// <summary>
        /// キャッシュに登録しつつ取得する
        /// キーになる path がちょっと適当かも、クエリストリングとか　あと更新日とか見たほうがいいのかな
        /// </summary>
        public async Task<Instance<TValue>> GetOrLoadAsync(TKey key, IArchive archive, Func<Task<IPrototype<TValue>>> loadMethod)
        {
            var prototype = await this.cache.GetOrAddLazyAaync(key, loadMethod);

            if (prototype is null)
            {
                this.cache.TryRemove(key, out var _);
                return null;
            }

            return await prototype.InstantiateAsync();
        }


        public async ValueTask ClearCacheAsync()
        {
            // 最初の await より先にクリアしておかないと、ほかのスレッドから参照される可能性がある
            var cacheValues = this.cache.Values;
            this.cache.Clear();

            foreach (var x in cacheValues)
            {
                var p = await x;
                if (p is null) continue;

                await p.DisposeAsync();
            }
        }
    }

}
