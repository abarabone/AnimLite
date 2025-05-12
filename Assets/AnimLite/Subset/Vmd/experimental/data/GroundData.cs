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





    //[StructLayout(LayoutKind.Sequential)]
    //public struct LegIkHitIndex
    //{
    //    public int leghitL_index;
    //    public int leghitR_index;
    //}

    [StructLayout(LayoutKind.Sequential)]
    public struct LegHitData
    {
        public int model_index;
        public int ikalways_index;
        public int legalways_index;
        //public int footalways_index;

        public LayerMask hitMask;
        public float ankleHightL;
        public float ankleHightR;
        public float rayDistance;
        public float rayOriginOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LegGroundcastCommandLR
    {
        public SpherecastCommand commandL;
        public SpherecastCommand commandR;
        //public RaycastCommand commandL;
        //public RaycastCommand commandR;
    }



    //[StructLayout(LayoutKind.Sequential)]
    [StructLayout(LayoutKind.Explicit)]
    public struct GroundLegInterpolationStorageLR
    {
        [FieldOffset(0)]
        public float footLocalHeightL;
        [FieldOffset(4)]
        public float footLocalHeightR;
        [FieldOffset(0)]
        public float2 legLocalHeightLR;

        [FieldOffset(8)]
        public float rootLocalHeight;

        [FieldOffset(12)]
        public float localGroundHeight;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LegRaycastHitLR
    {
        public RaycastHit hitL;
        public RaycastHit hitR;
    }






    [StructLayout(LayoutKind.Sequential)]
    public struct GroundFootInterpolationStorageLR
    {
        public quaternion footRotationL;
        public quaternion footRotationR;
    }


    //[StructLayout(LayoutKind.Sequential)]
    //public struct GroundFootFkIndex
    //{
    //    public int gr_index;
    //}

    [StructLayout(LayoutKind.Sequential)]
    public struct GroundFootFkTransformValueLR
    {
        public float4 legWorldPositionL;
        public quaternion legWorldRotationL;
        public float4 legWorldPositionR;
        public quaternion legWorldRotationR;

        public float4 footWorldPositionL;
        public quaternion footWorldRotationL;
        public float4 footWorldPositionR;
        public quaternion footWorldRotationR;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct GroundFootFkTransformValue
    {
        public float4 footWorldPosition;
        public quaternion footWorldRotation;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct GroundFootResultValueLR
    {
        public quaternion footWorldRotationL;
        public quaternion footWorldRotationR;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct GroundFootResultValue
    {
        public quaternion footWorldRotation;
    }
}
