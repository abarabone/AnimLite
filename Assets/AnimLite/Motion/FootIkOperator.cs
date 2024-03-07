using AnimLite.Utility;
using AnimLite.Vmd;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;

namespace AnimLite.IK
{


    public struct FootIkOperator<TTf>
        where TTf : ITransformProxy
    {

        [ReadOnly]
        public float bodyScale;

        [ReadOnly]
        public float3 footIkOffsetL;
        [ReadOnly]
        public float3 footIkOffsetR;


        //[ReadOnly] public TTf root;

        //[ReadOnly] public TransformStreamHandle animator;
        //[ReadOnly] public TransformStreamHandle hip;
        //[ReadOnly] public TransformStreamHandle spine;//
        [ReadOnly] public TTf uLegL;
        [ReadOnly] public TTf uLegR;
        [ReadOnly] public TTf lLegL;
        [ReadOnly] public TTf lLegR;
        [ReadOnly] public TTf footL;
        [ReadOnly] public TTf footR;
    }



    public static class FootIkExtension
    {

        public static void SolveLegPositionIk<TPFinder>(
            this FootIkOperator<Tf> op, TPFinder pkf, float3 rootpos, quaternion rootrot)
                where TPFinder : IKeyFinder<float4>
        =>
            op.SolveLegPositionIk(new Tf.StreamSource(), pkf, rootpos, rootrot);

        public static void SolveFootRotationIk<TRFinder>(
            this FootIkOperator<Tf> op, TRFinder rkf, float3 rootpos, quaternion rootrot)
                where TRFinder : IKeyFinder<quaternion>
        =>
            op.SolveFootRotationIk(new Tf.StreamSource(), rkf, rootpos, rootrot);


        public static void SolveLegPositionIk<TPFinder, TTf, TStream>(
            this FootIkOperator<TTf> op, TStream stream, TPFinder pkf, float3 rootpos, quaternion rootrot)
                where TTf : ITransformProxy
                where TStream : ITransformStreamSource<TTf>
                where TPFinder : IKeyFinder<float4>
        {
            var ikPosL = pkf.getpos(MmdBodyBones.左足ＩＫ).As3() * 0.1f * op.bodyScale + op.footIkOffsetL;
            var ikPosR = pkf.getpos(MmdBodyBones.右足ＩＫ).As3() * 0.1f * op.bodyScale + op.footIkOffsetR;

            var posL = math.rotate(rootrot, ikPosL) + rootpos;
            var posR = math.rotate(rootrot, ikPosR) + rootpos;

            stream.SolveTwoBonePairIk(
                op.uLegL, op.lLegL, op.footL, posL,
                op.uLegR, op.lLegR, op.footR, posR);
            //stream.SolveTwoBoneIk(op.uLegL, op.lLegL, op.footL, posL);
            //stream.SolveTwoBoneIk(op.uLegR, op.lLegR, op.footR, posR);
        }

        public static void SolveFootRotationIk<TRFinder, TTf, TStream>(
            this FootIkOperator<TTf> op, TStream stream, TRFinder rkf, float3 rootpos, quaternion rootrot)
                where TTf : ITransformProxy
                where TStream : ITransformStreamSource<TTf>
                where TRFinder : IKeyFinder<quaternion>
        {
            var lfikr = rkf.getrot(MmdBodyBones.左足ＩＫ);
            var rfikr = rkf.getrot(MmdBodyBones.右足ＩＫ);

            var lr = math.mul(rootrot, lfikr);
            var rr = math.mul(rootrot, rfikr);

            stream.SetRotation(op.footL, lr);
            stream.SetRotation(op.footR, rr);
        }
    }


    public static class FootIkOperatorBuilderExtension
    {

        public static FootIkOperator<Tf> ToFootIkTransformOperator(
            this Animator anim, TransformMappings bone)
        =>
            anim.ToFootIkOperator<TransformMappings, Tf>(bone);


        public static FootIkOperator<TTf> ToFootIkOperator<TBone, TTf>(this Animator anim, TBone bone)
            where TBone : ITransformMappings<TTf>
            where TTf : ITransformProxy, new()
        {
            var bodySizeRate = anim.humanScale * 0.8f;// 0.8 は、ミク → humaoid 補正

            var tfanim = anim.transform;

            // 足ＩＫ位置補正の調整。体格を考慮した値に補正する。
            var footLpos = anim.GetBoneTransform(HumanBodyBones.LeftFoot).position - tfanim.position;   // ルートからの足位置
            var footRpos = anim.GetBoneTransform(HumanBodyBones.RightFoot).position - tfanim.position;
            var footIkOffsetL = tfanim.InverseTransformVector(footLpos);                                // ローカル位置になおす（向き回転を可能にするため）
            var footIkOffsetR = tfanim.InverseTransformVector(footRpos);
            // vmd には相対位置しか記録されないと推測されるので、初期ポーズでの足の位置はこちらで用意する必要がある。

            anim.BindStreamTransform(anim.transform);// バインドしないと rootMotionPosition が取得できない様子

            return new FootIkOperator<TTf>
            {
                bodyScale = bodySizeRate,

                footIkOffsetL = footIkOffsetL,
                footIkOffsetR = footIkOffsetR,

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
