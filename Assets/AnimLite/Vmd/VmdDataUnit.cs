using System;
using System.Collections.Generic;
using Unity.Mathematics;


namespace AnimLite.Vmd
{
    /// <summary>
    /// 
    /// </summary>
    public struct VmdStreamData : IDisposable
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
