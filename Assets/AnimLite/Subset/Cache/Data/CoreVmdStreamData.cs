﻿using AnimLite.DancePlayable;
using AnimLite.Vmd;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UniVRM10;

namespace AnimLite.Utility
{
    using AnimLite.Vrm;
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;
    using Unity.Mathematics;
    using System.IO.Compression;



    /// <summary>
    /// データキャッシュ用のストリームデータ。
    /// ストリームキャッシュは構築しない。
    /// 参照カウントで管理され、カウントは interlocked で行われる。
    /// </summary>
    public class CoreVmdStreamData : IDisposable
    {
        public StreamDataHolder<quaternion, DummyStreamCache, StreamIndex> RotationStreams;
        public StreamDataHolder<float4, DummyStreamCache, StreamIndex> PositionStreams;
        public StreamDataHolder<float, DummyStreamCache, StreamIndex> FaceStreams;

        public void Dispose()
        {
            "VmdStreamData core disposed".ShowDebugLog();

            this.RotationStreams.Dispose();
            this.PositionStreams.Dispose();
            this.FaceStreams.Dispose();
        }
    }

    public static class CoreVmdStreamDataExtension
    {


        public static CoreVmdStreamData BuildStreamCoreData(
            this VmdMotionData vmddata, VmdFaceMapping facemap, CancellationToken ct)
        {
            if (vmddata.IsUnload()) return null;
            ct.ThrowIfCancellationRequested();

            var rot_data = vmddata.bodyKeyStreams.CreateRotationData();
            var pos_data = vmddata.bodyKeyStreams.CreatePositionData();
            var face_data = vmddata.faceKeyStreams.CreateFaceData(facemap);

            var rot_index = rot_data.CreateIndex(indexBlockLength: 100);
            var pos_index = pos_data.CreateIndex(indexBlockLength: 100);
            var face_index = face_data.CreateIndex(indexBlockLength: 100);

            var holder = new CoreVmdStreamData
            {
                PositionStreams = new()
                {
                    Streams = pos_data,
                    Index = pos_index,
                },
                RotationStreams = new()
                {
                    Streams = rot_data,
                    Index = rot_index,
                },
                FaceStreams = new()
                {
                    Streams = face_data,
                    Index = face_index,
                },
            };

            ct.ThrowIfCancellationRequested(holder.Dispose);

            return holder;
        }

        public static VmdStreamData CloneShallowlyWithCache(this CoreVmdStreamData srcvmddata)
        {
            if (srcvmddata == default) return default;

            var timer = new StreamingTimer(srcvmddata.RotationStreams.Streams.GetLastKeyTime());

            var dstvmddata = new VmdStreamData
            {
                RotationStreams = new()
                {
                    Streams = srcvmddata.RotationStreams.Streams,
                    Index = srcvmddata.RotationStreams.Index,
                    Cache = srcvmddata.RotationStreams.Streams
                        .ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulRot>(timer),
                },
                PositionStreams = new()
                {
                    Streams = srcvmddata.PositionStreams.Streams,
                    Index = srcvmddata.PositionStreams.Index,
                    Cache = srcvmddata.PositionStreams.Streams
                        .ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulPos>(timer),
                },
                FaceStreams = new()
                {
                    Streams = srcvmddata.FaceStreams.Streams,
                    Index = srcvmddata.FaceStreams.Index,
                    Cache = srcvmddata.FaceStreams.Streams
                        //.ToKey2CacheFactory().CreateCacheWithInitialize<Clamp, Key2NearestShift>(timer),
                        .ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4Catmul>(timer),
                },
            };

            dstvmddata.DisposeAction = () =>
            {
                "VmdStreamData cache only disposed".ShowDebugLog();

                // キーキャッシュだけ破棄する。ほかはデータキャッシュに置かれるので破棄しない。
                dstvmddata.PositionStreams.Cache.Dispose();
                dstvmddata.RotationStreams.Cache.Dispose();
                dstvmddata.FaceStreams.Cache.Dispose();
            };

            return dstvmddata;
        }

    }

}
