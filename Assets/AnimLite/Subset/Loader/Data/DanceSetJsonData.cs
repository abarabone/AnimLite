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

namespace AnimLite.Utility
{
    using AnimLite.DancePlayable;
    using AnimLite.Vmd;
    using AnimLite.Vrm;
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;

#nullable enable


    [System.Serializable]
    public class DanceSetJson : OpttionBase
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
    public class DanceMotionDefineJson : OpttionBase
    {
        public ModelDefineJson Model = new();
        public AnimationDefineJson Animation = new();
        //public MotionOptionsJson Options = new();

        public InformationDefine ModelInformation = new();
        public InformationDefine AnimationInformation = new();
    }

    [System.Serializable]
    public class AnimationDefineJson : OpttionBase
    {
        //public PathUnit AnimationFilePath = "";
        public PathList AnimationFilePath = new () { Paths = new PathUnit [] { } };
        public PathUnit FaceMappingFilePath = "";

        public float DelayTime = 0.0f;
    }
    [System.Serializable]
    public class AudioDefineJson : OpttionBase
    {
        public PathUnit AudioFilePath = "";

        public float DelayTime = 0.0f;
        public float Volume = 1.0f;
    }
    [System.Serializable]
    public class ModelDefineJson : OpttionBase
    {
        public PathUnit ModelFilePath = "";

        //public bool UsePositionAndDirection = true;
        public Vector3 Position = Vector3.zero;
        public Vector3 EulerAngles = Vector3.zero;
        public float Scale = 1.0f;
    }
    [System.Serializable]
    public class CameraDefineJson : OpttionBase
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


    public class OpttionBase
    {
        ExpandoObject? _options;

        public dynamic Options
        {
            get => this._options ?? new ExpandoObject { };
            set => this._options = value;
        }

        public T OptionsAs<T>() where T : new() =>
            JsonSupplemetUtility.PopulateDefaultViaJson<T>(this._options);
    }

    [System.Serializable]
    public class MotionOptionsJson
    {
        public float BodyScaleFromHuman = 0.0f;
        public VmdFootIkMode FootIkMode = VmdFootIkMode.auto;
    }

}
