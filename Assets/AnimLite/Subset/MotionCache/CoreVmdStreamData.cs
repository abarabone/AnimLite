using AnimLite.DancePlayable;
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
    /// �f�[�^�L���b�V���p�̃X�g���[���f�[�^�B
    /// �X�g���[���L���b�V���͍\�z���Ȃ��B
    /// �Q�ƃJ�E���g�ŊǗ�����A�J�E���g�� interlocked �ōs����B
    /// </summary>
    public class CoreVmdStreamData : IDisposable
    {
        public StreamDataHolder<quaternion, DummyStreamCache, StreamIndex> RotationStreams;
        public StreamDataHolder<float4, DummyStreamCache, StreamIndex> PositionStreams;
        public StreamDataHolder<float, DummyStreamCache, StreamIndex> FaceStreams;

        //public VmdFaceMapping FaceMap;

        int refCount;
        public CoreVmdStreamData AddRef() { Interlocked.Increment(ref this.refCount); return this; }

        public void Dispose()
        {
            //if ((Interlocked.Decrement(ref this.refCount) > 0) return;
            if (Interlocked.Decrement(ref this.refCount) > 0)
            {
                $"VmdStreamData core : {this.refCount}".ShowDebugLog();
                return;
            }

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
            if (vmddata.IsBlank()) return null;
            ct.ThrowIfCancellationRequested();

            var rot_data = vmddata.bodyKeyStreams.CreateRotationData();
            var pos_data = vmddata.bodyKeyStreams.CreatePositionData();
            var face_data = vmddata.faceKeyStreams.CreateFaceData(facemap.VmdToVrmMaps);

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

            return holder.AddRef();
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

                // �L�[�L���b�V�������j������B�ق��̓f�[�^�L���b�V���ɒu�����̂Ŕj�����Ȃ��B
                dstvmddata.PositionStreams.Cache.Dispose();
                dstvmddata.RotationStreams.Cache.Dispose();
                dstvmddata.FaceStreams.Cache.Dispose();

                srcvmddata.Dispose();
            };

            srcvmddata.AddRef();

            return dstvmddata;
        }

    }

}
