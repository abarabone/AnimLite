using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.UIElements;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Burst.CompilerServices;
using UnityEngine.Jobs;

using AnimLite.Vmd.experimental.Job;
using AnimLite;

[assembly: RegisterGenericJobType(typeof(BodyPositionJob<
    AnimLite.KeyFinderWithoutProcedure<float4, Key4CatmulPos, Clamp, Key4StreamCache<float4>, StreamIndex>,
    AnimLite.KeyFinderWithoutProcedure<quaternion, Key4CatmulRot, Clamp, Key4StreamCache<quaternion>, StreamIndex>>))]

[assembly: RegisterGenericJobType(typeof(BodyRotationJob<
    AnimLite.KeyFinderWithoutProcedure<float4, Key4CatmulPos, Clamp, Key4StreamCache<float4>, StreamIndex>,
    AnimLite.KeyFinderWithoutProcedure<quaternion, Key4CatmulRot, Clamp, Key4StreamCache<quaternion>, StreamIndex>>))]


namespace AnimLite.Vmd.experimental.Job
{
    using AnimLite.Vmd;
    using AnimLite.Vmd.experimental.Data;
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;



    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct UpdateTimeAndProcedureSelectorJob : IJobParallelFor
    {

        // r/w
        public NativeArray<ModelTimer> model_timer;

        [WriteOnly]
        public NativeArray<ModelProcedureSelector> model_procedureSelectors;


        public float deltaTime;


        public void Execute(int index)
        {
            var t = this.model_timer[index];
            
            t.previousTime = t.timer._curret_time_inner;
            t.timer.ProceedTime(this.deltaTime);

            var currentTime = t.timer.CurrentTime;
            var previousTime = t.previousTime;
            var blockRange = t.indexBlockTimeRange;
            this.model_procedureSelectors[index] = new ModelProcedureSelector
            {
                isForward = (previousTime <= currentTime) & (currentTime <= previousTime + blockRange),
            };

            this.model_timer[index] = t;
        }
    }




    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct BodyRotationJob<TPFinder, TRFinder> : IJobParallelFor
        where TPFinder : unmanaged, IKeyFinderWithoutProcedure<float4>
        where TRFinder : unmanaged, IKeyFinderWithoutProcedure<quaternion>
    {

        [ReadOnly]
        //[NativeDisableParallelForRestriction]
        //[NativeDisableContainerSafetyRestriction]
        //[NativeDisableUnsafePtrRestriction]
        public NativeArray<ModelFinderReference<TPFinder, TRFinder>> model_finders;
        [ReadOnly]
        public NativeArray<ModelBoneOption> model_boneOptions;
        [ReadOnly]
        public NativeArray<ModelProcedureSelector> model_procedureSelectors;


        [ReadOnly]
        public NativeArray<BoneIndexData> bonefull_rotIndices;
        [ReadOnly]
        public NativeArray<BoneRotationOffsetPose> bonefull_rotOffsets;
        [WriteOnly]
        public NativeArray<BodyBoneLocalRotationResult> bonefull_rotResults;

        [ReadOnly]
        public NativeArray<ModelTimer> model_timer;


        public void Execute(int index)
        {
            var i = this.bonefull_rotIndices[index];
            var inirot = this.bonefull_rotOffsets[index];
            var option = this.model_boneOptions[i.model_index].option;
            var timer = this.model_timer[i.model_index].timer;
            //Debug.Log($"{index}:{bone.humanReference.HumanBoneId}/{bone.humanReference.StreamId}");

            var isForward = this.model_procedureSelectors[i.model_index].isForward;
            var finder = this.model_finders[i.model_index];

            var _lrot = accumulate_(timer);
            this.bonefull_rotResults[index] = new BodyBoneLocalRotationResult
            {
                localRotation = inirot.rotationInitial.RotateBone(_lrot),
            };
            return;

            quaternion accumulate_(StreamingTimer timer)
            {
                if (Hint.Likely(isForward))
                {
                    var rot = finder.rotWith<Forward>(timer);

                    return rot.AccumulateStreamRotation(option, i.HumanBoneId, i.StreamId);
                }
                else
                {
                    var rot = finder.rotWith<Absolute>(timer);

                    return rot.AccumulateStreamRotation(option, i.HumanBoneId, i.StreamId);
                }
                //    switch (isForward)
                //    {
                //        case true:
                //            {
                //                var rot = finder.rotWith<Forward>(timer);

                //                return rot.AccumulateStreamRotation(option, i.HumanBoneId, i.StreamId);
                //            }
                //        case false:
                //            {
                //                var rot = finder.rotWith<Absolute>(timer);

                //                return rot.AccumulateStreamRotation(option, i.HumanBoneId, i.StreamId);
                //            }
                //    };
            }
        }
    }

    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct BodyPositionJob<TPFinder, TRFinder> : IJobParallelFor
        where TPFinder : unmanaged, IKeyFinderWithoutProcedure<float4>
        where TRFinder : unmanaged, IKeyFinderWithoutProcedure<quaternion>
    {

        [ReadOnly]
        //[NativeDisableParallelForRestriction]
        //[NativeDisableContainerSafetyRestriction]
        //[NativeDisableUnsafePtrRestriction]
        public NativeArray<ModelFinderReference<TPFinder, TRFinder>> model_finders;
        [ReadOnly]
        public NativeArray<ModelBoneOption> model_boneOptions;
        [ReadOnly]
        public NativeArray<ModelProcedureSelector> model_procedureSelectors;


        [ReadOnly]
        public NativeArray<BoneIndexData> boneroothip_posIndices;
        [ReadOnly]
        public NativeArray<BodyBoneScale> bodyroothip_posScales;
        [WriteOnly]
        public NativeArray<BodyBoneLocalPositionResult> boneroothip_posResults;

        [ReadOnly]
        public NativeArray<ModelTimer> model_timer;


        public void Execute(int index)
        {
            var i = this.boneroothip_posIndices[index];
            var option = this.model_boneOptions[i.model_index].option;
            var timer = this.model_timer[i.model_index].timer;

            var isForward = this.model_procedureSelectors[i.model_index].isForward;
            var finder = this.model_finders[i.model_index];

            var _lpos = accumulate_(timer);
            this.boneroothip_posResults[index] = new BodyBoneLocalPositionResult
            {
                localPosition = new float4(_lpos, 1.0f) * this.bodyroothip_posScales[index].scale,
            };
            return;

            float3 accumulate_(StreamingTimer timer)
            {
                if (Hint.Likely(isForward))
                {
                    var (pos, rot) = (finder.posWith<Forward>(timer), finder.rotWith<Forward>(timer));

                    return pos.AccumulateStreamPosition(rot, i.HumanBoneId);
                }
                else
                {
                    var (pos, rot) = (finder.posWith<Absolute>(timer), finder.rotWith<Absolute>(timer));

                    return pos.AccumulateStreamPosition(rot, i.HumanBoneId);
                }
            }
        }
    }


    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct BodyHipPositionAdjustJob : IJobParallelFor
    {

        [ReadOnly]
        public NativeArray<ModelHipBoneAdjust> model_hipAdjusts;

        public NativeSlice<BodyBoneLocalPositionResult> bodyhip_posResults;


        [ReadOnly]
        public NativeArray<BodyBoneLocalRotationResult> bonefull_rotResults;


        public void Execute(int index)
        {
            var adjust = this.model_hipAdjusts[index];

            var lrot = this.bonefull_rotResults[adjust.hiprot_index].localRotation;
            var hipHeight = adjust.rootToHipLocal.As4();
            var hipAdjust = math.rotate(lrot, adjust.spineToHipLocal).AsXZ().As4();

            var hiplpos_prev = this.bodyhip_posResults[index].localPosition;

            this.bodyhip_posResults[index] = new BodyBoneLocalPositionResult
            {
                localPosition = hiplpos_prev + hipHeight + hipAdjust,
            };
        }
    }





    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct ApplyBodyTransformJob : IJobParallelForTransform
    {

        [ReadOnly]
        public NativeArray<BoneTransformApplyIndex> bonefull_transformApplyIndices;
        [ReadOnly]
        public NativeArray<BodyBoneLocalRotationResult> bonefull_rotResults;

        [ReadOnly]
        public NativeArray<BodyBoneLocalPositionResult> boneroothip_posResults;



        public void Execute(int index_bonefull, TransformAccess tf)
        {
            var i = this.bonefull_transformApplyIndices[index_bonefull];

            var rot = this.bonefull_rotResults[index_bonefull].localRotation;

            if (Hint.Unlikely(i.pos_index >= 0))
            {
                var pos = this.boneroothip_posResults[i.pos_index].localPosition.As3();
                
                tf.SetLocalPositionAndRotation(pos, rot);

                return;
            }

            tf.localRotation = rot;
        }
    }

}
