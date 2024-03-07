using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace AnimLite
{
    /// <summary>
    /// ���f�����ƂɎ���
    /// �E�{�[�����ƂɁA�Ή�����X�g���[���̃C���f�b�N�X������
    /// �E���f���̃q���[�}�m�C�h�K�w�ɑ��݂���{�[���̂�
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
    /// ���f�����ƂɎ���
    /// �E�{�[�����ƂɁA�Ή�����X�g���[���̃C���f�b�N�X������
    /// �E���f���̃q���[�}�m�C�h�K�w�ɑ��݂���{�[���̂�
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
        public quaternion RotLocalize;  // �����炩����
        public quaternion RotGlobalize; // �E���炩����
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
