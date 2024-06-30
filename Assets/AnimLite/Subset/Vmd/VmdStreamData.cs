using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace AnimLite.Vmd
{
    using AnimLite.Vrm;
    using AnimLite.Utility;
    using VRM;


    /// <summary>
    /// 
    /// </summary>
    public class VmdStreamData : IDisposable
    {
        public StreamDataHolder<quaternion, Key4StreamCache<quaternion>, StreamIndex> RotationStreams;
        public StreamDataHolder<float4, Key4StreamCache<float4>, StreamIndex> PositionStreams;
        public StreamDataHolder<float, Key2StreamCache<float>, StreamIndex> FaceStreams;

        public bool IsCreated => this.RotationStreams.Streams.KeyStreams.Values.IsCreated;


        public Action DisposeAction;

        public void Dispose()
        {
            this.DisposeAction();
        }
    }

    public class VmdStreamSupports : IDisposable
    {
        public TransformHandleMappings bone;
        public VrmExpressionMappings face;

        public void Dispose()
        {
            this.bone.Dispose();
            //this.face.Dispose();

            "VmdStreamSupports disposed".ShowDebugLog();
        }
    }



    public static class VmdData
    {


        public static async Task<(VmdStreamData vmddata, VmdFaceMapping facemap)> LoadVmdStreamDataExAsync(
            this PathUnit vmdFilePath, PathUnit faceMapFilePath, CancellationToken ct)
        {
            var vmddata = await VmdParser.ParseVmdExAsync(vmdFilePath, ct);

            var facemap = await faceMapFilePath.ParseFaceMapExAsync(ct);

            var streamdata = vmddata.BuildVmdStreamData(facemap);
            if (ct.IsCancellationRequested) streamdata.Dispose();
            ct.ThrowIfCancellationRequested();

            return (streamdata, facemap);
        }




        public static async Task<VmdStreamData> LoadVmdStreamDataAsync(
            this PathUnit vmdFilePath, Vrm.VmdFaceMapping defaultmap, CancellationToken ct)
        {
            var vmddata = await VmdParser.ParseVmdAsync(vmdFilePath, ct);

            var streamdata = vmddata.BuildVmdStreamData(defaultmap.VmdToVrmMaps);
            if (ct.IsCancellationRequested) streamdata.Dispose();
            ct.ThrowIfCancellationRequested();

            return streamdata;
        }



        public static VmdStreamData BuildVmdStreamData(this VmdMotionData srcvmdData, VmdFaceMapping facemap)
        {
            var rot_data = srcvmdData.bodyKeyStreams.CreateRotationData();
            var pos_data = srcvmdData.bodyKeyStreams.CreatePositionData();
            var face_data = srcvmdData.faceKeyStreams.CreateFaceData(facemap.VmdToVrmMaps);

            var rot_index = rot_data.CreateIndex(indexBlockLength: 100);
            var pos_index = pos_data.CreateIndex(indexBlockLength: 100);
            var face_index = face_data.CreateIndex(indexBlockLength: 100);

            var timer = new StreamingTimer(rot_data.GetLastKeyTime());

            var rot_cache = rot_data.ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulRot>(timer);
            var pos_cache = pos_data.ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulPos>(timer);
            var face_cache = face_data.ToKey2CacheFactory().CreateCacheWithInitialize<Clamp, Key2NearestShift>(timer);

            var dstvmddata = new VmdStreamData
            {
                RotationStreams = rot_data.ToHolderWith(rot_cache, rot_index),
                PositionStreams = pos_data.ToHolderWith(pos_cache, pos_index),
                FaceStreams = face_data.ToHolderWith(face_cache, face_index),
            };

            dstvmddata.DisposeAction = () =>
            {
                "VmdStreamData disposed".ShowDebugLog();

                dstvmddata.RotationStreams.Dispose();
                dstvmddata.PositionStreams.Dispose();
                dstvmddata.FaceStreams.Dispose();
            };

            return dstvmddata;
        }
    }

}
