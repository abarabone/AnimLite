using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;


namespace AnimLite.Vmd
{
    using AnimLite.Utility;


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

        public bool IsBlank() => this.bodyKeyStreams is null;
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



    public static class VmdPoseExtension
    {

        public static void SetFirstPose(this Animator anim, VmdStreamData vmddata)
        {



        }


    }

}
