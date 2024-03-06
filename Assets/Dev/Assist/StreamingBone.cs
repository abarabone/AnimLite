using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace AnimLite
{
    public interface IStreamBone<TTf>
        where TTf : ITransformProxy
    {
        (HumanBoneReference<TTf> human, BoneRotationInitialPose initpose, OptionalBoneChecker option) this[int i] { get; }

        int BoneLength { get; }
    }



    /// <summary>
    /// モデルごとに持つ
    /// ・ボーンごとに、対応するストリームのインデックスを持つ
    /// ・モデルのヒューマノイド階層に存在するボーンのみ
    /// </summary>
    public struct JobPlayableStreamingBone : IStreamBone<TfHandle>, IDisposable
    {
        public NativeArray<HumanBoneReference<TfHandle>> BoneToStreamIndexMappings;
        public NativeArray<BoneRotationInitialPose> InitialPoseRotations;

        public OptionalBoneChecker OptionalBones;


        public (HumanBoneReference<TfHandle>, BoneRotationInitialPose, OptionalBoneChecker) this[int i]
        {
            get => (this.BoneToStreamIndexMappings[i], this.InitialPoseRotations[i], this.OptionalBones);
        }
        public int BoneLength => this.BoneToStreamIndexMappings.Length;

        public void Dispose()
        {
            this.BoneToStreamIndexMappings.Dispose();
            this.InitialPoseRotations.Dispose();
        }
    }

    /// <summary>
    /// モデルごとに持つ
    /// ・ボーンごとに、対応するストリームのインデックスを持つ
    /// ・モデルのヒューマノイド階層に存在するボーンのみ
    /// </summary>
    public struct TransformStreamingBone : IStreamBone<Tf>
    {
        public HumanBoneReference<Tf>[] BoneToStreamIndexMappings;
        public BoneRotationInitialPose[] InitialPoseRotations;

        public OptionalBoneChecker OptionalBones;


        public (HumanBoneReference<Tf>, BoneRotationInitialPose, OptionalBoneChecker) this[int i]
        {
            get => (this.BoneToStreamIndexMappings[i], this.InitialPoseRotations[i], this.OptionalBones);
        }
        public int BoneLength => this.BoneToStreamIndexMappings.Length;
    }

    /// <summary>
    /// 
    /// </summary>
    public struct TransformAccessStreamingBone
    {
        public TransformAccessArray transforms;
        public NativeArray<HumanBoneReference> BoneToStreamIndexMappings;
        public NativeArray<BoneRotationInitialPose> InitialPoseRotations;

        public OptionalBoneChecker OptionalBones;

        public void Dispose()
        {
            this.transforms.Dispose();
            this.BoneToStreamIndexMappings.Dispose();
            this.InitialPoseRotations.Dispose();
        }
    }





    /// <summary>
    /// 
    /// </summary>
    public struct OptionalBoneChecker
    {
        public bool HasChest;
        public bool HasLeftSholder;
        public bool HasRightSholder;
    }

    /// <summary>
    /// 
    /// </summary>
    public struct HumanBoneReference<TTf>
        where TTf : ITransformProxy
    {
        public TTf TransformHandle;

        public HumanBodyBones HumanBoneId;
        public int StreamId;
    }
    public struct HumanBoneReference
    {
        public HumanBodyBones HumanBoneId;
        public int StreamId;
    }

    /// <summary>
    /// 
    /// </summary>
    public struct BoneRotationInitialPose
    {
        public quaternion RotLocalize;  // 左からかける
        public quaternion RotGlobalize; // 右からかける
    }


    public static class BoneUtility
    {

        /// <summary>
        /// 
        /// </summary>
        public static quaternion RotateBone(this BoneRotationInitialPose initpose, quaternion streamLocalRotation)
        {
            //return quaternion.identity;
            //return streamLocalRotation;
            //return initpose.RotGlobalize;
            return mul(initpose.RotLocalize, streamLocalRotation, initpose.RotGlobalize);
        }

        static quaternion mul(quaternion r1, quaternion r2) => math.mul(r1, r2);
        static quaternion mul(quaternion r1, quaternion r2, quaternion r3) => math.mul(math.mul(r1, r2), r3);
    }

}
