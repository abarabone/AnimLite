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
    }

    public struct VmdCameraData
    {
        public VmdCameraMotionKey[] cameraKeyStream;
    }



    public struct VmdBodyMotionKey
    {
        public uint frameno;
        public float time;
        float padding0;
        float padding1;
        public float4 pos;
        public quaternion rot;
        //public float4[] interpolation;
    }

    public struct VmdFaceKey
    {
        public uint frameNo;
        public float time;
        public float weight;
        float padding0;
    }

    public struct VmdCameraMotionKey
    {
        public uint frameno;
        public float time;
        public float distance;
        public float fov;
        public float4 pos;
        public quaternion rot;
        //public float4[] interpolation;
    }

    public static class VmdUtilityExtension
    {

        public static VmdBoneName AsVmdBoneName(this string name) => new VmdBoneName { name = name };

        public static BoneIndex AsBoneIndex(this int index) => new BoneIndex { index = index };

        public static VmdFaceName AsVmdFaceName(this string name) => new VmdFaceName { name = name };




        /// <summary>
        /// �܂��u�l�c����p�[�X����Ă��Ȃ���� true ��Ԃ��B
        /// �p�[�X����Ă���ꍇ�́A�f�[�^����ł� false ��Ԃ��B
        /// </summary>
        public static bool IsUnload(this VmdMotionData vmddata) => vmddata.bodyKeyStreams is null;

        /// <summary>
        /// �܂��u�l�c����p�[�X����Ă��Ȃ���� true ��Ԃ��B
        /// �p�[�X����Ă���ꍇ�́A�f�[�^����ł� false ��Ԃ��B
        /// </summary>
        public static bool IsUnload(this VmdCameraData vmddata) => vmddata.cameraKeyStream is null;



        /// <summary>
        /// body ���[�V�����f�[�^�̃X�g���[�����P�ł������Ă���� true ��Ԃ��B
        /// �e�X�g���[�����̃L�[�� 0 �ł��A���݂��Ă���΃X�g���[���Ƃ��ĔF�߂�B
        /// bodyKeyStreams �� null �Ȃ� false ��Ԃ����A�p�[�X��ɂ� null �ɂȂ邱�Ƃ͂Ȃ��B
        /// </summary>
        public static bool HasBodyData(this VmdMotionData vmddata) => vmddata.bodyKeyStreams?.Any() ?? false;

        /// <summary>
        /// face ���[�V�����f�[�^�̃X�g���[�����P�ł������Ă���� true ��Ԃ��B
        /// �e�X�g���[�����̃L�[�� 0 �ł��A���݂��Ă���΃X�g���[���Ƃ��ĔF�߂�B
        /// faceKeyStreams �� null �Ȃ� false ��Ԃ����A�p�[�X��ɂ� null �ɂȂ邱�Ƃ͂Ȃ��B
        /// </summary>
        public static bool HasFaceData(this VmdMotionData vmddata) => vmddata.faceKeyStreams?.Any() ?? false;

        /// <summary>
        /// camera �X�g���[�����̃L�[�� 1 �ȏ�ł���� true ��Ԃ��B
        /// �L�[�� 0 �ł���� false ��Ԃ��B
        /// cameraKeyStream �� null �Ȃ� false ��Ԃ����A�p�[�X��ɂ� null �ɂȂ邱�Ƃ͂Ȃ��B
        /// </summary>
        public static bool HasCameraKey(this VmdCameraData vmddata) => vmddata.cameraKeyStream?.Any() ?? false;


    }



    public static class VmdPoseExtension
    {

        public static void SetFirstPose(this Animator anim, VmdStreamData vmddata)
        {



        }


    }

}
