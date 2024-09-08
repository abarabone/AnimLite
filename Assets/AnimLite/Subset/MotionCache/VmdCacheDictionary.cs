using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;
using System.Collections.Concurrent;
using System.IO.Compression;

namespace AnimLite.Vmd
{
    using AnimLite.Vrm;
    using AnimLite.Utility;
    using AnimLite.DancePlayable;
    using AnimLite.Utility.Linq;
    //using VRM;


    [Serializable]
    public class VmdCacheDictionary
    {

        //Dictionary<PathUnit, DataCache> cache { get; } = new();
        ConcurrentDictionary<PathUnit, AsyncLazy<InnerCache>> cache { get; } = new();

        //CancellationTokenSource cts = new();


        struct InnerCache
        {
            //public Dictionary<PathUnit, VmdStreamDataHolder> cache { get; private set; }
            public ConcurrentDictionary<PathUnit, AsyncLazy<CoreVmdStreamData>> cache { get; private set; }

            public VmdFaceMapping facemap;

            public InnerCache(VmdFaceMapping facemap)
            {
                this.cache = new();
                this.facemap = facemap;
            }
        }



        public Task<(CoreVmdStreamData vmddata, VmdFaceMapping facemap)> GetOrLoadAsync(
            PathUnit vmdpath, PathUnit facemappath, CancellationToken ct) =>
                GetOrLoadAsync(vmdpath, facemappath, null, ct);


        public async Task<(CoreVmdStreamData vmddata, VmdFaceMapping facemap)> GetOrLoadAsync(
            PathUnit vmdpath, PathUnit facemappath, IArchive archive, CancellationToken ct)
        {

            var outercache = this;
            var innercache = await getInnerCacheAsync_();

            var vmddata = await getDataAsync_(innercache);

            return (vmddata, innercache.facemap);


            Task<InnerCache> getInnerCacheAsync_() =>
                outercache.cache.GetOrAddLazyAaync(facemappath, async () =>
                {
                    var facemap = await facemappath.LoadFaceMapExAsync(ct);

                    return new InnerCache(facemap);
                });

            Task<CoreVmdStreamData> getDataAsync_(InnerCache innercache) =>
                innercache.cache.GetOrAddLazyAaync(vmdpath, () =>
                {
                    return vmdpath.LoadVmdCoreDataExAsync(innercache.facemap, archive, ct).AsTask();
                });
        }

        //public async Awaitable Remove(PathUnit vmdpath, PathUnit facemappath)
        //{
        //    var lazy = this.cache.TryGetOrDefault(facemappath);
        //    if (lazy == default) return;

        //    var innerlazy = (await lazy).cache.TryGetOrDefault(vmdpath);
        //    if (innerlazy == default) return;

        //    (await innerlazy).Dispose();
        //}

        public async Task ClearCache()
        {
            //this.cache
            //    .SelectMany(async x => (await x.Value).cache)
            //    .ForEach(async x => (await x.Value).Dispose());
            foreach (var x in this.cache)
            {
                var innercache = await x.Value;
                foreach (var y in innercache.cache)
                {
                    (await y.Value).Dispose();
                }
            }
            this.cache.Clear();
        }
    }
}
