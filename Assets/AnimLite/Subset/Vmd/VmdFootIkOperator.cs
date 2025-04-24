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
        public float3 moveScale;
        [ReadOnly]
        public float3 footScale;

        [ReadOnly]
        public float3 _footPerMoveScale;


        [ReadOnly]
        public float3 footIkOffsetL;
        [ReadOnly]
        public float3 footIkOffsetR;


        public FootIkTransforms<TTf> tf;

        [ReadOnly]
        public bool useLegPositionIk;
        [ReadOnly]
        public bool useFootRotationIk;

        [ReadOnly]
        public bool useGroundHit;
        [ReadOnly]
        public LayerMask groundHitMask;
        [ReadOnly]
        public float groundHitDistance;
        [ReadOnly]
        public float groundHitOriginOffset;


    }

    public struct FootIkTransforms<TTf>
    {
        [ReadOnly] public TTf baseAnimator;
        [ReadOnly] public TTf root;

        [ReadOnly] public TTf uLegL;
        [ReadOnly] public TTf uLegR;
        [ReadOnly] public TTf lLegL;
        [ReadOnly] public TTf lLegR;
        [ReadOnly] public TTf footL;
        [ReadOnly] public TTf footR;
    }

    public static class FootIkOperator
    {

        public static float defaultGroundHitDistance = 2.0f;
        public static float defaultGroundHitOriginOffset = 2.0f;

        public static string defaultHitLayer = "foot IK target";

    }

    public static class FootIkOperatorBuilderExtension
    {


        public static VmdFootIkOperator<Tf> ToVmdFootIkTransformOperator(this Animator anim, TransformMappings bone) =>
            anim.ToVmdFootIkOperator<TransformMappings, Tf>(bone);


        public static VmdFootIkOperator<TTf> ToVmdFootIkOperator<TBone, TTf>(this Animator anim, TBone bone)
            where TBone : ITransformMappings<TTf>
            where TTf : ITransformProxy, new()
        {
            var tfanim = anim.transform;

            // 足ＩＫローカル位置。
            var footLpos = anim.GetBoneTransform(HumanBodyBones.LeftFoot).position - tfanim.position;   // ルートからの足位置
            var footRpos = anim.GetBoneTransform(HumanBodyBones.RightFoot).position - tfanim.position;
            var footIkOffsetL = tfanim.InverseTransformVector(footLpos);                                // ローカル位置になおす（向き回転を可能にするため）
            var footIkOffsetR = tfanim.InverseTransformVector(footRpos);

            //var footScale_ = anim.calcVmdBoneScale(footScale);
            //var moveScale_ = anim.calcVmdBoneScale(moveScale);

            return new VmdFootIkOperator<TTf>
            {
                footScale = anim.humanScale * VmdBodyMotionOperator.VmdBodyScale,
                moveScale = anim.humanScale * VmdBodyMotionOperator.VmdBodyScale,
                _footPerMoveScale = 1.0f,

                footIkOffsetL = footIkOffsetL,
                footIkOffsetR = footIkOffsetR,

                useLegPositionIk = false,
                useFootRotationIk = false,

                useGroundHit = false,
                groundHitMask = LayerMask.GetMask(new[] { FootIkOperator.defaultHitLayer }),
                groundHitDistance = FootIkOperator.defaultGroundHitDistance,
                groundHitOriginOffset = FootIkOperator.defaultGroundHitOriginOffset,

                tf = new FootIkTransforms<TTf>
                {
                    baseAnimator = anim.CreateTransformProxy<TTf>(tfanim),
                    root = gettfhadle_(HumanBodyBones.LastBone),

                    uLegL = gettfhadle_(HumanBodyBones.LeftUpperLeg),
                    lLegL = gettfhadle_(HumanBodyBones.LeftLowerLeg),
                    footL = gettfhadle_(HumanBodyBones.LeftFoot),

                    uLegR = gettfhadle_(HumanBodyBones.RightUpperLeg),
                    lLegR = gettfhadle_(HumanBodyBones.RightLowerLeg),
                    footR = gettfhadle_(HumanBodyBones.RightFoot),
                },
            };

            TTf gettfhadle_(HumanBodyBones boneid) =>
                Enumerable.Range(0, bone.BoneLength)
                    .Select(i => bone[i].Item1)
                    .Where(x => x.HumanBoneId == boneid)
                    .Select(x => x.TransformHandle)
                    .First();
        }



        public static VmdFootIkOperator<TTf> WithIkUsage<TTf>(
            this VmdFootIkOperator<TTf> footop,
            StreamData<float4> pos, StreamData<quaternion> rot, VmdFootIkMode ikmode)
                where TTf : ITransformProxy, new()
        =>
            footop.WithIkUsage(pos, rot, ikmode,
                FootIkOperator.defaultGroundHitDistance,
                FootIkOperator.defaultGroundHitOriginOffset);

        public static VmdFootIkOperator<TTf> WithIkUsage<TTf>(
            this VmdFootIkOperator<TTf> footop,
            StreamData<float4> pos, StreamData<quaternion> rot, VmdFootIkMode ikmode,
            float groundHitDistance, float groundhitOriginOffset, string hitTarget = null)
                where TTf : ITransformProxy, new()
        {
            var ikusage = (pos, rot).CheckUseFootIk(ikmode);
            var useGroundHit = (ikmode & VmdFootIkMode.off_with_ground) != 0;
            
            Debug.Log($"legik:{ikusage.leg} footik:{ikusage.foot} groundhit:{useGroundHit}");

            return footop
                .WithFootIkUsage(ikusage.leg, ikusage.foot)
                .WithGroundIkUsage(useGroundHit, groundHitDistance, groundhitOriginOffset, hitTarget);
        }

        public static VmdFootIkOperator<TTf> WithIkUsage<TTf>(
            this VmdFootIkOperator<TTf> footop,
            VmdStreamData vmddata, VmdFootIkMode ikmode)
                where TTf : ITransformProxy, new()
        =>
            footop.WithIkUsage(vmddata.PositionStreams.Streams, vmddata.RotationStreams.Streams, ikmode);

        public static VmdFootIkOperator<TTf> WithIkUsage<TTf>(
            this VmdFootIkOperator<TTf> footop,
            VmdStreamData vmddata, VmdFootIkMode ikmode,
            float groundHitDistance, float groundhitOriginOffset, string hitTarget = null)
                where TTf : ITransformProxy, new()
        =>
            footop.WithIkUsage<TTf>(vmddata.PositionStreams.Streams, vmddata.RotationStreams.Streams, ikmode);

        public static VmdFootIkOperator<TTf> WithIkUsage<TTf, TPFinder, TRFinder>(
            this VmdFootIkOperator<TTf> footop,
            TPFinder pkf, TRFinder rkf, VmdFootIkMode ikmode)
                where TPFinder : struct, IKeyFinderWithoutProcedure<float4>
                where TRFinder : struct, IKeyFinderWithoutProcedure<quaternion>
                where TTf : ITransformProxy, new()
        =>
            footop.WithIkUsage(pkf.Streams, rkf.Streams, ikmode);

        public static VmdFootIkOperator<TTf> WithIkUsage<TTf, TPFinder, TRFinder>(
            this VmdFootIkOperator<TTf> footop,
            TPFinder pkf, TRFinder rkf, VmdFootIkMode ikmode,
            float groundHitDistance, float groundhitOriginOffset, string hitTarget = null)
                where TPFinder : struct, IKeyFinderWithoutProcedure<float4>
                where TRFinder : struct, IKeyFinderWithoutProcedure<quaternion>
                where TTf : ITransformProxy, new()
        =>
            footop.WithIkUsage(pkf.Streams, rkf.Streams, ikmode, groundHitDistance, groundhitOriginOffset, hitTarget);


        //public static VmdFootIkOperator<TTf> WithScales<TTf>(
        //    this VmdFootIkOperator<TTf> footop, float3 moveScale, float3 footScale)
        //        where TTf : ITransformProxy, new()
        //{
        //    if (moveScale != 0.0f) footop.moveScale = moveScale;
        //    if (footScale != 0.0f) footop.footScale = footScale;

        //    return footop;
        //}
        public static VmdFootIkOperator<TTf> WithScales<TTf>(
            this VmdFootIkOperator<TTf> footop, Animator anim, float3 moveScale, float3 footScale)
                where TTf : ITransformProxy, new()
        {
            footop.moveScale = anim.calcVmdBoneScale(moveScale);
            footop.footScale = anim.calcVmdBoneScale(footScale);
            footop._footPerMoveScale = footop.footScale / footop.moveScale;

            return footop;
        }
        public static VmdFootIkOperator<TTf> WithFootIkUsage<TTf>(
            this VmdFootIkOperator<TTf> footop, bool useLegIk, bool useFootIk)
                where TTf : ITransformProxy, new()
        {
            footop.useLegPositionIk = useLegIk;
            footop.useFootRotationIk = useFootIk;

            return footop;
        }

        public static VmdFootIkOperator<TTf> WithGroundIkUsage<TTf>(
            this VmdFootIkOperator<TTf> footop,
            bool useGroundHit,
            float groundHitDistance, float groundhitOriginOffset, string hitTarget = null)
                where TTf : ITransformProxy, new()
        {
            footop.useGroundHit = useGroundHit;
            footop.groundHitDistance = groundHitDistance;
            footop.groundHitOriginOffset = groundhitOriginOffset;
            
            if (hitTarget != null)
            {
                footop.groundHitMask = LayerMask.GetMask(new[] { hitTarget });
            }

            return footop;
        }


        //public static (bool leg, bool foot) CheckUseFootIk<TPFinder, TRFinder>(
        //    this (TPFinder p, TRFinder r) kf, VmdFootIkMode ikmode)
        //        where TPFinder : struct, IKeyFinderWithoutProcedure<float4>
        //        where TRFinder : struct, IKeyFinderWithoutProcedure<quaternion>
        //{

        //    var useleg = (ikmode & VmdFootIkMode.leg_only) != 0;
        //    var usefoot = (ikmode & VmdFootIkMode.foot_only) != 0;

        //    if ((ikmode & VmdFootIkMode.auto) == 0) return (useleg, usefoot);
        //    var useauto = getUseIk_();

        //    return (useauto.pos | useleg, useauto.rot | usefoot);


        //    (bool pos, bool rot) getUseIk_()
        //    {
        //        var kneeRotLengthL = kf.r.Streams.Sections[(int)MmdBodyBones.左ひざ].length;
        //        var ankleRotLengthL = kf.r.Streams.Sections[(int)MmdBodyBones.左足首].length;
        //        var footIkLengthL = kf.p.Streams.Sections[(int)MmdBodyBones.左足ＩＫ].length;

        //        var kneeRotLengthR = kf.r.Streams.Sections[(int)MmdBodyBones.右ひざ].length;
        //        var ankleRotLengthR = kf.r.Streams.Sections[(int)MmdBodyBones.右足首].length;
        //        var footIkLengthR = kf.p.Streams.Sections[(int)MmdBodyBones.右足ＩＫ].length;

        //        var usePosIk1 =
        //            kneeRotLengthL < 3 & footIkLengthL > 2
        //            &
        //            kneeRotLengthR < 3 & footIkLengthR > 2;

        //        var useRotIk1 =
        //            ankleRotLengthL < 3 & footIkLengthL > 2
        //            &
        //            ankleRotLengthR < 3 & footIkLengthR > 2;

        //        var useIk2 =
        //            ankleRotLengthL < footIkLengthL
        //            &
        //            ankleRotLengthR < footIkLengthR;

        //        return (usePosIk1 | useIk2, useRotIk1 | useIk2);
        //    }
        //}
        public static (bool leg, bool foot) CheckUseFootIk(
            this (StreamData<float4> pos, StreamData<quaternion> rot) data, VmdFootIkMode ikmode)
        {

            var useleg = (ikmode & VmdFootIkMode.leg_only) != 0;
            var usefoot = (ikmode & VmdFootIkMode.foot_only) != 0;

            if ((ikmode & VmdFootIkMode.auto) == 0) return (useleg, usefoot);
            var useauto = getUseIk_();

            return (useauto.pos | useleg, useauto.rot | usefoot);


            (bool pos, bool rot) getUseIk_()
            {
                var kneeRotLengthL = data.rot.Sections[(int)MmdBodyBones.左ひざ].length;
                var ankleRotLengthL = data.rot.Sections[(int)MmdBodyBones.左足首].length;
                var legIkLengthL = data.pos.Sections[(int)MmdBodyBones.左足ＩＫ].length;
                var footIkLengthL = data.rot.Sections[(int)MmdBodyBones.左足ＩＫ].length;

                var kneeRotLengthR = data.rot.Sections[(int)MmdBodyBones.右ひざ].length;
                var ankleRotLengthR = data.rot.Sections[(int)MmdBodyBones.右足首].length;
                var legIkLengthR = data.pos.Sections[(int)MmdBodyBones.右足ＩＫ].length;
                var footIkLengthR = data.rot.Sections[(int)MmdBodyBones.右足ＩＫ].length;

                var usePosIk1 =
                    kneeRotLengthL < 3 & legIkLengthL > 2
                    &
                    kneeRotLengthR < 3 & legIkLengthR > 2;

                var useRotIk1 =
                    //ankleRotLengthL < 3 & legIkLengthL > 2
                    //&
                    //ankleRotLengthR < 3 & legIkLengthR > 2;
                    ankleRotLengthL < 3 & footIkLengthL > 2
                    &
                    ankleRotLengthR < 3 & footIkLengthR > 2;

                var useIk2 =
                    ankleRotLengthL < legIkLengthL
                    &
                    ankleRotLengthR < legIkLengthR;

                return (usePosIk1 | useIk2, useRotIk1 | useIk2);
            }
        }
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
            where TTf : ITransformProxy<TStream>
            where TStream : ITransformStreamSource
            where TPFinder : IKeyFinder<float4>
        {
            if (!op.useLegPositionIk) return;

            var basewpos = op.tf.baseAnimator.GetPosition(stream);
            var basewrot = op.tf.baseAnimator.GetRotation(stream);
            var rootlpos_move = op.tf.root.GetLocalPosition(stream);

            var rootpos_foot = rootlpos_move * op._footPerMoveScale;
            var iklposL = pkf.getpos(MmdBodyBones.左足ＩＫ) * op.footScale * 0.1f - rootpos_foot;
            var iklposR = pkf.getpos(MmdBodyBones.右足ＩＫ) * op.footScale * 0.1f - rootpos_foot;

            var ikPosL = iklposL + op.footIkOffsetL + rootlpos_move;
            var ikPosR = iklposR + op.footIkOffsetR + rootlpos_move;

            //var ikPosL = pkf.getpos(MmdBodyBones.左足ＩＫ).As3() * 0.1f * op.bodyScale + op.footIkOffsetL;
            //var ikPosR = pkf.getpos(MmdBodyBones.右足ＩＫ).As3() * 0.1f * op.bodyScale + op.footIkOffsetR;
            
            var posL = math.rotate(basewrot, ikPosL) + basewpos;
            var posR = math.rotate(basewrot, ikPosR) + basewpos;

            //stream.SolveTwoBoneIk(op.tf.uLegL, op.tf.lLegL, op.tf.footL, posL);
            //stream.SolveTwoBoneIk(op.tf.uLegR, op.tf.lLegR, op.tf.footR, posR);
            //stream.SolveTwoBonePairIk(
            //    op.tf.uLegL, op.tf.lLegL, op.tf.footL, posL,
            //    op.tf.uLegR, op.tf.lLegR, op.tf.footR, posR);
            //stream.SolveTwoBoneIk_(ref op.tf.uLegL, ref op.tf.lLegL, op.tf.uLegL, op.tf.lLegL, op.tf.footL, posL);
            //stream.SolveTwoBoneIk_(ref op.tf.uLegR, ref op.tf.lLegR, op.tf.uLegR, op.tf.lLegR, op.tf.footR, posR);
            stream.SolveTwoBonePairIk(
                ref op.tf.uLegL, ref op.tf.lLegL,
                op.tf.uLegL, op.tf.lLegL, op.tf.footL,
                posL,
                ref op.tf.uLegR, ref op.tf.lLegR,
                op.tf.uLegR, op.tf.lLegR, op.tf.footR,
                posR);
        }


        public static void SolveFootRotationIk<TRFinder, TTf, TStream>(this VmdFootIkOperator<TTf> op, TStream stream, TRFinder rkf)
            where TTf : ITransformProxy<TStream>
            where TStream : ITransformStreamSource
            where TRFinder : IKeyFinder<quaternion>
        {
            if (!op.useFootRotationIk) return;

            var basewrot = op.tf.baseAnimator.GetRotation(stream);

            var lfikr = rkf.getrot(MmdBodyBones.左足ＩＫ);
            var rfikr = rkf.getrot(MmdBodyBones.右足ＩＫ);

            var lr = math.mul(basewrot, lfikr);
            var rr = math.mul(basewrot, rfikr);

            op.tf.footL.SetRotation(stream, lr);
            op.tf.footR.SetRotation(stream, rr);
        }
    }


}
