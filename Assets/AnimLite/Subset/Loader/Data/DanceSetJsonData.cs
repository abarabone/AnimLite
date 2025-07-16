using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Dynamic;
using UnityEngine;
using UnityEngine.Networking;
using UniVRM10;
using Unity.VisualScripting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace AnimLite.Utility
{
    using AnimLite.DancePlayable;
    using AnimLite.Vmd;
    using AnimLite.Vrm;
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;
    using AnimLite.Geometry;

#nullable enable


    [System.Serializable]
    public class DanceSceneJson : OptionBase
    {
        public AudioDefineJson Audio = new();
        public AnimationDefineJson DefaultAnimation = new();

        public Dictionary<string, ModelDefineJson> BackGrounds = new ();
        public Dictionary<string, DanceMotionDefineJson> Motions = new ();

        public CameraDefineJson Camera = new();

        public InformationDefine AudioInformation = new ();
        public InformationDefine AnimationInformation = new ();
    }
    [System.Serializable]
    public class DanceMotionDefineJson : OptionBase
    {
        public ModelDefineJson Model = new();
        public AnimationDefineJson Animation = new();
        //public MotionOptionsJson Options = new();

        public InformationDefine ModelInformation = new();
        public InformationDefine AnimationInformation = new();
    }

    [System.Serializable]
    public class AnimationDefineJson : OptionBase
    {
        //public PathUnit AnimationFilePath = "";
        public PathList AnimationFilePath = new () { Paths = new PathUnit [] { } };
        public PathUnit FaceMappingFilePath = "";
        public PathUnit BodyAdjustFilePath = "";

        public float DelayTime = 0.0f;
    }
    [System.Serializable]
    public class AudioDefineJson : OptionBase
    {
        public PathUnit AudioFilePath = "";

        public float DelayTime = 0.0f;
        public float Volume = 1.0f;
    }
    [System.Serializable]
    public class ModelDefineJson : OptionBase
    {
        public PathUnit ModelFilePath = "";

        //public bool UsePositionAndDirection = true;
        public numeric3 Position = new numeric3(0.0f);
        public numeric3 EulerAngles = new numeric3(0.0f);
        public float Scale = 1.0f;
    }
    [System.Serializable]
    public class CameraDefineJson : OptionBase
    {
        public PathUnit AnimationFilePath = "";

        public float DelayTime = 0.0f;
    }


    [System.Serializable]
    public class InformationDefine
    {
        public string Caption = "";
        public string Author = "";
        public string Url = "";
        public string Description = "";
    }


    public class OptionBase
    {
        public JObject Options = new();

        public T OptionsAs<T>() where T : new() =>
            this.Options.ToObject<T>(JsonSerializer.Create(JsonSupplemetUtility.DeserializeOptions))!;
    }

    //[System.Serializable]
    //public class BgModelOptionsJson
    //{
    //    public MeshCombineMode MeshCombineMode = MeshCombineMode.None;
    //    public string MeshMaterialName = "";
    //    public string SkinMaterialName = "";
    //}
    //[System.Serializable]
    //public class ChModelOptionsJson
    //{
    //    public MeshCombineMode MeshCombineMode = MeshCombineMode.None;
    //    public string BodyMaterialName = "";
    //    public string FaceMaterialName = "";
    //    public string MeshMaterialName = "";
    //}
    [System.Serializable]
    public class ModelOptionsJson
    {
        public MeshCombineMode MeshCombineMode = MeshCombineMode.None;
        public string MeshMaterialOnSingleMesh = "";
        public string SkinMaterialOnSingleMesh = "";
        public string BlendShapeMaterialOnSingleMesh = "";

        public string[] TargetMeshList = new string[] { };
        public string[] TargetMaterialList = new string[] { };
    }

    [System.Serializable]
    public class AnimationOptionsJson
    {
        public numeric3 BodyScaleFromHuman = new numeric3(0.0f);
        public numeric3 FootScaleFromHuman = new numeric3(0.0f);
        public numeric3 MoveScaleFromHuman = new numeric3(0.0f);

        public VmdFootIkMode FootIkMode = VmdFootIkMode.auto;

        public float GroundHitDistance = 2.0f;
        public float GroundHitOriginOffset = 2.0f;

        public bool UseStreamHandleAnimationJob = true;
    }

    public struct numeric3
    {
        public float x, y, z;
        
        public numeric3(double[] src)
        {
            this.x = (float)src[0];
            this.y = (float)src[1];
            this.z = (float)src[2];
        }
        public numeric3(Vector3 src)
        {
            this.x = src.x;
            this.y = src.y;
            this.z = src.z;
        }
        public numeric3(double src)
        {
            this.x = (float)src;
            this.y = (float)src;
            this.z = (float)src;
        }

        public Unity.Mathematics.float3 to_float3() => new Unity.Mathematics.float3(this.x, this.y, this.z);
        public Vector3 ToVector3() => new Vector3(this.x, this.y, this.z);

        public static implicit operator Unity.Mathematics.float3(numeric3 src) => src.to_float3();
        public static implicit operator Vector3(numeric3 src) => src.ToVector3();

    }

}
