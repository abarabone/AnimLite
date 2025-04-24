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
        public int footalways_index;

        public LayerMask hitMask;
        public float ankleHightL;
        public float ankleHightR;
        public float rayDistance;
        public float rayOriginOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LegHitCastCommandLR
    {
        //public SpherecastCommand commandL;
        //public SpherecastCommand commandR;
        public RaycastCommand commandL;
        public RaycastCommand commandR;
    }



    //[StructLayout(LayoutKind.Sequential)]
    [StructLayout(LayoutKind.Explicit)]
    public struct LegHitInterpolationStorage
    {
        [FieldOffset(0)]
        public float footLocalHeightL;
        [FieldOffset(4)]
        public float footLocalHeightR;
        [FieldOffset(0)]
        public float2 footLocalHeightLR;

        [FieldOffset(8)]
        public float rootLocalHeight;

        [FieldOffset(16)]
        public quaternion footRotationL;
        [FieldOffset(16 + 16)]
        public quaternion footRotationR;

        [FieldOffset(48)]
        public float3 easeSpeed;
        [FieldOffset(48 + 16)]
        int pad0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LegHitRaycastHitLR
    {
        public RaycastHit hitL;
        public RaycastHit hitR;
    }




}
