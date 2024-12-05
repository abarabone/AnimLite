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


    /// <summary>
    /// streamdata と facemap をキャッシュする
    /// facemap 単位で内部辞書があり、streamdata を登録する
    /// facemap も一度だけのみロードされ、キャッシュされる
    /// </summary>
    [Serializable]
    public class VmdCacheDictionary
    {

        //Dictionary<PathUnit, DataCache> cache { get; } = new();
        ConcurrentDictionary<PathUnit, AsyncLazy<InnerCache>> cache { get; } = new();

        //CancellationTokenSource cts = new();


        struct InnerCache
        {
            //public Dictionary<PathUnit, VmdStreamDataHolder> cache { get; private set; }
            public ConcurrentDictionary<PathList, AsyncLazy<CoreVmdStreamData>> cache { get; private set; }

            public VmdFaceMapping facemap;

            public InnerCache(VmdFaceMapping facemap)
            {
                this.cache = new();
                this.facemap = facemap;
            }
        }

        public async ValueTask<VmdFaceMapping> GetFaceMapAsync(PathUnit facemappath) =>
            (await this.cache[facemappath].Value).facemap;






        //public Task<(CoreVmdStreamData vmddata, VmdFaceMapping facemap)> GetOrLoadAsync(
        //    PathUnit vmdpath, PathUnit facemappath, CancellationToken ct) =>
        //        GetOrLoadAsync(vmdpath, facemappath, null, ct);


        //public async Task<(CoreVmdStreamData vmddata, VmdFaceMapping facemap)> GetOrLoadAsync(
        //    PathUnit vmdpath, PathUnit facemappath, IArchive archive, CancellationToken ct)
        //;






        public Task<(CoreVmdStreamData vmddata, VmdFaceMapping facemap)> GetOrLoadAsync(
            PathList vmdpaths, PathUnit facemappath, CancellationToken ct) =>
                GetOrLoadAsync(vmdpaths, facemappath, null, ct);


        public async Task<(CoreVmdStreamData vmddata, VmdFaceMapping facemap)> GetOrLoadAsync(
            PathList vmdpaths, PathUnit facemappath, IArchive archive, CancellationToken ct)
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
                innercache.cache.GetOrAddLazyAaync(vmdpaths, async () =>
                {
                    var vmddata = await vmdpaths.Paths
                        .ToAsyncEnumerable()
                        .SelectAwait(x => archive.LoadVmdExAsync(x, ct))
                        .Where(x => !x.IsUnload())
                        .DefaultIfEmpty()
                        .AggregateAsync((pre, cur) => pre.AppendOrOverwrite(cur));

                    return vmddata.BuildStreamCoreData(innercache.facemap, ct);
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
