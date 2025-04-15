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



    [StructLayout(LayoutKind.Sequential)]
    public struct LegHitRootHeightStorage
    {
        public float rootHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LegHitRaycastHitLR
    {
        public RaycastHit hitL;
        public RaycastHit hitR;
    }




}
