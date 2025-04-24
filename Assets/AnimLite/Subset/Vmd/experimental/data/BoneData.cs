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
using UnityEngine.Jobs;

namespace AnimLite.Vmd.experimental.Data
{
    using AnimLite.Vmd;
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;



    [StructLayout(LayoutKind.Sequential)]
    public struct BoneRotationOffsetPose
    {
        public BoneRotationInitialPose rotationInitial;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct BoneIndexData
    {
        public int model_index;
        
        public HumanBodyBones HumanBoneId;
        public Vmd.MmdBodyBones StreamId;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct BodyBoneScale
    {
        public float4 scale;
    }



    [StructLayout(LayoutKind.Sequential)]
    public struct BodyBoneLocalRotationResult
    {
        public quaternion localRotation;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BodyBoneLocalPositionResult
    {
        public float4 localPosition;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct BoneTransformApplyIndex
    {
        public int pos_index;
    }

}
