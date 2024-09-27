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
    using AnimLite.Utility.Linq;

    using AnimLite.Vmd;
    using AnimLite.Vrm;
    using static AnimLite.DancePlayable.DanceGraphy2;


#nullable enable

    public class DanceSetDefineData
    {
        public AudioDefineData Audio;
        public AnimationDefineData DefaultAnimation;

        public ModelDefineData[] BackGrounds;
        public DanceMotionDefineData[] Motions;

        public string CaptionMode;
        public InformationDefine AudioInformation;
        public InformationDefine AnimationInformation;
    }
    public class DanceMotionDefineData
    {
        public ModelDefineData Model;
        public AnimationDefineData Animation;
        public MotionOptionsData Options;

        public InformationDefine ModelInformation;
        public InformationDefine AnimationInformation;
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

        //public bool UsePositionAndDirection;
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

        // json �� data ������B�璷�Ȃ̂łȂ�Ƃ��ł��Ȃ����Ȃ��c ���@�p�~����
        public static DanceSetDefineData ToData(this DanceSetJson json) =>
            new()
            {
                Audio = new()
                {
                    AudioFilePath = json.Audio?.AudioFilePath ?? "",
                    Volume = json.Audio?.Volume ?? default,
                    DelayTime = json.Audio?.DelayTime ?? default,
                },

                DefaultAnimation = new()
                {
                    AnimationFilePath = json.DefaultAnimation.AnimationFilePath.Paths.First(),// ?? "",
                    FaceMappingFilePath = json.DefaultAnimation?.FaceMappingFilePath ?? "",
                    DelayTime = json.DefaultAnimation?.DelayTime ?? default,
                },

                BackGrounds = Enumerable.ToArray(
                    from model in json.BackGrounds.Values ?? json.BackGrounds.Values.EmptyEnumerable().Box()
                    select new ModelDefineData
                    {
                        ModelFilePath = model.ModelFilePath.Value ?? "",
                        Position = model.Position,
                        Rotation = Quaternion.Euler(model.EulerAngles),
                        Scale = model.Scale,
                    }
                ),

                Motions = Enumerable.ToArray(
                    from motion in json.Motions.Values ?? json.Motions.Values.EmptyEnumerable().Box()
                    select new DanceMotionDefineData
                    {
                        Model = new()
                        {
                            ModelFilePath = motion.Model.ModelFilePath.Value ?? "",
                            Position = motion.Model.Position,
                            Rotation = Quaternion.Euler(motion.Model.EulerAngles),
                            Scale = motion.Model.Scale,
                        },

                        Animation = new()
                        {
                            AnimationFilePath = motion.Animation.AnimationFilePath.Paths.First(),
                            FaceMappingFilePath = motion.Animation?.FaceMappingFilePath ?? "",
                            DelayTime = motion.Animation?.DelayTime ?? default,
                        },

                        Options = new()
                        {
                            FootIkMode = motion.Options?.FootIkMode ?? VmdFootIkMode.auto,
                            BodyScaleFromHuman = motion.Options?.BodyScaleFromHuman ?? default,
                        },

                        ModelInformation = motion.ModelInformation,
                        AnimationInformation = motion.AnimationInformation,
                    }
                ),

                AudioInformation = json.AudioInformation,
                AnimationInformation = json.AnimationInformation,
                CaptionMode = json.CaptionMode ?? "",
            };
    }

#nullable disable

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
            this DanceSetDefineData ds, IArchive archive, VmdStreamDataCache cache, AudioSource audioSource, CancellationToken ct)
        {

            var aorder =
                await ds.Audio.buildAudioOrderAsync(archive, audioSource, ct);

            var bgorder = archive == null
                ? await ds.BackGrounds
                    .Select(model => Task.Run(() => model.buildBackGroundOrderAsync(archive, ct)))
                    .WhenAll()
                : await ds.BackGrounds.ToAsyncEnumerable()
                    .SelectAwait(async model => await model.buildBackGroundOrderAsync(archive, ct))
                    .ToArrayAsync()
                ;

            // �ǂ��� ziparchive �̓}���`�X���b�h�ɑΉ����ĂȂ����ۂ��̂ŁA�b��I�ɔ񓯊��񋓂őΉ��B�Ȃ�Ƃ��Ȃ�񂩁H
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

            var disposeAction = (aorder, bgorder, morders).buildDisposeAction();
            ct.ThrowIfCancellationRequested(disposeAction);

            return new()
            {
                Audio = aorder,
                BackGrouds = bgorder,
                Motions = morders,
                DisposeAction = disposeAction,
            };
        }


        static async ValueTask<AudioOrder> buildAudioOrderAsync(
            this AudioDefineData audio, IArchive archive, AudioSource audioSource, CancellationToken ct) =>
                new()
                {
                    AudioSource = audioSource,
                    AudioClip = await archive.LoadAudioClipExAsync(audio.AudioFilePath, ct),
                    Volume = audio.Volume,
                    DelayTime = audio.DelayTime,
                };


        static async Task<ModelOrder> buildBackGroundOrderAsync(
            this ModelDefineData model, IArchive archive, CancellationToken ct)
        =>
            new()
            {
                Model = await archive.LoadModelExAsync(model.ModelFilePath, ct),
                Position = model.Position,
                Rotation = model.Rotation,
                Scale = model.Scale,
            };


        static async Task<MotionOrder> buildMotionOrderAsync(
            this DanceMotionDefineData motion, IArchive archive, VmdStreamDataCache cache, CancellationToken ct)
        {
            var model = await archive.LoadModelExAsync(motion.Model.ModelFilePath, ct);

            var vmdpath = motion.Animation.AnimationFilePath;
            var facepath = motion.Animation.FaceMappingFilePath;
            var (vmddata, facemap) = cache == null
                ? await buildAsync_()
                : await buildWithCacheAsync_();

            await Awaitable.MainThreadAsync();
            return motion.toOrder(vmddata, facemap, model);


            async ValueTask<(VmdStreamData, VmdFaceMapping)> buildWithCacheAsync_()
            {
                var res = await cache.GetOrLoadVmdStreamDataAsync(vmdpath, facepath, archive, ct);

                return (res.vmddata, res.facemap);
            }
            async ValueTask<(VmdStreamData, VmdFaceMapping)> buildAsync_()
            {
                var facemap = await archive.LoadFaceMapExAsync(facepath, ct);
                var vmddata = await VmdData.LoadVmdStreamDataExAsync(vmdpath, facemap, archive, ct);

                return (vmddata, facemap);
            }
        }


        static MotionOrder toOrder(
            this DanceMotionDefineData m, VmdStreamData vmddata, VmdFaceMapping facemap, GameObject model)
        {
            return new MotionOrder
            {
                Model = model,
                FaceRenderer = model.FindFaceRenderer(),

                vmddata = vmddata,
                bone = model.GetComponent<Animator>().BuildVmdPlayableJobTransformMappings(),
                face = facemap.BuildStreamingFace(),

                DelayTime = m.Animation.DelayTime,
                BodyScale = m.Options.BodyScaleFromHuman,
                FootIkMode = m.Options.FootIkMode,

                //OverWritePositionAndRotation = m.Model.UsePositionAndDirection,
                Position = m.Model.Position,
                Rotation = m.Model.Rotation,
                Scale = m.Model.Scale,
            };
        }

        static Action buildDisposeAction(this (AudioOrder audio, ModelOrder[] bgs, MotionOrder[] motions) order) =>
            () =>
            {
                order.audio.AudioClip.Dispose();

                foreach (var bg in order.bgs)
                {
                    bg.Model.AsUnityNull()?.Destroy();
                }

                foreach (var m in order.motions)
                {
                    //m.face.Dispose();
                    m.bone.Dispose();
                    m.vmddata.Dispose();
                    m.Model.AsUnityNull()?.Destroy();
                }
            };
    }



    //public struct VrmModelLoader : ILoader<GameObject>
    //{
    //    public ValueTask<GameObject> LoadAsync(PathUnit path, CancellationToken ct)
    //    {
    //        return path.LoadModelExAsync(ct);
    //    }
    //}
    //public class VrmModelLoaderWithCache : ILoader<GameObject>
    //{
    //    public ModelGameObjectStocker stocker;

    //    public ValueTask<GameObject> LoadAsync(PathUnit path, CancellationToken ct)
    //    {
    //        return new ValueTask<GameObject>(this.stocker.GetOrLoadAsync(path, null, ct));
    //    }
    //}
    //public class VrmModelLoaderInArchive : ILoader<GameObject>
    //{
    //    public ZipArchive archive;

    //    public ValueTask<GameObject> LoadAsync(PathUnit path, CancellationToken ct)
    //    {
    //        return path.LoadModelExAsync(this.archive, ct);
    //    }
    //}
    //public class VrmModelLoaderInArchiveWithCache : ILoader<GameObject>
    //{
    //    public ZipArchive archive;
    //    public ModelGameObjectStocker stocker;

    //    public ValueTask<GameObject> LoadAsync(PathUnit path, CancellationToken ct)
    //    {
    //        return new ValueTask<GameObject>(this.stocker.GetOrLoadAsync(path, this.archive, ct)); 
    //    }
    //}

}
