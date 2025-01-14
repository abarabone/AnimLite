using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Compression;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
//using UniVRM10;
using Unity.VisualScripting;
using Unity.Mathematics;


#nullable enable

namespace AnimLite.Loader
{
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;
    using AnimLite.DancePlayable;

    using AnimLite.Vrm;
    using AnimLite.Vmd;
    using static AnimLite.DancePlayable.DanceGraphy;

    public static class SceneLoadUtilitiy
    {


        /// <summary>
        /// 
        /// </summary>
        public static ValueTask<Order> BuildDanceGraphyOrderAsync(
            this DanceSetJson ds, PrototypeCacheHolder cache, IArchive? archive, AudioSource audioSource, CancellationToken ct)
        =>
            DanceSceneLoader.UseSeaquentialLoading
                ? ds.BuildDanceGraphyOrderSequentialAsync(cache, archive, audioSource, ct)
                : ds.BuildDanceGraphyOrderParallelAsync(cache, archive, audioSource, ct);



        /// <summary>
        /// 
        /// </summary>
        public static async ValueTask<Order> BuildDanceGraphyOrderParallelAsync(
            this DanceSetJson ds, PrototypeCacheHolder cache, IArchive? archive, AudioSource audioSource, CancellationToken ct)
        {
            var audioTask =
                Task.Run(async () => await ds.Audio.buildAudioOrderAsync(archive, audioSource, ct) as object);

            var bgTasks = ds.BackGrounds
                .Where(x => x.Key[0] != '_')
                .Select(define =>
                Task.Run(async () => await define.Value.buildBackGroundModelOrderAsync(cache, archive, ct) as object));

            var motionTasks = ds.Motions
                .Where(x => x.Key[0] != '_')
                .Select(define =>
                Task.Run(async () => await define.Value.buildMotionOrderParallelAsync(cache, archive, ct) as object));


            var orders = await audioTask.WrapEnumerable().Concat(bgTasks).Concat(motionTasks)
                .WhenAll();


            var audioOrder = orders.First() as AudioOrder;
            var bgOrders = orders.Skip(1).Take(ds.BackGrounds.Count).Cast<ModelOrder>().ToArray();
            var motionOrders = orders.Skip(1).Skip(ds.BackGrounds.Count).Cast<MotionOrderBase>().ToArray();

            var order = await toOrderAsync(audioOrder!, bgOrders, motionOrders, ct);
            await ds.OrverrideInformationIfBlankAsync(order);
            return order;
        }

        /// <summary>
        /// 
        /// </summary>
        public static async ValueTask<Order> BuildDanceGraphyOrderSequentialAsync(
            this DanceSetJson ds, PrototypeCacheHolder cache, IArchive? archive, AudioSource audioSource, CancellationToken ct)
        {

            var audioOrder = await ds.Audio
                .buildAudioOrderAsync(archive, audioSource, ct);

            var bgOrders = await ds.BackGrounds
                .Where(x => x.Key[0] != '_')
                .ToAsyncEnumerable()
                .SelectAwait(define => define.Value.buildBackGroundModelOrderAsync(cache, archive, ct))
                .ToArrayAsync();

            var motionOrders = await ds.Motions
                .Where(x => x.Key[0] != '_')
                .ToAsyncEnumerable()
                .SelectAwait(define => define.Value.buildMotionOrderSequentialAsync(cache, archive, ct))
                .ToArrayAsync();


            var order = await toOrderAsync(audioOrder, bgOrders, motionOrders, ct);
            await ds.OrverrideInformationIfBlankAsync(order);
            return order;
        }
        ///// <summary>
        ///// 
        ///// </summary>
        //public static async ValueTask<Order> BuildDanceGraphyOrderSequentialAsync_(
        //    this DanceSetDefineData ds, VmdStreamDataCache cache, IArchive archive, AudioSource audioSource, CancellationToken ct)
        //{

        //    var audioclip = await ds.Audio
        //        .AudioFilePath.LoadAudioClipExAsync(archive, ct);
        //    var audioOrder = ds.Audio.toAudioOrder(audioclip, audioSource);

        //    var bgs = ds.BackGrounds//.ToAsyncEnumerable()
        //        .Select(x => x.ModelFilePath)
        //        .Select(path => loadModelAsync(cache, path, archive, ct));
        //    var bgOrders = await (ds.BackGrounds, bgs).Zip().ToAsyncEnumerable()
        //        .SelectAwait(async x => x.Item1.toBackGroundOrder(await x.Item2))
        //        .ToArrayAsync();

        //    var models = ds.Motions.ToAsyncEnumerable()
        //        .Select(x => x.Model.ModelFilePath)
        //        .SelectAwait(path => loadModelAsync(cache, path, archive, ct));
        //    var anims = ds.Motions.ToAsyncEnumerable()
        //        .Select(x => (anim: x.Animation.AnimationFilePath, face: x.Animation.FaceMappingFilePath))
        //        .SelectAwait(path => loadVmdAsync(cache, path.anim, path.face, archive, ct));
        //    var motionOrders = await (ds.Motions, await (models, anims).Zip()).Zip()
        //        .SelectAwait(async x => (d: x.Item1, m: await x.Item2.Item1, a: await x.Item2.Item2))
        //        .SelectAwait(async x =>
        //        {
        //            await Awaitable.MainThreadAsync();
        //            return x.d.toMotionOrder(x.a.Item1, x.a.Item2, x.m);
        //        })
        //        .ToArrayAsync();

        //    await Awaitable.MainThreadAsync();
        //    var order = toOrder(audioOrder, bgOrders, motionOrders, ct);
        //    await ds.OrverrideInformationIfBlankAsync(order);
        //    return order;
        //}




        /// <summary>
        /// 
        /// </summary>
        static async ValueTask<AudioOrder> buildAudioOrderAsync(
            this AudioDefineJson define, IArchive? archive, AudioSource audioSource, CancellationToken ct)
        {
            var audiopath = define.AudioFilePath;

            var prototype = await archive.LoadAudioClipPrototypeAsync(audiopath, ct);
            var clip = await prototype.NullableAsync(x => x.InstantiateAsync());

            await prototype.DisposeNullableAsync();

            return define.toAudioOrder(clip, audioSource);
        }




        /// <summary>
        /// 
        /// </summary>
        static async ValueTask<ModelOrder> buildBackGroundModelOrderAsync(
            this ModelDefineJson define, PrototypeCacheHolder cache, IArchive? archive, CancellationToken ct)
        {
            var bgpath = define.ModelFilePath;

            var model = await cache.loadModelAsync(bgpath, archive, ct);
            //var model = await bgpath.LoadModelExAsync(archive, ct);

            return define.toBackGroundOrder(model);
        }




        /// <summary>
        /// 
        /// </summary>
        static async ValueTask<MotionOrderBase> buildMotionOrderParallelAsync(
            this DanceMotionDefineJson define, PrototypeCacheHolder cache, IArchive? archive, CancellationToken ct)
        {

            var modelpath = define.Model.ModelFilePath;
            var facepath = define.Animation.FaceMappingFilePath;
            var adjustpath = define.Animation.BodyAdjustFilePath;

            var vmdpath = define.Animation.AnimationFilePath;

            var modelTask =
                Task.Run(async () => await cache.loadModelAsync(modelpath, archive, ct) as object);
            var streamdataTask =
                Task.Run(async () => await cache.loadVmdAsync(vmdpath, facepath, archive, ct) as object);
            var adjustTask =
                Task.Run(async () => await archive.LoadBodyAdjustAsync(adjustpath, ct) as object);

            var data = await Task.WhenAll(modelTask!, streamdataTask, adjustTask);


            var model = data[0] as Instance<GameObject>;
            var (vmddata, facemap) = ((Instance<VmdStreamData>, Instance<VmdFaceMapping>))data[1];
            var adjust = (BodyAdjustData)data[2];

            await Awaitable.MainThreadAsync();
            return
                define.toMotionOrder(vmddata, facemap, model, adjust) as MotionOrderBase
                ??
                // animation clip が face やブレンドを整備するまでの暫定
                await define.toMotionOrderAwait(model, ct);
        }
        //static async ValueTask<MotionOrder> buildMotionOrderParallelAsync(
        //    this DanceMotionDefineJson define, VmdStreamDataCache cache, IArchive archive, CancellationToken ct)
        //{
        //    var modelpath = define.Model.ModelFilePath;
        //    var facepath = define.Animation.FaceMappingFilePath;
        //    var vmdpaths = define.Animation.AnimationFilePath;

        //    var modelTask =
        //        Task.Run(async () => await cache.loadModelAsync(modelpath, archive, ct) as object);
        //    //var streamdataTask =
        //    //    Task.Run(async () => await cache.loadVmdAsync(vmdpath0, facepath, archive, ct) as object);
        //    //var streamdataTaskList = vmdpaths.Paths.Select(vmdpath =>
        //    //    Task.Run(async () => (await cache.loadVmdAsync(vmdpath, facepath, archive, ct)).Item1 as object));


        //    var data = await modelTask.WrapEnumerable().Concat(streamdataTaskList).WhenAll();
        //    //var data = await Task.WhenAll(modelTask, streamdataTask);


        //    var model = data[0] as GameObject;
        //    var (vmddata, facemap) = ((VmdStreamData, VmdFaceMapping))data[1];

        //    await Awaitable.MainThreadAsync();
        //    return define.toMotionOrder(vmddata, facemap, model);
        //}
        /// <summary>
        /// 
        /// </summary>
        static async ValueTask<MotionOrderBase> buildMotionOrderSequentialAsync(
            this DanceMotionDefineJson define, PrototypeCacheHolder cache, IArchive? archive, CancellationToken ct)
        {
            var modelpath = define.Model.ModelFilePath;
            var vmdpath = define.Animation.AnimationFilePath;
            var facepath = define.Animation.FaceMappingFilePath;
            var adjustpath = define.Animation.BodyAdjustFilePath;

            var model = await cache.loadModelAsync(modelpath, archive, ct);
            var (vmddata, facemap) = await cache.loadVmdAsync(vmdpath, facepath, archive, ct);
            var adjust = await archive.LoadBodyAdjustAsync(adjustpath, ct);

            await Awaitable.MainThreadAsync();
            return
                define.toMotionOrder(vmddata, facemap, model, adjust) as MotionOrderBase
                ??
                // animation clip が face やブレンドを整備するまでの暫定
                await define.toMotionOrderAwait(model, ct);
        }





        /// <summary>
        /// とりあえずざんていで stocker あるなしでキャッシュ使うか決める
        /// </summary>
        static async ValueTask<Instance<GameObject>?> loadModelAsync(
            this PrototypeCacheHolder cache, PathUnit modelpath, IArchive? archive, CancellationToken ct)
        {
            return cache is not null
                ? await cache.ModelCache.GetOrLoadModelAsync(modelpath, archive, ct)
                : await archive.LoadModelInstanceAsync(modelpath, ct);
        }
        //static ValueTask<GameObject> loadModelAsync(
        //    this VmdStreamDataCache stocker, PathUnit modelpath, IArchive? archive, CancellationToken ct)
        //=>
        //    (modelpath.IsResource() ? null : stocker)?.GetOrLoadModelAsync(modelpath, archive, ct).AsValueTask()
        //    ??
        //    archive.LoadModelExAsync(modelpath, ct);




        //static async ValueTask<Instance<VmdFaceMapping>> loadFacemapAsync(
        //    this PrototypeCacheHolder cache, PathUnit facepath, IArchive? archive, CancellationToken ct)
        //=>
        //    cache is not null
        //        ? await cache.FaceMapCache.GetOrLoadVmdFaceMappingAsync(facepath, archive, ct)
        //        : await (await archive.LoadFaceMapExAsync(facepath, ct)).ToPrototype().InstantiateAsync();


        static ValueTask<(Instance<VmdStreamData>, Instance<VmdFaceMapping>)> loadVmdAsync(
            this PrototypeCacheHolder cache, PathList vmdpaths, PathUnit facepath, IArchive? archive, CancellationToken ct)
        {
            return cache is not null
                ? loadFromCache_()
                : loadFromFile_();

            async ValueTask<(Instance<VmdStreamData>, Instance<VmdFaceMapping>)> loadFromCache_() =>
            (
                await cache.VmdCache.GetOrLoadVmdAsync(vmdpaths, facepath, archive, ct),
                await cache.VmdCache.facemap.GetOrLoadVmdFaceMappingAsync(facepath, archive, ct)
            );
            async ValueTask<(Instance<VmdStreamData>, Instance<VmdFaceMapping>)> loadFromFile_()
            {
                var (vmd, map) = await archive.LoadVmdStreamDataPrototypeAsync(vmdpaths, facepath, ct);
                return (await vmd.InstantiateAsync(), await map.InstantiateAsync());
            }
        }





        static AudioOrder toAudioOrder(
            this AudioDefineJson define, Instance<AudioClip>? clip, AudioSource audioSource)
        =>
            new()
            {
                AudioSource = audioSource,
                AudioClip = clip,
                Volume = define.Volume,
                DelayTime = define.DelayTime,
            };


        static ModelOrder toBackGroundOrder(
            this ModelDefineJson define, Instance<GameObject>? model)
        =>
            new()
            {
                Model = model,
                Position = define.Position,
                Rotation = Quaternion.Euler(define.EulerAngles),
                Scale = define.Scale,
            };

        static MotionOrder? toMotionOrder(
            this DanceMotionDefineJson define, Instance<VmdStreamData> vmddata, Instance<VmdFaceMapping> facemap, Instance<GameObject>? model, BodyAdjustData adjust)
        {
            var options = define.Animation.OptionsAs<MotionOptionsJson>();

            // animation clip が face やブレンドを整備するまでの暫定
            return vmddata is not null
            ? new()
            //new()
            {
                Model = model,
                FaceRenderer = model.AsUnityNull()?.FindFaceRenderer(),

                //vmddata = vmddata,
                vmd = vmddata,
                bone = model.AsUnityNull()?.GetComponent<Animator>()
                    .BuildVmdPlayableJobTransformMappings(adjust)
                    ??
                    default,
                face = facemap.Value.BuildStreamingFace(),

                DelayTime = define.Animation.DelayTime,
                BodyScale = options.BodyScaleFromHuman,
                FootIkMode = options.FootIkMode,

                Position = define.Model.Position,
                Rotation = Quaternion.Euler(define.Model.EulerAngles),
                Scale = define.Model.Scale,
            }
            //;
            : null;
        }
            
        // animation clip がキャッシュ、face やブレンドを整備するまでの暫定
        static async ValueTask<MotionOrderWithAnimationClip> toMotionOrderAwait(
            this DanceMotionDefineJson define, Instance<GameObject>? model, CancellationToken ct)
        =>
            new()
            {
                Model = model,
                FaceRenderer = model.AsUnityNull()?.FindFaceRenderer(),

                // vmd ロード失敗してたら、animation clip をリソースロードする
                AnimationClip = await(await define.Animation.AnimationFilePath.Paths
                        .DefaultIfEmpty("".ToPath())
                        .First()
                        .ToResourceName()
                        .LoadAnimationClipPrototypeAsync(ct))
                    .NullableAsync(async x =>
                    {
                        var i = await x.InstantiateAsync();
                        await x.DisposeAsync();
                        return i;
                    }),

                DelayTime = define.Animation.DelayTime,

                Position = define.Model.Position,
                Rotation = Quaternion.Euler(define.Model.EulerAngles),
                Scale = define.Model.Scale,
            };


        /// <summary>
        /// オーディオクリップ、モデルゲームオブジェクト、アニメーションストリームデータ、ボーンデータのうち、破棄が必要なリソースだけ破棄する。
        /// モデルに関しては、現在アクティブなゲームオブジェクトだけ Destroy() する。
        /// モデルストックによって非アクティブになっている場合は、Destroy() されない。
        /// ただし、Destroy() が反映されるタイミングには注意すること。（おそらく Destory() の次のフレームから）
        /// </summary>
        static Func<ValueTask> buildDisposeAction(this (AudioOrder audio, ModelOrder[] bgs, MotionOrderBase[] motions) order) =>
            async () =>
            {
                await order.audio.AudioClip.DisposeNullableAsync();

                foreach (var bg in order.bgs)
                {
                    //bg.Model.AsUnityNull(o => o?.activeSelf)?.Destroy();
                    await bg.Model.DisposeNullableAsync();
                }

                foreach (var m in order.motions)
                {
                    await m.DisposeNullableAsync();
                }
            };


        static async ValueTask<Order> toOrderAsync(AudioOrder audioOrder, ModelOrder[] bgOrders, MotionOrderBase[] motionOrders, CancellationToken ct)
        {
            var disposeAction = (audioOrder, bgOrders, motionOrders).buildDisposeAction();
            await ct.ThrowIfCancellationRequested(disposeAction);

            return new()
            {
                Audio = audioOrder,
                BackGrouds = bgOrders,
                Motions = motionOrders,
                DisposeAction = disposeAction,
            };
        }

    }
}
