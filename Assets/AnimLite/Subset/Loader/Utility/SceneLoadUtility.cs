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

    using AnimLite.Vrm;
    using AnimLite.Vmd;
    using static AnimLite.DancePlayable.DanceGraphy2;




    public static class SceneLoadUtilitiy
    {

        /// <summary>
        /// true であれば、同じ zip 内のデータロードには１つの ZipArchive しかオープンしない。
        /// ただしその場合、並列的なロードは行われない。
        /// false であれば常に並列的なロードを行うが、それぞれ別個に ZipArchive をオープンする。
        /// </summary>
        public static bool IsSeaquentialLoadingInZip = false;




        /// <summary>
        /// パスが zip であれば、ZipArchive を返す。それ以外は null を返す。
        /// IsSeaquentialLoadingInZip が false であれば、常に null を返す。
        /// </summary>
        public static async ValueTask<ZipArchive> OpenZipAsync(this PathUnit path, CancellationToken ct)
        {
            if (!SceneLoadUtilitiy.IsSeaquentialLoadingInZip) return null;

            ValueTask<Stream> openAsync_(PathUnit path) => path.OpenStreamFileOrWebAsync(ct);

            var fullpath = path.ToFullPath();
            fullpath.ThrowIfAccessedOutsideOfParentFolder();

            return fullpath.DividZipAndEntry() switch
            {
                var (zippath, _) when zippath != "" =>
                    await openAsync_(zippath).OpenZipAsync(),
                _ =>
                    null,
            };
        }





        /// <summary>
        /// 
        /// </summary>
        public static ValueTask<Order> BuildDanceGraphyOrderAsync(
            this DanceSetDefineData ds, VmdStreamDataCache cache, ZipArchive archive, AudioSource audioSource, CancellationToken ct)
        =>
            archive != null
                ? ds.BuildDanceGraphyOrderSequentialAsync(cache, archive, audioSource, ct)
                : ds.BuildDanceGraphyOrderParallelAsync(cache, audioSource, ct);



        /// <summary>
        /// 
        /// </summary>
        public static async ValueTask<Order> BuildDanceGraphyOrderParallelAsync(
            this DanceSetDefineData ds, VmdStreamDataCache cache, AudioSource audioSource, CancellationToken ct)
        {
            var audioTask =
                Task.Run(async () => await ds.Audio.buildAudioOrderAsync(null, audioSource, ct) as dynamic);

            var bgTasks = ds.BackGrounds.Select(define =>
                Task.Run(async () => await define.buildBackGroundModelOrderAsync(cache, null, ct) as dynamic));

            var motionTasks = ds.Motions.Select(define =>
                Task.Run(async () => await define.buildMotionOrderAsync(cache, null, ct) as dynamic));


            var orders = await audioTask.WrapEnumerable().Concat(bgTasks).Concat(motionTasks)
                .WhenAll();


            var audioOrder = orders.First() as AudioOrder;
            var bgOrders = orders.Skip(1).Take(ds.BackGrounds.Length).Cast<ModelOrder>().ToArray();
            var motionOrders = orders.Skip(1).Skip(ds.BackGrounds.Length).Cast<MotionOrder>().ToArray();

            var order = toOrder(audioOrder, bgOrders, motionOrders, ct);
            await ds.OrverrideInformationIfBlankAsync(order);
            return order;
        }

        /// <summary>
        /// 
        /// </summary>
        public static async ValueTask<Order> BuildDanceGraphyOrderSequentialAsync(
            this DanceSetDefineData ds, VmdStreamDataCache cache, ZipArchive archive, AudioSource audioSource, CancellationToken ct)
        {

            var audioOrder = await ds.Audio
                .buildAudioOrderAsync(archive, audioSource, ct);

            var bgOrders = await ds.BackGrounds.ToAsyncEnumerable()
                .SelectAwait(define => define.buildBackGroundModelOrderAsync(cache, archive, ct))
                .ToArrayAsync();

            var motionOrders = await ds.Motions.ToAsyncEnumerable()
                .SelectAwait(define => define.buildMotionOrderAsync(cache, archive, ct))
                .ToArrayAsync();


            var order = toOrder(audioOrder, bgOrders, motionOrders, ct);
            await ds.OrverrideInformationIfBlankAsync(order);
            return order;
        }




        /// <summary>
        /// 
        /// </summary>
        static async ValueTask<AudioOrder> buildAudioOrderAsync(
            this AudioDefineData define, ZipArchive archive, AudioSource audioSource, CancellationToken ct)
        {
            var audiopath = define.AudioFilePath;

            var clip = await audiopath.LoadAudioClipExAsync(archive, ct);

            return define.toAudioOrder(clip, audioSource);
        }




        /// <summary>
        /// 
        /// </summary>
        static async ValueTask<ModelOrder> buildBackGroundModelOrderAsync(
            this ModelDefineData define, VmdStreamDataCache cache, ZipArchive archive, CancellationToken ct)
        {
            var bgpath = define.ModelFilePath;

            var model = await cache.loadModelAsync(bgpath, archive, ct);
            //var model = await bgpath.LoadModelExAsync(archive, ct);

            return define.toBackGroundOrder(model);
        }




        /// <summary>
        /// 
        /// </summary>
        static async ValueTask<MotionOrder> buildMotionOrderAsync(
            this DanceMotionDefineData define, VmdStreamDataCache cache, ZipArchive archive, CancellationToken ct)
        {
            var modelpath = define.Model.ModelFilePath;
            var vmdpath = define.Animation.AnimationFilePath;
            var facepath = define.Animation.FaceMappingFilePath;

            var modelTask =
                Task.Run(async () => await cache.loadModelAsync(modelpath, archive, ct) as dynamic);
            var streamdataTask =
                Task.Run(async () => await cache.loadVmdAsync(vmdpath, facepath, archive, ct) as dynamic);

            var data = await Task.WhenAll(modelTask, streamdataTask);


            var model = data[0] as GameObject;
            var vmddata = (VmdStreamData)data[1].Item1;
            var facemap = (VmdFaceMapping)data[1].Item2;

            await Awaitable.MainThreadAsync();
            return define.toMotionOrder(vmddata, facemap, model);
        }






        static Task<GameObject> loadModelAsync(
            this VmdStreamDataCache stocker, PathUnit modelpath, ZipArchive archive, CancellationToken ct)
        =>
            (modelpath.IsResource() ? null : stocker)?.GetOrLoadModelAsync(modelpath, archive, ct)
            ??
            modelpath.LoadModelExAsync(archive, ct).AsTask();


        static Task<(VmdStreamData, VmdFaceMapping)> loadVmdAsync(
            this VmdStreamDataCache cache, PathUnit vmdpath, PathUnit facepath, ZipArchive archive, CancellationToken ct)
        =>
            cache?.GetOrLoadVmdStreamDataAsync(vmdpath, facepath, archive, ct)
            ??
            VmdData.LoadVmdStreamDataExAsync(vmdpath, facepath, archive, ct).AsTask();





        static AudioOrder toAudioOrder(
            this AudioDefineData define, AudioClipAsDisposable clip, AudioSource audioSource)
        =>
            new()
            {
                AudioSource = audioSource,
                AudioClip = clip,
                Volume = define.Volume,
                DelayTime = define.DelayTime,
            };


        static ModelOrder toBackGroundOrder(
            this ModelDefineData define, GameObject model)
        =>
            new()
            {
                Model = model,
                Position = define.Position,
                Rotation = define.Rotation,
                Scale = define.Scale,
            };

        static MotionOrder toMotionOrder(
            this DanceMotionDefineData define, VmdStreamData vmddata, VmdFaceMapping facemap, GameObject model)
        =>
            new()
            {
                Model = model,
                FaceRenderer = model.AsUnityNull()?.FindFaceRenderer(),

                vmddata = vmddata,
                bone = model.AsUnityNull()?.GetComponent<Animator>().BuildVmdPlayableJobTransformMappings() ?? default,
                face = facemap.BuildStreamingFace(),

                DelayTime = define.Animation.DelayTime,
                BodyScale = define.Options.BodyScaleFromHuman,
                FootIkMode = define.Options.FootIkMode,

                Position = define.Model.Position,
                Rotation = define.Model.Rotation,
                Scale = define.Model.Scale,
            };


        /// <summary>
        /// オーディオクリップ、モデルゲームオブジェクト、アニメーションストリームデータ、ボーンデータのうち、破棄が必要なリソースだけ破棄する。
        /// モデルに関しては、現在アクティブなゲームオブジェクトだけ Destroy() する。
        /// モデルストックによって非アクティブになっている場合は、Destroy() されない。
        /// ただし、Destroy() が反映されるタイミングには注意すること。（おそらく Destory() の次のフレームから）
        /// </summary>
        static Action buildDisposeAction(this (AudioOrder audio, ModelOrder[] bgs, MotionOrder[] motions) order) =>
            () =>
            {
                order.audio.AudioClip.Dispose();

                foreach (var bg in order.bgs)
                {
                    bg.Model.AsUnityNull(o => o?.activeSelf)?.Destroy();
                }

                foreach (var m in order.motions)
                {
                    //m.face.Dispose();
                    m.bone.Dispose();
                    m.vmddata?.Dispose();
                    m.Model.AsUnityNull(o => o?.activeSelf)?.Destroy();
                }
            };


        static Order toOrder(AudioOrder audioOrder, ModelOrder[] bgOrders, MotionOrder[] motionOrders, CancellationToken ct)
        {
            var disposeAction = (audioOrder, bgOrders, motionOrders).buildDisposeAction();
            ct.ThrowIfCancellationRequested(disposeAction);

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
