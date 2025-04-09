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
        public Vector3 Position = Vector3.zero;
        public Vector3 EulerAngles = Vector3.zero;
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
        JObject? _options;

    #if UNITY_IL2CPP || UNITY_WEBGL || UNITY_IOS || !USE_DYNAMIC
        public object Options
    #else
        public dynamic Options
    #endif
        {
            //get => this._options ?? = new ExpandoObject { };
            get => this._options ??= new JObject { };
            set => this._options = value as JObject;
        }

        public T OptionsAs<T>() where T : new() =>
            JsonSupplemetUtility.PopulateDefaultViaJson<T>(this._options);
    }

    [System.Serializable]
    public class MotionOptionsJson
    {
        public float BodyScaleFromHuman = 0.0f;
        public float FootScaleFromHuman = 0.0f;
        public float MoveScaleFromHuman = 0.0f;

        public VmdFootIkMode FootIkMode = VmdFootIkMode.auto;

        public float GroundHitDistance = 2.0f;
        public float GroundHitOriginOffset = 2.0f;

        public bool UseStreamHandleAnimationJob = true;
    }

}
