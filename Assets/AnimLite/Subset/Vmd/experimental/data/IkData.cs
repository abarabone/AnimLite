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
using Unity.Burst.Intrinsics;
using UnityEngine.Jobs;

namespace AnimLite.Vmd.experimental.Data
{
    using AnimLite.Vmd;
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;
    using AnimLite.IK;




    [StructLayout(LayoutKind.Sequential)]
    public struct IkBaseTransformValue
    {
        public quaternion rotation;
        public quaternion rotation_inv;

        public float4 position;
        public float4 scale;
        public float4 worldUp;
    }




    [StructLayout(LayoutKind.Sequential)]
    public struct LegIkData
    {
        public float4 footPosOffsetL;
        public float4 footPosOffsetR;

        public float4 footScale;
        public float4 MoveToUnscale;

        public int model_index;
        public int ikalways_index;
        public int legalways_index;

        int pad0;
        int pad1;
        int pad2;
    }

    public struct LegIkAnchorIndex
    {
        public int legalways_ikAnchorIndex;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct LegIkAnchorLR
    {
        public float4 legWorldPositionL;
        public float4 legWorldPositionR;
    }
    public struct LegIkAnchor
    {
        public float4 legWorldPosition;
    }






    [StructLayout(LayoutKind.Sequential)]
    public struct FootIkData
    {
        public int model_index;
        public int ikalways_index;
        public int ikfoot_index;
    }

    public struct FootIkAnchorIndex
    {
        public int footalways_ikAnchorIndex;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct FootIkAnchorLR
    {
        public quaternion footWorldRotationL;
        public quaternion footWorldRotationR;
    }
    public struct FootIkAnchor
    {
        public quaternion footWorldRotation;
    }





    [StructLayout(LayoutKind.Sequential)]
    public struct SolveIkTransformValueSet
    {
        public TfPosision ULegPosL;
        public TfRotation ULegRotL;
        public TfPosision ULegPosR;
        public TfRotation ULegRotR;

        public TfPosision LLegPosL;
        public TfRotation LLegRotL;
        public TfPosision LLegPosR;
        public TfRotation LLegRotR;

        public TfPosision FootPosL;
        public TfRotation FootRotL;
        public TfPosision FootPosR;
        public TfRotation FootRotR;
    }

    public static class SolveIkTransformValueSetExtension
    {
        public static SolveIkTransformValueSet SetNanToLeg(this SolveIkTransformValueSet value)
        {
            var s = new ValueStreamSource { };
            value.ULegRotL.SetRotation(s, new float4(float.NaN, float.NaN, float.NaN, float.NaN));
            value.ULegRotR.SetRotation(s, new float4(float.NaN, float.NaN, float.NaN, float.NaN));
            value.LLegRotL.SetRotation(s, new float4(float.NaN, float.NaN, float.NaN, float.NaN));
            value.LLegRotR.SetRotation(s, new float4(float.NaN, float.NaN, float.NaN, float.NaN));
            return value;
        }
        public static SolveIkTransformValueSet SetNanToFoot(this SolveIkTransformValueSet value)
        {
            var s = new ValueStreamSource { };
            value.FootRotL.SetRotation(s, new float4(float.NaN, float.NaN, float.NaN, float.NaN));
            value.FootRotR.SetRotation(s, new float4(float.NaN, float.NaN, float.NaN, float.NaN));
            return value;
        }
    }









    [StructLayout(LayoutKind.Sequential)]
    public struct SolveIkAppliedTransformValue
    {
        public float4 pos;
        public quaternion rot;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct SolveIkAnchorIndex
    {
        public int legalways_ikAnchorIndex;
        public int ikfoot_ikAnchorIndex;
    }






}
