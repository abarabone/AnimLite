using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace AnimLite
{
    /// <summary>
    /// モデルごとに持つ
    /// ・ボーンごとに、対応するストリームのインデックスを持つ
    /// ・モデルのヒューマノイド階層に存在するボーンのみ
    /// </summary>
    public struct TransformHandleMappings : ITransformMappings<TfHandle>, IDisposable
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
    public struct TransformMappings : ITransformMappings<Tf>
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
    public struct TransformAccessMappings
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
        public quaternion RotGlobalize;  // 左からかける
        public quaternion RotLocalize; // 右からかける
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
            return mul(initpose.RotGlobalize, streamLocalRotation, initpose.RotLocalize);
            //return mul(initpose.RotLocalize, initpose.RotGlobalize);
        }

        static quaternion mul(quaternion r1, quaternion r2) => math.mul(r1, r2);
        static quaternion mul(quaternion r1, quaternion r2, quaternion r3) => math.mul(math.mul(r1, r2), r3);
    }

}
