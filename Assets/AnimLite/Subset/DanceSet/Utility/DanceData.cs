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
    //using VRM;
    using static AnimLite.DancePlayable.DanceGraphy;

    [System.Serializable]
    public class DanceSet
    {
        public AudioDefine Audio;
        public DefaultDanceMotionDefine DefaultAnimation;

        public InformationDefain AudioInfo;
        public InformationDefain AnimationInfo;

        public DanceMotionDefine[] Motions;
    }

    [System.Serializable]
    public class AudioDefine
    {
        public AudioSource AudioSource;
        public AudioClipAsDisposable AudioClip;

        public float DelayTime;
    }

    [System.Serializable]
    public class DanceMotionDefine
    {
        [FilePath]
        public PathUnit AnimationFilePath;
        [FilePath]
        public PathUnit FaceMappingFilePath;

        public Animator ModelAnimator;
        public SkinnedMeshRenderer FaceRenderer;

        public float DelayTime;

        public VmdFootIkMode FootIkMode = VmdFootIkMode.auto;
        public float BodyScale = 0;

        public bool OverWritePositionAndRotation;
        public Vector3 Position;
        public Quaternion Rotation;
        public float Scale;

        public InformationDefain ModelInfo;
        public InformationDefain AnimationInfo;
    }
    [System.Serializable]
    public class DefaultDanceMotionDefine
    {
        [FilePath]
        public PathUnit AnimationFilePath;
        [FilePath]
        public PathUnit FaceMappingFilePath;

        public float DelayTime;

        public VmdFootIkMode FootIkMode = VmdFootIkMode.auto;
        public float BodyScale = 0;
    }


    //public interface IAudioMedia
    //{
    //    PathUnit path { get; }
    //    AudioClip clip { get; }
    //}
    //public interface IModel
    //{
    //    PathUnit path { get; }
    //    Animator model { get; }
    //}
    //public interface IMotion
    //{
    //    PathUnit path { get; }

    //}



    public static class DanceGraphyExtension
    {

        public static async Task<MotionResource[]> BuildMotionResourcesAsync(
            this DanceSet dance, VmdStreamDataCache cache, CancellationToken ct)
        {

            var motions = dance.Motions;

            return await BuildMotionResourcesAsync(motions, cache, ct);
        }


        public static async Task<MotionResource[]> BuildMotionResourcesAsync(
            this DanceMotionDefine[] motions, VmdStreamDataCache cache, CancellationToken ct)
        {

            var defaultFaceMap =
                cache.IsUnityNull()
                &&
                motions.Any(x => x.FaceMappingFilePath.IsBlank())
                    ? await "".ToPath().LoadFaceMapExAsync(ct)
                    : default;

            var resources = await motions
                .Select(motion => Task.Run(() => buildAsync_(motion)))
                .WhenAll();

            if (ct.IsCancellationRequested) resources.DisposeAll();
            ct.ThrowIfCancellationRequested();

            return resources;


            async Task<MotionResource> buildAsync_(DanceMotionDefine motion)
            {
                var vmdfullpath = motion.AnimationFilePath;
                var facefullpath = motion.FaceMappingFilePath;

                var useDefaultFacemap = defaultFaceMap.VmdToVrmMaps != default;
                var useCache = !cache.IsUnityNull();

                var (vmddata, facemap) = useCache
                    ? await loadWithCacheAsync_()
                    : await loadAsync_();

                await Awaitable.MainThreadAsync();
                return vmddata.ToMotionResource(facemap, motion.ModelAnimator);//, motion.FaceRenderer);


                async Task<(VmdStreamData, VmdFaceMapping)> loadAsync_()
                {
                    var facemap = useDefaultFacemap
                        ? defaultFaceMap
                        : await facefullpath.ParseFaceMapAsync(ct);

                    var vmddata = await vmdfullpath.LoadVmdStreamDataExAsync(facemap, ct);

                    return (vmddata, facemap);
                }
                async Task<(VmdStreamData, VmdFaceMapping)> loadWithCacheAsync_()
                {
                    var (vmddata, facemap) =
                        await cache.GetOrLoadVmdStreamDataAsync(vmdfullpath, facefullpath, ct);

                    return (vmddata, facemap);
                }
            }
        }


        public static MotionResource ToMotionResource(
            this VmdStreamData vmddata, VmdFaceMapping facemap, Animator anim) =>//, SkinnedMeshRenderer faceRenderer) =>
                new MotionResource
                {
                    vmddata = vmddata,

                    bone = anim.BuildVmdPlayableJobTransformMappings(),
                    face = facemap.BuildStreamingFace(),
                    //face = faceRenderer?.sharedMesh?.BuildStreamingFace(facemap) ?? default,
                };

    }

}