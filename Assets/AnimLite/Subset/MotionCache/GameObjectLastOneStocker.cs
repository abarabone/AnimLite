//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using Unity.Mathematics;
//using UnityEngine;
//using System.Linq;
//using System.Collections.Concurrent;
//using System.IO.Compression;

//namespace AnimLite.Vmd
//{
//    using AnimLite.Vrm;
//    using AnimLite.Utility;
//    using AnimLite.DancePlayable;
//    using AnimLite.Utility.Linq;
//    using VRM;

    
//    public class GameObjectLastOneStocker : MonoBehaviour
//    {


//        ConcurrentDictionary<PathUnit, AsyncLazy<GameObject>> cache { get; } = new();

//        CancellationTokenSource cts = new();



//        public Task<T> GetOrLoadAsync(PathUnit path, Func<Task<T>> loadAction)
//        {
//            if (this.cache.Count )

//            return this.cache.GetOrAddLazyAaync(path, loadAction);


//        }

//        public void 


//        public async Task ClearCache()
//        {
//            //this.cache
//            //    .SelectMany(async x => (await x.Value).cache)
//            //    .ForEach(async x => (await x.Value).Dispose());
//            foreach (var x in this.cache)
//            {
//                var innercache = await x.Value;
//                foreach (var y in innercache.cache)
//                {
//                    (await y.Value).Dispose();
//                }
//            }
//            this.cache.Clear();
//        }
//    }

//}
