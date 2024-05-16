using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;
using System.Collections.Concurrent;

namespace AnimLite.Vmd
{
    using AnimLite.Vrm;
    using AnimLite.Utility;
    using AnimLite.DancePlayable;
    using AnimLite.Utility.Linq;
    using VRM;
    using UnityEngine.WSA;

    public class VmdStreamDataCache : MonoBehaviour
    {

        VmdCacheDictionary Cache = new();


        public async Awaitable OnDisable()
        {
            await this.Cache.ClearCache();
        }


        //public Awaitable<VmdStreamDataHolder> GetOrLoadAsync(PathUnit vmdpath, CancellationToken ct) =>
        //    this.Cache.GetOrLoadAsync(vmdpath, "", ct);

        //public Awaitable<VmdStreamDataHolder> GetOrLoadAsync(PathUnit vmdpath, PathUnit facemappath, CancellationToken ct) =>
        //    this.Cache.GetOrLoadAsync(vmdpath, facemappath, ct);


        public async Task<DanceGraphy.MotionResource[]> GetOrBuildMotionResourcesAsync(
            DanceMotionDefine[] motions, CancellationToken ct)
        {
            return await motions
                .Select(async motion =>
                {
                    var vmdfullpath = motion.VmdFilePath.ToFullPath();
                    var facefullpath = motion.FaceMappingFilePath.ToFullPath();
                    var anim = motion.ModelAnimator;
                    var face = motion.FaceRenderer;
                    return await buildMotionResourceAsync_(vmdfullpath, facefullpath, anim, face, ct);
                })
                .WhenAll();

            async Task<DanceGraphy.MotionResource> buildMotionResourceAsync_(
                PathUnit vmdpath, PathUnit facemappath, Animator anim, SkinnedMeshRenderer faceRenderer, CancellationToken ct)
            {
                var vmd = await this.GetOrLoadVmdStreamDataAsync(vmdpath, facemappath, ct);

                var r = faceRenderer ?? anim.FindFaceRenderer();

                return new DanceGraphy.MotionResource
                {
                    vmddata = vmd.data,

                    bone = anim.BuildVmdPlayableJobTransformMappings(),
                    face = r?.sharedMesh?.BuildStreamingFace(vmd.facemap) ?? default,

                    DisposeAction = (DanceGraphy.MotionResource mr) =>
                    {
                        // キーキャッシュだけ破棄する。ほかはデータキャッシュに置かれるので破棄しない。
                        mr.vmddata.PositionStreams.Cache.Dispose();
                        mr.vmddata.RotationStreams.Cache.Dispose();
                        mr.vmddata.FaceStreams.Cache.Dispose();
                        mr.bone.Dispose();
                        //mr.face.Dispose();
                    },
                };
            }
        }


        public async Task<(VmdStreamData data, VmdFaceMapping facemap)> GetOrLoadVmdStreamDataAsync(
            PathUnit vmdFilePath, PathUnit faceMapFilePath, CancellationToken ct)
        {
            await Awaitable.BackgroundThreadAsync();

            var holder = await this.Cache.GetOrLoadAsync(vmdFilePath, faceMapFilePath, ct);

            var vmddata = holder.WithStreamCache();

            return (vmddata, holder.FaceMap);
        }


        class VmdCacheDictionary
        {

            //Dictionary<PathUnit, DataCache> cache { get; } = new();
            ConcurrentDictionary<PathUnit, AsyncLazy<InnerCache>> cache { get; } = new();

            CancellationTokenSource cts = new();


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


            public async Awaitable<CoreVmdStreamData> GetOrLoadAsync(
                PathUnit vmdpath, PathUnit facemappath, CancellationToken ct)
            {

                var innercache = await getInnerCacheAsync_();

                var holder = await getDataAsync_(innercache);

                return holder;


                Task<InnerCache> getInnerCacheAsync_() =>
                    this.cache.GetOrAddLazyAaync(facemappath, async () =>
                    {
                        var facemap = await facemappath.LoadFaceMapExAsync(ct);

                        return new InnerCache(facemap);
                    });

                Task<CoreVmdStreamData> getDataAsync_(InnerCache innercache) =>
                    innercache.cache.GetOrAddLazyAaync(vmdpath, () =>
                        vmdpath.LoadVmdCoreDataAsync(innercache.facemap, ct)
                    );
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


    /// <summary>
    /// データキャッシュ用のストリームデータ。
    /// ストリームキャッシュは構築しない。
    /// </summary>
    public class CoreVmdStreamData : IDisposable
    {
        public StreamDataHolder<quaternion, DummyStreamCache, StreamIndex> RotationStreams;
        public StreamDataHolder<float4, DummyStreamCache, StreamIndex> PositionStreams;
        public StreamDataHolder<float, DummyStreamCache, StreamIndex> FaceStreams;

        public VmdFaceMapping FaceMap;

        public void Dispose()
        {
            this.RotationStreams.Dispose();
            this.PositionStreams.Dispose();
            this.FaceStreams.Dispose();
        }
    }

    public static class CoreVmdStreamDataExtension
    {
        public static Task<CoreVmdStreamData> LoadVmdCoreDataAsync(
            this PathUnit vmdpath, VmdFaceMapping facemap, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                var vmddata = VmdParser.ParseVmd(vmdpath);
                ct.ThrowIfCancellationRequested();

                var rot_data = vmddata.bodyKeyStreams.CreateRotationData();
                var pos_data = vmddata.bodyKeyStreams.CreatePositionData();
                var face_data = vmddata.faceKeyStreams.CreateFaceData(facemap.VmdToVrmMaps);

                var rot_index = rot_data.CreateIndex(indexBlockLength: 100);
                var pos_index = pos_data.CreateIndex(indexBlockLength: 100);
                var face_index = face_data.CreateIndex(indexBlockLength: 100);

                var holder = new CoreVmdStreamData
                {
                    PositionStreams = new StreamDataHolder<float4, DummyStreamCache, StreamIndex>
                    {
                        Streams = pos_data,
                        Index = pos_index,
                    },
                    RotationStreams = new StreamDataHolder<quaternion, DummyStreamCache, StreamIndex>
                    {
                        Streams = rot_data,
                        Index = rot_index,
                    },
                    FaceStreams = new StreamDataHolder<float, DummyStreamCache, StreamIndex>
                    {
                        Streams = face_data,
                        Index = face_index,
                    },

                    FaceMap = facemap,
                };

                if (ct.IsCancellationRequested)
                {
                    holder.Dispose();
                }
                ct.ThrowIfCancellationRequested();

                return holder;
            }, ct);
        }

        public static VmdStreamData WithStreamCache(this CoreVmdStreamData holder)
        {
            var timer = new StreamingTimer(holder.RotationStreams.Streams.GetLastKeyTime());

            return new VmdStreamData
            {
                RotationStreams = new StreamDataHolder<quaternion, Key4StreamCache<quaternion>, StreamIndex>
                {
                    Streams = holder.RotationStreams.Streams,
                    Index = holder.RotationStreams.Index,
                    Cache = holder.RotationStreams.Streams
                        .ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulRot>(timer),
                },
                PositionStreams = new StreamDataHolder<float4, Key4StreamCache<float4>, StreamIndex>
                {
                    Streams = holder.PositionStreams.Streams,
                    Index = holder.PositionStreams.Index,
                    Cache = holder.PositionStreams.Streams
                        .ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulPos>(timer),
                },
                FaceStreams = new StreamDataHolder<float, Key2StreamCache<float>, StreamIndex>
                {
                    Streams = holder.FaceStreams.Streams,
                    Index = holder.FaceStreams.Index,
                    Cache = holder.FaceStreams.Streams
                        .ToKey2CacheFactory().CreateCacheWithInitialize<Clamp, Key2NearestShift>(timer),
                },
            };
        }
    }

}