using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.UIElements;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using UnityEngine.Jobs;

using AnimLite.Vmd.experimental;
using AnimLite;
using AnimLite.Vmd.experimental.Job;

[assembly: RegisterGenericJobType(typeof(GetLegIkAnchorJob<
    AnimLite.KeyFinderWithoutProcedure<float4, Key4CatmulPos, Clamp, Key4StreamCache<float4>, StreamIndex>,
    AnimLite.KeyFinderWithoutProcedure<quaternion, Key4CatmulRot, Clamp, Key4StreamCache<quaternion>, StreamIndex>>))]

[assembly: RegisterGenericJobType(typeof(GetFootIkAnchorJob<
    AnimLite.KeyFinderWithoutProcedure<float4, Key4CatmulPos, Clamp, Key4StreamCache<float4>, StreamIndex>,
    AnimLite.KeyFinderWithoutProcedure<quaternion, Key4CatmulRot, Clamp, Key4StreamCache<quaternion>, StreamIndex>>))]


namespace AnimLite.Vmd.experimental.Job
{
    using AnimLite.Vmd;
    using AnimLite.Vmd.experimental.Data;
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;
    using AnimLite.IK;




    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct IkBaseTransformCopyJob : IJobParallelForTransform
    {

        [WriteOnly]
        public NativeArray<IkBaseTransformValue> ikalways_baseTransformValues;


        public void Execute(int index_ikalways, TransformAccess tf)
        {
            var rot = tf.rotation;

            this.ikalways_baseTransformValues[index_ikalways] = new IkBaseTransformValue
            {
                rotation = rot,
                rotation_inv = math.inverse(rot),

                position = tf.position.As_float3().As4(1.0f),
                worldUp = math.rotate(rot, Vector3.up).As4(1.0f),
            };
        }
    }





    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct GetLegIkAnchorJob<TPFinder, TRFinder> : IJobParallelFor
        where TPFinder : unmanaged, IKeyFinderWithoutProcedure<float4>
        where TRFinder : unmanaged, IKeyFinderWithoutProcedure<quaternion>
    {

        [ReadOnly]
        public NativeArray<LegIkData> ikleg_ikData;

        [WriteOnly]
        public NativeArray<LegIkAnchorLR> legalways_ikAnchors;


        [ReadOnly]
        public NativeArray<ModelFinderReference<TPFinder, TRFinder>> model_finders;
        [ReadOnly]
        public NativeArray<ModelProcedureSelector> model_procedureSelectors;
        [ReadOnly]
        public NativeArray<ModelTimer> model_timer;


        [ReadOnly]
        public NativeArray<BodyBoneLocalPositionResult> boneroothip_posResults;

        [ReadOnly]
        public NativeArray<IkBaseTransformValue> ikalways_baseTransformValues;



        public void Execute(int ikleg_index)
        {
            var ikdata = this.ikleg_ikData[ikleg_index];

            var finder = this.model_finders[ikdata.model_index];
            var timer = this.model_timer[ikdata.model_index].timer;
            var isForward = this.model_procedureSelectors[ikdata.model_index].isForward;

            var tfBase = this.ikalways_baseTransformValues[ikdata.ikalways_index];


            //var rootlpos_move = this.localPositions[ikdata.rootLocalPositionIndex].localPosition.As3();
            var rootlpos_move = this.boneroothip_posResults[ikdata.model_index].localPosition.As3();
            var rootpos_foot = rootlpos_move * ikdata.footPerMoveScale;
            var (lposL, lposR) = get_(timer);

            var iklposL = lposL * ikdata.footScale * 0.1f - rootpos_foot;
            var iklposR = lposR * ikdata.footScale * 0.1f - rootpos_foot;

            var ikPosL = iklposL + ikdata.footPosOffsetL.As3() + rootlpos_move;
            var ikPosR = iklposR + ikdata.footPosOffsetR.As3() + rootlpos_move;


            var basewpos = tfBase.position.As3();
            var basewrot = tfBase.rotation;
            var footposL = math.rotate(basewrot, ikPosL) + basewpos;
            var footposR = math.rotate(basewrot, ikPosR) + basewpos;

            this.legalways_ikAnchors[ikdata.legalways_index] = new LegIkAnchorLR
            {
                legWorldPositionL = footposL.As4(1.0f),
                legWorldPositionR = footposR.As4(1.0f),
            };
            return;


            (float3, float3) get_(StreamingTimer timer)
            {
                if (Hint.Likely(isForward))
                {
                    var pkf = finder.posWith<Forward>(timer);
                    return (pkf.getpos(MmdBodyBones.ç∂ë´ÇhÇj), pkf.getpos(MmdBodyBones.âEë´ÇhÇj));
                }
                else
                {
                    var pkf = finder.posWith<Absolute>(timer);
                    return (pkf.getpos(MmdBodyBones.ç∂ë´ÇhÇj), pkf.getpos(MmdBodyBones.âEë´ÇhÇj));
                }
                //switch (isForward)
                //{
                //    case true:
                //        {
                //            var pkf = finder.posWith<Forward>(timer);
                //            return (pkf.getpos(MmdBodyBones.ç∂ë´ÇhÇj), pkf.getpos(MmdBodyBones.âEë´ÇhÇj));
                //        }
                //    case false:
                //        {
                //            var pkf = finder.posWith<Absolute>(timer);
                //            return (pkf.getpos(MmdBodyBones.ç∂ë´ÇhÇj), pkf.getpos(MmdBodyBones.âEë´ÇhÇj));
                //        }
                //};
            }
        }
    }




    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct GetFootIkAnchorJob<TPFinder, TRFinder> : IJobParallelFor
        where TPFinder : unmanaged, IKeyFinderWithoutProcedure<float4>
        where TRFinder : unmanaged, IKeyFinderWithoutProcedure<quaternion>
    {

        [ReadOnly]
        public NativeArray<FootIkData> ikfoot_ikData;
        [WriteOnly]
        public NativeArray<FootIkAnchorLR> footalways_ikAnchors;


        [ReadOnly]
        public NativeArray<ModelFinderReference<TPFinder, TRFinder>> model_finders;
        [ReadOnly]
        public NativeArray<ModelProcedureSelector> model_procedureSelectors;
        [ReadOnly]
        public NativeArray<ModelTimer> model_timer;


        [ReadOnly]
        public NativeArray<IkBaseTransformValue> ikalways_baseTransformValues;



        public void Execute(int index)
        {
            var ikdata = this.ikfoot_ikData[index];

            var finder = this.model_finders[ikdata.model_index];
            var timer = this.model_timer[ikdata.model_index].timer;
            var isForward = this.model_procedureSelectors[ikdata.model_index].isForward;

            var (lrotL, lrotR) = get_(timer);

            var tfBase = this.ikalways_baseTransformValues[ikdata.ikalways_index];

            var basewrot = tfBase.rotation;

            this.footalways_ikAnchors[ikdata.footalways_index] = new FootIkAnchorLR
            {
                footWorldRotationL = math.mul(basewrot, lrotL),
                footWorldRotationR = math.mul(basewrot, lrotR),
            };
            return;


            (quaternion, quaternion) get_(StreamingTimer timer)
            {
                if (Hint.Likely(isForward))
                {
                    var rkf = finder.rotWith<Forward>(timer);
                    return (rkf.getrot(MmdBodyBones.ç∂ë´ÇhÇj), rkf.getrot(MmdBodyBones.âEë´ÇhÇj));
                }
                else
                {
                    var rkf = finder.rotWith<Forward>(timer);
                    return (rkf.getrot(MmdBodyBones.ç∂ë´ÇhÇj), rkf.getrot(MmdBodyBones.âEë´ÇhÇj));
                }
            }
        }
    }



}
