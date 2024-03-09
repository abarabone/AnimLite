using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace AnimLite.Vmd
{

    public struct VmdBodyMotionOperator<TBone, TTf>
        where TBone : ITransformMappings<TTf>
        where TTf : ITransformProxy
    {

        [ReadOnly]
        public TBone bone;


        [ReadOnly]
        public float bodyScale;

        [ReadOnly]
        public float3 spineToHipLocal;
        [ReadOnly]
        public float3 rootToHipLocal;


        //public float3 prevLocalPosition;
        //public quaternion prevLocalRotation;

    }


    public static class VmdBodyMotionOperatorBuilderExtension
    {



        public static VmdBodyMotionOperator<TransformMappings, Tf> ToVmdBodyTransformMotionOperator(
            this Animator anim, TransformMappings bone)
        =>
            anim.ToVmdBodyMotionOperator<TransformMappings, Tf>(bone);



        // mmd �� humanoid �̃X�P�[���� 80cm : 100cm ���炢
        // 158cm �̃~�N�̌҈ʒu�����̂��炢�Ǝv����
        // humanoid �� humanscale 1m �́Ahip �̈ʒu�炵���Ƃ̂���

        public static VmdBodyMotionOperator<TBone, TTf> ToVmdBodyMotionOperator<TBone, TTf>(
            this Animator anim, TBone bone)
                where TBone : ITransformMappings<TTf>
                where TTf : ITransformProxy
        {
            //anim.ResetPose();

            var bodySizeRate = anim.humanScale * 0.8f;// 0.8 �́A�~�N �� humaoid �␳

            // humanoid �̏����|�[�Y�̒��S�� root �ɗ���悤�ŁA���̂��ʒu�͂��Ԃ� 1m * humanScale 
            // �Ȃ̂Ŏd���Ȃ� hip �̈ʒu�� humanScale �Ō��ɖ߂����߂ɁA�����Őݒ肷��B
            //var bodyInitialOffset = Vector3.up.As_float3() * anim.humanScale;
            // ����L�̂悤�ɍl���Ă������A�ǂ��� hip �̍����ƍl���Ă悢�炵���B

            //anim.BindStreamTransform(anim.transform);// �o�C���h���Ȃ��� rootMotionPosition ���擾�ł��Ȃ��l�q

            return new VmdBodyMotionOperator<TBone, TTf>
            {
                bodyScale = bodySizeRate,

                bone = bone,

                spineToHipLocal = -anim.GetBoneTransform(HumanBodyBones.Spine).localPosition,
                rootToHipLocal = anim.GetBoneTransform(HumanBodyBones.Hips).localPosition,
            };
        }

    }

    // �l�l�c�̈ړ��f�[�^�́A�{�[���̃I�t�Z�b�g�͏������l���i�[����Ă���悤�Ɏv���B
    // �Ȃ̂ŁA�Z���^�[ �� �����g �܂ł̍����́A�ړ��f�[�^�ɂ͔��f����Ă��Ȃ��ƍl����B
    // �܂�A���ΓI�ړ��ʂ݂̂��f�[�^�ƂȂ��Ă���Aroot �ʒu�͑����̂܂܈ړ�������΂悢�A�ƍl����B

}
