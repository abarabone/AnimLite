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

using AnimLite.Vmd.experimental;
using AnimLite;

namespace AnimLite.Vmd.experimental.Data
{
    using AnimLite.Vmd;
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;
    using AnimLite.IK;



    [StructLayout(LayoutKind.Sequential)]
    public struct ModelTimer
    {
        public StreamingTimer timer;
        public float previousTime;
        public float indexBlockTimeRange;
        //public float timeOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ModelProcedureSelector
    {
        public bool isForward;
    }





    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ModelFinder<TPFinder, TRFinder>
        where TPFinder : unmanaged, IKeyFinderWithoutProcedure<float4>
        where TRFinder : unmanaged, IKeyFinderWithoutProcedure<quaternion>
    {
        public TPFinder pos;
        public TRFinder rot;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ModelFinderReference<TPFinder, TRFinder>
        where TPFinder : unmanaged, IKeyFinderWithoutProcedure<float4>
        where TRFinder : unmanaged, IKeyFinderWithoutProcedure<quaternion>
    {

        public TPFinder* p_pos;
        public TRFinder* p_rot;

        public TPFinder pos => *this.p_pos;
        public TRFinder rot => *this.p_rot;



        public KeyFinderProcedureAdapter<float4, TProcedure, TPFinder> posWith<TProcedure>(StreamingTimer timer)
            where TProcedure : IStreamProcedure, new()
        =>
            this.p_pos->With<float4, TPFinder, TProcedure>(timer);

        public KeyFinderProcedureAdapter<quaternion, TProcedure, TRFinder> rotWith<TProcedure>(StreamingTimer timer)
            where TProcedure : IStreamProcedure, new()
        =>
            this.p_rot->With<quaternion, TRFinder, TProcedure>(timer);
    }



    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ModelBoneOption
    {
        public OptionalBoneChecker option;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct ModelHipBoneAdjust
    {
        public float3 rootToHipLocal;
        public int hiprot_index;
        public float3 spineToHipLocal;
        int padding;
    }



}
