using AnimLite.Utility;
using AnimLite.Vmd;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;

namespace AnimLite.Vmd
{
    using AnimLite.IK;


    public struct VmdFootIkOperator<TTf>
        where TTf : ITransformProxy
    {

        [ReadOnly]
        public float moveScale;
        [ReadOnly]
        public float footScale;

        [ReadOnly]
        public float _footPerMoveScale;


        [ReadOnly]
        public float3 footIkOffsetL;
        [ReadOnly]
        public float3 footIkOffsetR;


        [ReadOnly] public TTf baseAnimator;
        [ReadOnly] public TTf root;

        [ReadOnly] public TTf uLegL;
        [ReadOnly] public TTf uLegR;
        [ReadOnly] public TTf lLegL;
        [ReadOnly] public TTf lLegR;
        [ReadOnly] public TTf footL;
        [ReadOnly] public TTf footR;
    }



    public static class FootIkExtension
    {

        public static void SolveLegPositionIk<TPFinder>(this VmdFootIkOperator<Tf> op, TPFinder pkf)
            where TPFinder : IKeyFinder<float4>
        =>
            op.SolveLegPositionIk(new Tf.StreamSource(), pkf);

        public static void SolveFootRotationIk<TRFinder>(this VmdFootIkOperator<Tf> op, TRFinder rkf)
            where TRFinder : IKeyFinder<quaternion>
        =>
            op.SolveFootRotationIk(new Tf.StreamSource(), rkf);


        public static void SolveLegPositionIk<TPFinder, TTf, TStream>(this VmdFootIkOperator<TTf> op, TStream stream, TPFinder pkf)
            where TTf : ITransformProxy
            where TStream : ITransformStreamSource<TTf>
            where TPFinder : IKeyFinder<float4>
        {
            var basewpos = stream.GetPosition(op.baseAnimator);
            var basewrot = stream.GetRotation(op.baseAnimator);
            var rootlpos_move = stream.GetLocalPosition(op.root);

            var rootpos_foot = rootlpos_move * op._footPerMoveScale;
            var iklposL = pkf.getpos(MmdBodyBones.左足ＩＫ).As3() * op.footScale * 0.1f - rootpos_foot;
            var iklposR = pkf.getpos(MmdBodyBones.右足ＩＫ).As3() * op.footScale * 0.1f - rootpos_foot;

            var ikPosL = iklposL + op.footIkOffsetL + rootlpos_move;
            var ikPosR = iklposR + op.footIkOffsetR + rootlpos_move;

            //var ikPosL = pkf.getpos(MmdBodyBones.左足ＩＫ).As3() * 0.1f * op.bodyScale + op.footIkOffsetL;
            //var ikPosR = pkf.getpos(MmdBodyBones.右足ＩＫ).As3() * 0.1f * op.bodyScale + op.footIkOffsetR;
            
            var posL = math.rotate(basewrot, ikPosL) + basewpos;
            var posR = math.rotate(basewrot, ikPosR) + basewpos;

            //stream.SolveTwoBoneIk(op.uLegL, op.lLegL, op.footL, posL);
            //stream.SolveTwoBoneIk(op.uLegR, op.lLegR, op.footR, posR);
            stream.SolveTwoBonePairIk(
                op.uLegL, op.lLegL, op.footL, posL,
                op.uLegR, op.lLegR, op.footR, posR);
        }


        public static void SolveFootRotationIk<TRFinder, TTf, TStream>(this VmdFootIkOperator<TTf> op, TStream stream, TRFinder rkf)
            where TTf : ITransformProxy
            where TStream : ITransformStreamSource<TTf>
            where TRFinder : IKeyFinder<quaternion>
        {
            var basewrot = stream.GetRotation(op.baseAnimator);

            var lfikr = rkf.getrot(MmdBodyBones.左足ＩＫ);
            var rfikr = rkf.getrot(MmdBodyBones.右足ＩＫ);

            var lr = math.mul(basewrot, lfikr);
            var rr = math.mul(basewrot, rfikr);

            stream.SetRotation(op.footL, lr);
            stream.SetRotation(op.footR, rr);
        }
    }


    public static class FootIkOperatorBuilderExtension
    {

        public static VmdFootIkOperator<Tf> ToVmdFootIkTransformOperator(
            this Animator anim, TransformMappings bone, float moveScale = 0, float footScale = 0)
        =>
            anim.ToVmdFootIkOperator<TransformMappings, Tf>(bone, moveScale, footScale);


        public static VmdFootIkOperator<TTf> ToVmdFootIkOperator<TBone, TTf>(
            this Animator anim, TBone bone, float moveScale = 0, float footScale = 0)
                where TBone : ITransformMappings<TTf>
                where TTf : ITransformProxy, new()
        {
            var tfanim = anim.transform;

            // 足ＩＫローカル位置。
            var footLpos = anim.GetBoneTransform(HumanBodyBones.LeftFoot).position - tfanim.position;   // ルートからの足位置
            var footRpos = anim.GetBoneTransform(HumanBodyBones.RightFoot).position - tfanim.position;
            var footIkOffsetL = tfanim.InverseTransformVector(footLpos);                                // ローカル位置になおす（向き回転を可能にするため）
            var footIkOffsetR = tfanim.InverseTransformVector(footRpos);

            var footScale_ = anim.calcVmdBoneScale(footScale);
            var moveScale_ = anim.calcVmdBoneScale(moveScale);
            
            return new VmdFootIkOperator<TTf>
            {
                footScale = footScale_,
                moveScale = moveScale_,
                _footPerMoveScale = footScale_ / moveScale_,

                footIkOffsetL = footIkOffsetL,
                footIkOffsetR = footIkOffsetR,

                baseAnimator =  anim.CreateTransformProxy<TTf>(tfanim),
                root = gettfhadle_(HumanBodyBones.LastBone),

                uLegL = gettfhadle_(HumanBodyBones.LeftUpperLeg),
                lLegL = gettfhadle_(HumanBodyBones.LeftLowerLeg),
                footL = gettfhadle_(HumanBodyBones.LeftFoot),

                uLegR = gettfhadle_(HumanBodyBones.RightUpperLeg),
                lLegR = gettfhadle_(HumanBodyBones.RightLowerLeg),
                footR = gettfhadle_(HumanBodyBones.RightFoot),
            };

            TTf gettfhadle_(HumanBodyBones boneid) =>
                Enumerable.Range(0, bone.BoneLength)
                    .Select(i => bone[i].Item1)
                    .Where(x => x.HumanBoneId == boneid)
                    .Select(x => x.TransformHandle)
                    .First();
        }

    }

}
