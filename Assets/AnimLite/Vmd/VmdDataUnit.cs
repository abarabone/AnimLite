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


    /// <summary>
    /// 
    /// </summary>
    public class VmdStreamData : IDisposable
    {
        public StreamDataHolder<quaternion, Key4StreamCache<quaternion>, StreamIndex> RotationStreams;
        public StreamDataHolder<float4, Key4StreamCache<float4>, StreamIndex> PositionStreams;
        public StreamDataHolder<float, Key2StreamCache<float>, StreamIndex> FaceStreams;

        public bool IsCreated => this.RotationStreams.Streams.KeyStreams.Values.IsCreated;

        public void Dispose()
        {
            this.RotationStreams.Dispose();
            this.PositionStreams.Dispose();
            this.FaceStreams.Dispose();

            "VmdStreamData disposed".ShowDebugLog();
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


        public static Task<(VmdStreamData data, VmdFaceMapping facemap)> LoadVmdStreamDataAsync(
            PathUnit vmdFilePath, PathUnit faceMapFilePath, CancellationToken ct) =>
                Task.Run(() =>
                {
                    var vmddata = VmdParser.ParseVmd(vmdFilePath);
                    ct.ThrowIfCancellationRequested();

                    var facemap = VrmParser.ParseFaceMap(faceMapFilePath);
                    ct.ThrowIfCancellationRequested();

                    var streamdata = vmddata.BuildVmdStreamData(facemap);
                    
                    return (streamdata, facemap);
                }, ct);

        public static Task<VmdStreamData> LoadVmdStreamDataAsync(
            PathUnit vmdFilePath, Vrm.VmdFaceMapping defaultmap, CancellationToken ct) =>
                Task.Run(() =>
                {
                    var vmddata = VmdParser.ParseVmd(vmdFilePath);
                    ct.ThrowIfCancellationRequested();

                    var streamdata = vmddata.BuildVmdStreamData(defaultmap.VmdToVrmMaps);
                    
                    return streamdata;
                }, ct);



        public static Task<VmdStreamData> BuildVmdStreamDataAsync(
            this VmdMotionData vmdData, VmdFaceMapping facemap, CancellationToken ct) =>
                Task.Run(() => vmdData.BuildVmdStreamData(facemap), ct);


        public static VmdStreamData BuildVmdStreamData(this VmdMotionData vmdData, VmdFaceMapping facemap)
        {
            var rot_data = vmdData.bodyKeyStreams.CreateRotationData();
            var pos_data = vmdData.bodyKeyStreams.CreatePositionData();
            var face_data = vmdData.faceKeyStreams.CreateFaceData(facemap.VmdToVrmMaps);

            var rot_index = rot_data.CreateIndex(indexBlockLength: 100);
            var pos_index = pos_data.CreateIndex(indexBlockLength: 100);
            var face_index = face_data.CreateIndex(indexBlockLength: 100);

            var timer = new StreamingTimer(rot_data.GetLastKeyTime());

            var rot_cache = rot_data.ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulRot>(timer);
            var pos_cache = pos_data.ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulPos>(timer);
            var face_cache = face_data.ToKey2CacheFactory().CreateCacheWithInitialize<Clamp, Key2NearestShift>(timer);

            return new VmdStreamData
            {
                RotationStreams = rot_data.ToHolderWith(rot_cache, rot_index),
                PositionStreams = pos_data.ToHolderWith(pos_cache, pos_index),
                FaceStreams = face_data.ToHolderWith(face_cache, face_index),
            };
        }
    }




    public struct VmdBoneName
    {
        public string name;

        static public implicit operator VmdBoneName(string name) => name.AsVmdBoneName();
    }

    public struct BoneIndex
    {
        public int index;

        static public implicit operator BoneIndex(int i) => i.AsBoneIndex();
    }

    public struct VmdFaceName
    {
        public string name;

        static public implicit operator VmdFaceName(string name) => name.AsVmdFaceName();
    }

    public struct VmdMotionData
    {
        public Dictionary<VmdBoneName, VmdBodyMotionKey[]> bodyKeyStreams;
        public Dictionary<VmdFaceName, VmdFaceKey[]> faceKeyStreams;
    }


    public struct VmdBodyMotionKey
    {
        public uint frameno;
        public float time;
        public float4 pos;
        public quaternion rot;
        //public float4[] interpolation;
    }

    //public struct BoneMappingEntry
    //{
    //    public HumanBoneName humanBoneName;

    //    public VmdBoneName vmdBoneName;
    //}

    public struct VmdFaceKey
    {
        public uint frameNo;
        public float time;
        public float weight;
    }

    public static class VmdUtilityExtension
    {

        public static VmdBoneName AsVmdBoneName(this string name) => new VmdBoneName { name = name };

        public static BoneIndex AsBoneIndex(this int index) => new BoneIndex { index = index };

        public static VmdFaceName AsVmdFaceName(this string name) => new VmdFaceName { name = name };
    }



}
