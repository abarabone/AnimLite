using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.UIElements;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using UnityEngine.Jobs;

namespace AnimLite.Vmd.experimental
{
    using AnimLite.Vmd;
    using AnimLite.Vmd.experimental.Data;
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;


    public class ModelParams<TPFinder, TRFinder>
        where TPFinder : unmanaged, IKeyFinderWithoutProcedure<float4>
        where TRFinder : unmanaged, IKeyFinderWithoutProcedure<quaternion>
    {

        public struct ModelData
        {
            public Animator anim;
            public VmdBodyMotionOperator<TransformMappings, Tf> bodyop;
            public VmdFootIkOperator<Tf> footop;
        }


        // pre
        public ModelData model_data;

        public IEnumerable<ModelFinder<TPFinder, TRFinder>> model_finders_origin;
        public IEnumerable<ModelBoneOption> model_boneOptions;
        public EnumerableWithParam<ModelHipBoneAdjust> model_hipAdjusts;
        public IEnumerable<ModelTimer> model_timeOptions;


        // body motion
        public EnumerableWithParam<BoneIndexData> bonefull_rotIndices;
        public IEnumerable<BoneRotationOffsetPose> bonefull_rotOffsets;
        public IEnumerable<Transform> bonefull_transgorms;
        public EnumerableWithParam<BoneTransformApplyIndex> bonefull_transformApplyIndices;

        public EnumerableWithParam<BoneIndexData> boneroot_posIndices;
        public IEnumerable<BodyBoneScale> boneroot_posScales;

        public EnumerableWithParam<BoneIndexData> bonehip_posIndices;
        public IEnumerable<BodyBoneScale> bonehip_posScales;


        // ik
        public IEnumerable<Transform> ikalways_baseTransforms;

        public EnumerableWithParam<LegIkData> ikleg_ikData;
        public EnumerableWithParam<FootIkData> ikfoot_ikData;

        // ik : solving
        public IEnumerable<Transform> ikalways_legTransforms;
        public EnumerableWithParam<SolveIkAnchorIndex> ikalways_ikAnchorIndices;


        // grounding : 
        public EnumerableWithParam<LegIkAnchorIndex> noikleg_ikIndices;
        public IEnumerable<Transform> noikleg_footTransforms;
        //public EnumerableWithParam<FootIkAnchorIndex> noikfoot_ikIndices;
        //public IEnumerable<Transform> noikfoot_footTransforms;

        // grounding : 
        public EnumerableWithParam<LegHitData> ground_hitData;
        public IEnumerable<Transform> ground_rootTransforms;
        public IEnumerable<GroundLegInterpolationStorageLR> ground_rootHeights;

        // grounding : foot interpolation
        public IEnumerable<GroundFootInterpolationStorageLR> ground_footStorages;
        public IEnumerable<Transform> ground_footFkTransforms;
        //public IEnumerable<GroundFootFkTransformValueLR> ground_footFkTransformValues;
        public IEnumerable<Transform> ground_footResultTransforms;
        //public IEnumerable<GroundFootResultValueLR> ground_transformValueResults;

    }

    public struct EnumerableWithParam<T>
    {
        public Func<ParamCount, IEnumerable<T>> invoke;
    }




    public static class ModelParamExtension
    {


        public static unsafe ModelParams<TPFinder, TRFinder> BuildJobParams<TPFinder, TRFinder>(
            this Animator anim, TransformMappings bones, TPFinder pkf, TRFinder rkf,
            float delayTime = 0)
                where TPFinder : unmanaged, IKeyFinderWithoutProcedure<float4>
                where TRFinder : unmanaged, IKeyFinderWithoutProcedure<quaternion>
        =>
            anim.BuildJobParams(bones, pkf, rkf,
                anim.ToVmdBodyTransformMotionOperator(bones),
                anim.ToVmdFootIkTransformOperator(bones).WithIkUsage(pkf, rkf, VmdFootIkMode.auto),
                delayTime);


        public static unsafe ModelParams<TPFinder, TRFinder> BuildJobParams<TPFinder, TRFinder>(
            this Animator anim, TransformMappings bones, TPFinder pkf, TRFinder rkf,
            VmdBodyMotionOperator<TransformMappings, Tf> bodyop,
            float delayTime = 0)
                where TPFinder : unmanaged, IKeyFinderWithoutProcedure<float4>
                where TRFinder : unmanaged, IKeyFinderWithoutProcedure<quaternion>
        =>
            anim.BuildJobParams(bones, pkf, rkf,
                bodyop,
                anim.ToVmdFootIkTransformOperator(bones).WithIkUsage(pkf, rkf, VmdFootIkMode.auto),
                delayTime);


        public static unsafe ModelParams<TPFinder, TRFinder> BuildJobParams<TPFinder, TRFinder>(
            this Animator anim, TransformMappings bones, TPFinder pkf, TRFinder rkf,
            VmdFootIkOperator<Tf> footop,
            float delayTime = 0)
                where TPFinder : unmanaged, IKeyFinderWithoutProcedure<float4>
                where TRFinder : unmanaged, IKeyFinderWithoutProcedure<quaternion>
        =>
            anim.BuildJobParams(bones, pkf, rkf,
                anim.ToVmdBodyTransformMotionOperator(bones),
                footop,
                delayTime);


        /// <summary>
        /// ÉÇÉfÉãÇPëÃÇÃÉpÉâÉÅÅ[É^Çç\ízÇ∑ÇÈ
        /// </summary>
        public static unsafe ModelParams<TPFinder, TRFinder> BuildJobParams<TPFinder, TRFinder>(
            this Animator anim, TransformMappings bones, TPFinder pkf, TRFinder rkf,
            VmdBodyMotionOperator<TransformMappings, Tf> bodyop, VmdFootIkOperator<Tf> footop,
            float delayTime = 0)
                where TPFinder : unmanaged, IKeyFinderWithoutProcedure<float4>
                where TRFinder : unmanaged, IKeyFinderWithoutProcedure<quaternion>
        {

            var result = new ModelParams<TPFinder, TRFinder>();
            var timer = new StreamingTimer(rkf.Streams.GetLastKeyTime());


            // model
            result.model_finders_origin =
                new ModelFinder<TPFinder, TRFinder>
                {
                    pos = pkf,
                    rot = rkf,
                }
                .WrapEnumerable();
            result.model_boneOptions =
                new ModelBoneOption
                {
                    option = bones[0].option,
                }
                .WrapEnumerable();
            result.model_hipAdjusts.invoke = p =>
                new ModelHipBoneAdjust
                {
                    hiprot_index = p.rot_offset + 1,
                    rootToHipLocal = bodyop.rootToHipLocal,
                    spineToHipLocal = bodyop.spineToHipLocal,
                }
                .WrapEnumerable();
            result.model_timeOptions =
                new ModelTimer
                {
                    timer = timer,
                    //previousTime = 0.0f,
                    //nextTime = 0.0f,
                    delayTime = delayTime,
                    indexBlockTimeRange = rkf.IndexBlockTimeRange,
                    speedHint = 1.0f,
                }
                .WrapEnumerable();


            // ê⁄ín
            // ÅEÇhÇjÇ†ÇË
            //      - ë´à íuÅAë´å¸Ç´ÇÇhÇjÉXÉgÉäÅ[ÉÄÇ©ÇÁéÊìæ Å® ÉèÅ[ÉãÉhïœä∑ Å® ê⁄ínï‚ê≥ Å® ÇhÇjèàóù
            // ÅEÇhÇjÇ»Çµ
            //      - ë´à íuÅAë´å¸Ç´ÇÇeÇjÉXÉgÉäÅ[ÉÄÇ©ÇÁéÊìæ Å® ÉèÅ[ÉãÉhïœä∑ Å® ê⁄ínï‚ê≥ Å® ÇhÇjèàóù

            if (footop.useGroundHit | footop.useLegPositionIk | footop.useFootRotationIk)
            {

                result.ikalways_baseTransforms = anim.transform
                    .WrapEnumerable();


                result.ikalways_legTransforms = new[]
                {
                    anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg),
                    anim.GetBoneTransform(HumanBodyBones.RightUpperLeg),
                    anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg),
                    anim.GetBoneTransform(HumanBodyBones.RightLowerLeg),
                    anim.GetBoneTransform(HumanBodyBones.LeftFoot),
                    anim.GetBoneTransform(HumanBodyBones.RightFoot),
                };

                result.ikalways_ikAnchorIndices.invoke = p =>
                    new SolveIkAnchorIndex
                    {
                        legalways_ikAnchorIndex =
                            footop.useGroundHit | footop.useLegPositionIk
                                ? p.legalways_offset
                                : -1,
                        ikfoot_ikAnchorIndex =
                            footop.useFootRotationIk
                                ? p.footik_offset
                                : -1,
                    }
                    .WrapEnumerable();
            }


            if (footop.useGroundHit)
            {
                result.ground_hitData.invoke = p =>
                    new LegHitData
                    {
                        model_index = p.model_offset,
                        ikalways_index = p.ikalways_offset,
                        legalways_index = p.legalways_offset,
                        //footalways_index = p.footalways_offset,
                        hitMask = footop.groundHitMask,
                        ankleHightL = footop.footIkOffsetL.y,
                        ankleHightR = footop.footIkOffsetR.y,
                        rayDistance = footop.groundHitDistance + footop.groundHitOriginOffset,
                        rayOriginOffset = footop.groundHitOriginOffset,
                    }
                    .WrapEnumerable();

                result.ground_rootTransforms =
                    bones[0].human.TransformHandle.tf
                    //anim.GetBoneTransform(HumanBodyBones.Hips)
                    .WrapEnumerable();

                var pkf_ = pkf.With<float4, TPFinder, Forward>(new StreamingTimer());
                var rkf_ = rkf.With<quaternion, TRFinder, Forward>(new StreamingTimer());
                var rootlpos = pkf_.AccumulateStreamPosition(rkf_, HumanBodyBones.LastBone);
                var tfbase = anim.transform;
                var rootwpos = tfbase.TransformPoint(rootlpos);
                var origin_offset = footop.groundHitOriginOffset * tfbase.up;
                var isGround = Physics.Raycast(rootwpos + origin_offset, -tfbase.up, out var hit, footop.groundHitDistance + footop.groundHitOriginOffset, footop.groundHitMask);
                var height = math.select(0.0f, math.dot(hit.point - rootwpos, tfbase.up), isGround);
                result.ground_rootHeights = new GroundLegInterpolationStorageLR
                {
                    rootLocalHeight = height,
                }
                .WrapEnumerable();


                result.ground_footStorages = new GroundFootInterpolationStorageLR { }
                    .WrapEnumerable();
                result.ground_footFkTransforms = new[]
                {
                    anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg),
                    anim.GetBoneTransform(HumanBodyBones.RightLowerLeg),
                    anim.GetBoneTransform(HumanBodyBones.LeftFoot),
                    anim.GetBoneTransform(HumanBodyBones.RightFoot),
                };
                result.ground_footResultTransforms = new[]
                {
                    anim.GetBoneTransform(HumanBodyBones.LeftFoot),
                    anim.GetBoneTransform(HumanBodyBones.RightFoot),
                };
            }


            if (footop.useLegPositionIk)
            {
                result.ikleg_ikData.invoke = p =>
                    new LegIkData
                    {
                        model_index = p.model_offset,
                        legalways_index = p.legalways_offset,
                        ikalways_index = p.ikalways_offset,
                        footPosOffsetL = footop.footIkOffsetL.As4(1.0f),
                        footPosOffsetR = footop.footIkOffsetR.As4(1.0f),
                        MoveToUnscale = footop._MoveToUnscale.As4(1.0f),
                        footScale = footop.footScale.As4(1.0f),
                        //rootLocalPositionIndex = p.model_offset,
                    }
                    .WrapEnumerable();
            }


            if (footop.useFootRotationIk)
            {
                result.ikfoot_ikData.invoke = p =>
                    new FootIkData
                    {
                        model_index = p.model_offset,
                        ikalways_index = p.ikalways_offset,
                        ikfoot_index = p.footik_offset,//p.footalways_offset,
                    }
                    .WrapEnumerable();
            }



            if (!footop.useLegPositionIk & footop.useGroundHit)
            {
                result.noikleg_footTransforms = new[]
                {
                    anim.GetBoneTransform(HumanBodyBones.LeftFoot),
                    anim.GetBoneTransform(HumanBodyBones.RightFoot),
                };

                result.noikleg_ikIndices.invoke = p => Enumerable.Range(0, 2)
                    .Select(i => new LegIkAnchorIndex
                    {
                        legalways_ikAnchorIndex = p.legalways_offset * 2 + i,
                    });
            }


            //if (!footop.useFootRotationIk & footop.useGroundHit)
            //{
            //    result.noikfoot_footTransforms = new[]
            //    {
            //        anim.GetBoneTransform(HumanBodyBones.LeftFoot),
            //        anim.GetBoneTransform(HumanBodyBones.RightFoot),
            //    };

            //    result.noikfoot_ikIndices.invoke = p => Enumerable.Range(0, 2)
            //        .Select(i => new FootIkAnchorIndex
            //        {
            //            footalways_ikAnchorIndex = p.footalways_offset * 2 + i,
            //        });
            //}



            // root, hip, other
            result.bonefull_rotIndices.invoke = p => Enumerable.Range(0, bones.BoneLength)
                .Select(x => new BoneIndexData
                {
                    model_index = p.model_offset,
                    HumanBoneId = bones[x].human.HumanBoneId,
                    StreamId = bones[x].human.ToVmd().StreamId,
                });

            // root
            result.boneroot_posIndices.invoke = p =>
                new BoneIndexData
                {
                    model_index = p.model_offset,
                    HumanBoneId = bones[0].human.HumanBoneId,
                    StreamId = bones[0].human.ToVmd().StreamId,
                }
                .WrapEnumerable();
            // hip
            result.bonehip_posIndices.invoke = p =>
                new BoneIndexData
                {
                    model_index = p.model_offset,
                    HumanBoneId = bones[1].human.HumanBoneId,
                    StreamId = bones[1].human.ToVmd().StreamId,
                }
                .WrapEnumerable();


            // root, hip, other
            result.bonefull_rotOffsets = Enumerable.Range(0, bones.BoneLength)
                .Select(i => new BoneRotationOffsetPose
                {
                    rotationInitial = bones[i].initpose,
                });

            // root, hip, other
            result.bonefull_transgorms = Enumerable.Range(0, bones.BoneLength)
                .Select(i => bones[i].human.TransformHandle.tf);

            // root, hip, other
            result.bonefull_transformApplyIndices.invoke = p => Enumerable.Range(0, bones.BoneLength)
                .Select(i => new BoneTransformApplyIndex
                {
                    // root:0, hip:1 ÇÃÇ› position ÇïKóvÇ∆Ç∑ÇÈ
                    pos_index = i switch
                    {
                        0 => p.model_offset,
                        1 => p.model_total_length + p.model_offset,
                        _ => -1,
                    },
                });


            // root
            result.boneroot_posScales =
                new BodyBoneScale
                {
                    scale = bodyop.moveScale.As4(1.0f),
                }
                .WrapEnumerable();
            // hip
            result.bonehip_posScales =
                new BodyBoneScale
                {
                    scale = bodyop.bodyScale.As4(1.0f),
                }
                .WrapEnumerable();

            result.model_data =
                new ModelParams<TPFinder, TRFinder>.ModelData
                {
                    anim = anim,
                    //bones = bodyop.bones,
                    //foottf = footop.tf,
                    bodyop = bodyop,
                    footop = footop,
                };

            return result;
        }


    }
}
