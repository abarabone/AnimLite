using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;

namespace AnimLite.DancePlayable
{
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;

    using AnimLite.Vmd;
    using AnimLite.Vrm;


    [System.Serializable]
    public class DanceSet
    {
        public AudioDefine Audio;

        public DanceMotionDefine[] Motions;
    }

    [System.Serializable]
    public class AudioDefine
    {
        public AudioSource AudioSource;
        public AudioClip AudioClip;

        public float DelayTime;
    }

    [System.Serializable]
    public class DanceMotionDefine
    {
        [FilePath]
        public PathUnit VmdFilePath;
        [FilePath]
        public PathUnit FaceMappingFilePath;

        public Animator ModelAnimator;
        public SkinnedMeshRenderer FaceRenderer;

        public float DelayTime;

        public VmdFootIkMode FootIkMode = VmdFootIkMode.auto;
        public float BodyScale = 0;

        /*[HideInInspector]*/ public bool OverWritePositionAndRotation;
        /*[HideInInspector]*/ public Vector3 Position;
        /*[HideInInspector]*/ public Quaternion Rotation;
    }


    public static class DanceGraphyExtension
    {

        public static Task<DanceGraphy> CreateDanceGraphyAsync(
            this DanceSet dance, VmdStreamDataCache cache, CancellationToken ct)
        {
            return DanceGraphy.CreateDanceGraphyAsync(dance, cache, ct);
        }

    }


}