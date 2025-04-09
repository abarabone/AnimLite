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

namespace AnimLite.Vmd.experimental.Job
{
    using AnimLite.Vmd;
    using AnimLite.Vmd.experimental.Data;
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;
    using AnimLite.IK;



    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct SolveIkJob : IJobParallelFor
    {

        [ReadOnly]
        public NativeArray<LegIkAnchorLR> legalways_ikAnchors;
        
        [ReadOnly]
        public NativeArray<FootIkAnchorLR> footalways_ikAnchors;


        // r/w
        public NativeArray<SolveIkTransformValueSet> ikalways_legTransformValueSets;
        [ReadOnly]
        public NativeArray<SolveIkAnchorIndex> ikalways_ikAnchorIndices;


        public void Execute(int ikalways_index)
        {
            var stream = new ValueStreamSource { };

            var legvalue = this.ikalways_legTransformValueSets[ikalways_index];
            var i = this.ikalways_ikAnchorIndices[ikalways_index];


            if (Hint.Likely(i.legalways_ikAnchorIndex != -1))
            {
                var ikleg = this.legalways_ikAnchors[i.legalways_ikAnchorIndex];

                stream.SolveTwoBonePairIk(
                    ref legvalue.ULegRotL, ref legvalue.LLegRotL,
                    legvalue.ULegPosL, legvalue.LLegPosL, legvalue.FootPosL,
                    ikleg.legWorldPositionL.As3(),
                    ref legvalue.ULegRotR, ref legvalue.LLegRotR,
                    legvalue.ULegPosR, legvalue.LLegPosR, legvalue.FootPosR,
                    ikleg.legWorldPositionR.As3());
            }

            if (Hint.Likely(i.footalways_ikAnchorIndex != -1))
            {
                var ikfoot = this.footalways_ikAnchors[i.footalways_ikAnchorIndex];

                legvalue.FootRotL.rot = ikfoot.footWorldRotationL;
                legvalue.FootRotR.rot = ikfoot.footWorldRotationR;
            }


            this.ikalways_legTransformValueSets[ikalways_index] = legvalue;
        }
    }




    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct SolveCopyFromTransformJob : IJobParallelForTransform
    {

        [WriteOnly]
        public NativeArray<SolveIkAppliedTransformValue> ikalways_legTransformValues;


        public void Execute(int index, TransformAccess tf)
        {
            tf.GetPositionAndRotation(out var pos, out var rot);

            this.ikalways_legTransformValues[index] = new SolveIkAppliedTransformValue
            {
                pos = pos.As_float4(1.0f),
                rot = rot,
            };
        }
    }

    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct SolveCopyToTransformJob : IJobParallelForTransform
    {

        [ReadOnly]
        public NativeArray<SolveIkAppliedTransformValue> ikalways_legTransformValues;


        public void Execute(int index, TransformAccess tf)
        {
            //if (index > 3) return;
            var value = this.ikalways_legTransformValues[index];

            tf.rotation = value.rot;
            //tf.SetPositionAndRotation(value.pos.As3(), value.rot);
        }
    }



}
