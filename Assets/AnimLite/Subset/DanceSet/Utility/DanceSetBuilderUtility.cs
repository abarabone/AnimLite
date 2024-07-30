using AnimLite.DancePlayable;
using AnimLite.Vmd;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Networking;
using UniVRM10;
using Unity.VisualScripting;

namespace AnimLite.Utility
{
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;

    using AnimLite.Vmd;
    using AnimLite.Vrm;
    using static AnimLite.DancePlayable.DanceGraphy2;




    public class DanceSetDefineData
    {
        public AudioDefineData Audio;
        public AnimationDefineData DefaultAnimation;

        public string CaptionMode;
        public InformationDefain AudioInformation;
        public InformationDefain AnimationInformation;

        public DanceMotionDefineData[] Motions;
    }
    public class DanceMotionDefineData
    {
        public ModelDefineData Model;
        public AnimationDefineData Animation;
        public MotionOptionsData Options;

        public InformationDefain ModelInformation;
        public InformationDefain AnimationInformation;
    }

    public class AnimationDefineData
    {
        public PathUnit AnimationFilePath;
        public PathUnit FaceMappingFilePath;

        public float DelayTime;
    }
    public class AudioDefineData
    {
        public PathUnit AudioFilePath;

        public float Volume;
        public float DelayTime;
    }
    public class ModelDefineData
    {
        public PathUnit ModelFilePath;

        public bool UsePositionAndDirection;
        public Vector3 Position;
        public Quaternion Rotation;
        public float Scale;
    }
    public class MotionOptionsData
    {
        public float BodyScaleFromHuman;
        public VmdFootIkMode FootIkMode;
    }

    //public class InformationDefainData
    //{
    //    public string Caption;
    //    public string Author;
    //    public string Url;
    //    public string Description;
    //}



    public static class JsonConverter
    {

        // json を data 化する。冗長なのでなんとかできないかなぁ…
        public static DanceSetDefineData ToData(this DanceSetJson json) =>
            new()
            {
                Audio = new()
                {
                    AudioFilePath = json.Audio.AudioFilePath,
                    Volume = json.Audio.Volume,
                    DelayTime = json.Audio.DelayTime,
                },

                DefaultAnimation = new()
                {
                    AnimationFilePath = json.DefaultAnimation.AnimationFilePath,
                    FaceMappingFilePath = json.DefaultAnimation.FaceMappingFilePath,
                    DelayTime = json.DefaultAnimation.DelayTime,
                },

                Motions = Enumerable.ToArray(
                    from motion in json.Motions
                    select new DanceMotionDefineData
                    {
                        Model = new()
                        {
                            ModelFilePath = motion.Model.ModelFilePath,
                            //UsePositionAndDirection = motion.Model.UsePositionAndDirection,
                            UsePositionAndDirection = !(motion.Model.Position.IsZero() && motion.Model.EulerAngles.IsZero()),
                            Position = motion.Model.Position,
                            Rotation = Quaternion.Euler(motion.Model.EulerAngles),
                            Scale = motion.Model.Scale,
                        },

                        Animation = new()
                        {
                            AnimationFilePath = motion.Animation.AnimationFilePath,
                            FaceMappingFilePath = motion.Animation.FaceMappingFilePath,
                            DelayTime = motion.Animation.DelayTime,
                        },

                        Options = new()
                        {
                            FootIkMode = motion.Options.FootIkMode switch
                            {
                                "on" => VmdFootIkMode.@on,
                                "off" => VmdFootIkMode.off,
                                _ => VmdFootIkMode.auto,
                            },
                            BodyScaleFromHuman = motion.Options.BodyScaleFromHuman,
                        },

                        ModelInformation = motion.ModelInformation,
                        AnimationInformation = motion.AnimationInformation,
                    }
                ),

                AudioInformation = json.AudioInformation,
                AnimationInformation = json.AnimationInformation,
                CaptionMode = json.CaptionMode,
            };
    }



    public static class DanceSetBuildUtility
    {

        //public static async ValueTask<Order> a(
        //    this PathUnit jsonpath, VmdStreamDataCache cache, CancellationToken ct)
        //{
        //    using var archive = await jsonpath.OpenZipAsync(ct);

        //    var json = archive == null
        //        ? await jsonpath.ReadJsonExAsync<DanceSetJson>(ct)
        //        : await jsonpath.ToZipEntryPath().ReadJsonExAsync<DanceSetJson>(archive, ct);

        //    var ds = json.ToData();

        //    return ds;
        //}


        public static ValueTask<Order> BuildDanceOrderAsync(
            this DanceSetDefineData ds, VmdStreamDataCache cache, AudioSource audioSource, CancellationToken ct)
        =>
            ds.BuildDanceOrderAsync(null, cache, audioSource, ct);



        public static async ValueTask<Order> BuildDanceOrderAsync(
            this DanceSetDefineData ds, ZipArchive archive, VmdStreamDataCache cache, AudioSource audioSource, CancellationToken ct)
        {

            var aorder =
                await ds.Audio.buildAudioOrderAsync(archive, audioSource, ct);

            // どうも ziparchive はマルチスレッドに対応してないっぽいので、暫定的に非同期列挙で対応。なんとかならんか？
            var morders = archive == null
                ? await ds.Motions
                    .Select(motion => Task.Run(() => motion.buildMotionOrderAsync(archive, cache, ct)))
                    .WhenAll()
                : await ds.Motions.ToAsyncEnumerable()
                    .SelectAwait(async motion => await motion.buildMotionOrderAsync(archive, cache, ct))
                    .ToArrayAsync()
                ;
            //var morders = await ds.Motions
            //        .Select(motion => motion.buildMotionOrderAsync(archive, cache, ct))
            //        .WhenAll();

            var disposeAction = (aorder, morders).buildDisposeAction();
            ct.ThrowIfCancellationRequested(disposeAction);

            return new()
            {
                Audio = aorder,
                Motions = morders,
                DisposeAction = disposeAction,
            };
        }

        static async ValueTask<AudioOrder> buildAudioOrderAsync(
            this AudioDefineData audio, ZipArchive archive, AudioSource audioSource, CancellationToken ct) =>
                new()
                {
                    AudioSource = audioSource,
                    AudioClip = await audio.AudioFilePath.LoadAudioClipExAsync(archive, ct),
                    Volume = audio.Volume,
                    DelayTime = audio.DelayTime,
                };


        static async Task<MotionOrder> buildMotionOrderAsync(
            this DanceMotionDefineData motion, ZipArchive archive, VmdStreamDataCache cache, CancellationToken ct)
        {
            var anim = await motion.Model.ModelFilePath.LoadModelExAsync(archive, ct);

            var vmdfullpath = motion.Animation.AnimationFilePath;
            var facefullpath = motion.Animation.FaceMappingFilePath;
            var (vmddata, facemap) = cache == null
                ? await buildAsync_()
                : await buildWithCacheAsync_();

            await Awaitable.MainThreadAsync();
            return motion.toOrder(vmddata, facemap, anim);


            async ValueTask<(VmdStreamData, VmdFaceMapping)> buildWithCacheAsync_()
            {
                var res = await cache.GetOrLoadVmdStreamDataAsync(vmdfullpath, facefullpath, archive, ct);

                return (res.vmddata, res.facemap);
            }
            async ValueTask<(VmdStreamData, VmdFaceMapping)> buildAsync_()
            {
                var facemap = await facefullpath.LoadFaceMapExAsync(ct);
                var vmddata = await vmdfullpath.LoadVmdStreamDataExAsync(facemap, archive, ct);

                return (vmddata, facemap);
            }
        }


        static MotionOrder toOrder(
            this DanceMotionDefineData m, VmdStreamData vmddata, VmdFaceMapping facemap, Animator animator)
        {
            return new MotionOrder
            {
                ModelAnimator = animator,
                FaceRenderer = animator.FindFaceRenderer(),

                vmddata = vmddata,
                bone = animator.BuildVmdPlayableJobTransformMappings(),
                face = facemap.BuildStreamingFace(),

                DelayTime = m.Animation.DelayTime,
                BodyScale = m.Options.BodyScaleFromHuman,
                FootIkMode = m.Options.FootIkMode,

                OverWritePositionAndRotation = m.Model.UsePositionAndDirection,
                Position = m.Model.Position,
                Rotation = m.Model.Rotation,
                Scale = m.Model.Scale,
            };
        }

        static Action buildDisposeAction(this (AudioOrder audio, MotionOrder[] motions) order) =>
            () =>
            {
                order.audio.AudioClip.Dispose();

                foreach (var m in order.motions)
                {
                    //m.face.Dispose();
                    m.bone.Dispose();
                    m.vmddata.Dispose();
                    m.ModelAnimator.AsUnityNull()?.gameObject?.Destroy();
                }
            };
    }
}
