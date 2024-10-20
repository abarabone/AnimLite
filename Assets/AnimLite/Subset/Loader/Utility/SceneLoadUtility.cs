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




    public static class DanceSceneLoader
    {

        /// <summary>
        /// true であれば、同じ zip 内のデータロードには１つの ZipArchive しかオープンしない。
        /// ただしその場合、並列的なロードは行われない。
        /// false であれば常に並列的なロードを行うが、それぞれ別個に ZipArchive をオープンする。
        /// </summary>
        public static bool IsSeaquentialLoadingInZip = false;


        /// <summary>
        /// FileStream で完全な非同期モードを使用する。ただしサイズが 3MB 以上のファイルのみ。
        /// </summary>
        public static bool UseAsyncModeForFileStreamApi = false;



        /// <summary>
        /// パスが zip であれば、ZipArchive を返す。それ以外は null を返す。
        /// IsSeaquentialLoadingInZip が false であれば、常に null を返す。
        /// </summary>
        public static async ValueTask<IArchive> OpenWhenZipAsync(this PathUnit path, IArchive fallback, CancellationToken ct)
        {
            ValueTask<Stream> openAsync_(PathUnit path) => path.OpenStreamFileOrWebAsync(ct);


            if (path.IsBlank()) return default;

            var (fullpath, queryString) = path.ToFullPath().DividToPathAndQueryString();
            fullpath.ThrowIfAccessedOutsideOfParentFolder();

            return fullpath.DividZipToArchiveAndEntry() switch
            {
                var (zippath, _) when zippath != "" =>
                    DanceSceneLoader.IsSeaquentialLoadingInZip
                        ? await openAsync_(zippath + queryString).OpenZipArchiveAwait(fallback)
                        : await (zippath + queryString).OpenDummyArchiveAsync(fallback, ct),
                _ =>
                    //fallback,
                    new NoArchive(fullpath, fallback),
            };
         }
        public static ValueTask<IArchive> OpenWhenZipAsync(this PathUnit path, CancellationToken ct) =>
            path.OpenWhenZipAsync(null, ct);




        //public static async ValueTask<DanceSetDefineData> LoadDanceSceneAsync(
        //    this PathUnit path, IArchive archive, CancellationToken ct)
        //{

        //    var json = await archive.LoadJsonAsync<DanceSetJson>(path, ct);

        //    return json.ToData();

        //}


        //public static async ValueTask<DanceSetDefineData> LoadDanceSceneAsync(
        //    this PathUnit path, CancellationToken ct)
        //{

        //    var json = await path.LoadJsonAsync<DanceSetJson>(ct);

        //    return json.ToData();

        //}



        //public static async IAsyncEnumerable<(DanceSetJson danceset, IArchive archive)> LoadDaceSceneAsync(
        //    this IEnumerable<PathUnit> jsonpaths, IArchive fallbackArchive, CancellationToken ct)
        //{
        //    IArchive ac = null;
        //    DanceSetJson ds = null;

        //    foreach (var path in jsonpaths)
        //    {
        //        ac = await path.OpenWhenZipAsync(archive, ct);

        //        ds = await ac.LoadJsonAsync<DanceSetJson>(path, ds, ct);

        //        yield return (ds, archive);
        //    }
        //}


        public struct ArchiveDanceScene : IDisposable
        {
            public IArchive archive;
            public DanceSetJson dancescene;

            public void Dispose() => this.archive?.Dispose();

            public void Deconstruct(out IArchive archive, out DanceSetJson dancescene)
            {
                archive = this.archive;
                dancescene = this.dancescene;
            }
        }

        public static ValueTask<ArchiveDanceScene> LoadDaceSceneAsync(
            this IEnumerable<PathUnit> jsonpaths, CancellationToken ct) =>
                jsonpaths.LoadDaceSceneAsync(null, null, ct);

        public static async ValueTask<ArchiveDanceScene> LoadDaceSceneAsync(
            this IEnumerable<PathUnit> jsonpaths, IArchive fallbackArchive, DanceSetJson dancescene, CancellationToken ct)
        {
            IArchive ac = fallbackArchive;
            DanceSetJson ds = dancescene;

            foreach (var path in jsonpaths)
            {
                ac = await path.OpenWhenZipAsync(ac, ct);

                ds = await ac.LoadJsonAsync<DanceSetJson>(path, ds, ct);
            }

            return new ArchiveDanceScene
            {
                archive = ac,
                dancescene = ds
            };
        }

    }

#nullable enable

    public static class SceneLoadUtilitiy
    {





        /// <summary>
        /// 
        /// </summary>
        public static ValueTask<Order> BuildDanceGraphyOrderAsync(
            this DanceSetJson ds, VmdStreamDataCache cache, IArchive? archive, AudioSource audioSource, CancellationToken ct)
        =>
            archive != null && DanceSceneLoader.IsSeaquentialLoadingInZip
                ? ds.BuildDanceGraphyOrderSequentialAsync(cache, archive, audioSource, ct)
                : ds.BuildDanceGraphyOrderParallelAsync(cache, archive, audioSource, ct);



        /// <summary>
        /// 
        /// </summary>
        public static async ValueTask<Order> BuildDanceGraphyOrderParallelAsync(
            this DanceSetJson ds, VmdStreamDataCache cache, IArchive? archive, AudioSource audioSource, CancellationToken ct)
        {
            var audioTask =
                Task.Run(async () => await ds.Audio.buildAudioOrderAsync(archive, audioSource, ct) as object);

            var bgTasks = ds.BackGrounds.Values.Select(define =>
                Task.Run(async () => await define.buildBackGroundModelOrderAsync(cache, archive, ct) as object));

            var motionTasks = ds.Motions.Values.Select(define =>
                Task.Run(async () => await define.buildMotionOrderParallelAsync(cache, archive, ct) as object));


            var orders = await audioTask.WrapEnumerable().Concat(bgTasks).Concat(motionTasks)
                .WhenAll();


            var audioOrder = orders.First() as AudioOrder;
            var bgOrders = orders.Skip(1).Take(ds.BackGrounds.Count).Cast<ModelOrder>().ToArray();
            var motionOrders = orders.Skip(1).Skip(ds.BackGrounds.Count).Cast<MotionOrderBase>().ToArray();

            var order = toOrder(audioOrder!, bgOrders, motionOrders, ct);
            await ds.OrverrideInformationIfBlankAsync(order);
            return order;
        }

        /// <summary>
        /// 
        /// </summary>
        public static async ValueTask<Order> BuildDanceGraphyOrderSequentialAsync(
            this DanceSetJson ds, VmdStreamDataCache cache, IArchive? archive, AudioSource audioSource, CancellationToken ct)
        {

            var audioOrder = await ds.Audio
                .buildAudioOrderAsync(archive, audioSource, ct);

            var bgOrders = await ds.BackGrounds.Values.ToAsyncEnumerable()
                .SelectAwait(define => define.buildBackGroundModelOrderAsync(cache, archive, ct))
                .ToArrayAsync();

            var motionOrders = await ds.Motions.Values.ToAsyncEnumerable()
                .SelectAwait(define => define.buildMotionOrderSequentialAsync(cache, archive, ct))
                .ToArrayAsync();


            var order = toOrder(audioOrder, bgOrders, motionOrders, ct);
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

            var clip = await archive.LoadAudioClipExAsync(audiopath, ct);

            return define.toAudioOrder(clip, audioSource);
        }




        /// <summary>
        /// 
        /// </summary>
        static async ValueTask<ModelOrder> buildBackGroundModelOrderAsync(
            this ModelDefineJson define, VmdStreamDataCache cache, IArchive? archive, CancellationToken ct)
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
            this DanceMotionDefineJson define, VmdStreamDataCache cache, IArchive? archive, CancellationToken ct)
        {
            var modelpath = define.Model.ModelFilePath;
            var facepath = define.Animation.FaceMappingFilePath;

            var vmdpath = define.Animation.AnimationFilePath;

            var modelTask =
                Task.Run(async () => await cache.loadModelAsync(modelpath, archive, ct) as object);
            var streamdataTask =
                Task.Run(async () => await cache.loadVmdAsync(vmdpath, facepath, archive, ct) as object);


            var data = await Task.WhenAll(modelTask, streamdataTask);


            var model = data[0] as Instance<GameObject>;
            var (vmddata, facemap) = ((VmdStreamData, VmdFaceMapping))data[1];

            await Awaitable.MainThreadAsync();
            return
                define.toMotionOrder(vmddata, facemap, model) as MotionOrderBase
                ??
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
            this DanceMotionDefineJson define, VmdStreamDataCache cache, IArchive? archive, CancellationToken ct)
        {
            var modelpath = define.Model.ModelFilePath;
            var vmdpath = define.Animation.AnimationFilePath;
            var facepath = define.Animation.FaceMappingFilePath;

            var model = await cache.loadModelAsync(modelpath, archive, ct);
            var (vmddata, facemap) = await cache.loadVmdAsync(vmdpath, facepath, archive, ct);

            await Awaitable.MainThreadAsync();
            return
                define.toMotionOrder(vmddata, facemap, model) as MotionOrderBase
                ??
                await define.toMotionOrderAwait(model, ct);
        }





        /// <summary>
        /// とりあえずざんていで stocker あるなしでキャッシュ使うか決める
        /// </summary>
        static async ValueTask<Instance<GameObject>?> loadModelAsync(
            this VmdStreamDataCache stocker, PathUnit modelpath, IArchive? archive, CancellationToken ct)
        {
            return stocker != null
                ? await ModelCacheDictionary.Instance.GetOrLoadAsync(modelpath, archive, ct)
                : await archive.LoadModelInstanceAsync(modelpath, PrototypeReleaseMode.AutoRelease, ct);
        }
        //static ValueTask<GameObject> loadModelAsync(
        //    this VmdStreamDataCache stocker, PathUnit modelpath, IArchive? archive, CancellationToken ct)
        //=>
        //    (modelpath.IsResource() ? null : stocker)?.GetOrLoadModelAsync(modelpath, archive, ct).AsValueTask()
        //    ??
        //    archive.LoadModelExAsync(modelpath, ct);


        static ValueTask<(VmdStreamData, VmdFaceMapping)> loadVmdAsync(
            this VmdStreamDataCache cache, PathList vmdpaths, PathUnit facepath, IArchive? archive, CancellationToken ct)
        =>
            cache?.GetOrLoadVmdStreamDataAsync(vmdpaths, facepath, archive, ct).AsValueTask()
            ??
            VmdData.LoadVmdStreamDataExAsync(vmdpaths, facepath, archive, ct);





        static AudioOrder toAudioOrder(
            this AudioDefineJson define, AudioClipAsDisposable clip, AudioSource audioSource)
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
            this DanceMotionDefineJson define, VmdStreamData vmddata, VmdFaceMapping facemap, Instance<GameObject>? model)
        =>
            //vmddata is not null
            //? new()
            new()
            {
                Model = model,
                FaceRenderer = model.AsUnityNull()?.FindFaceRenderer(),

                vmddata = vmddata,
                bone = model.AsUnityNull()?.GetComponent<Animator>()
                    .BuildVmdPlayableJobTransformMappings()
                    ??
                    default,
                face = facemap.BuildStreamingFace(),

                DelayTime = define.Animation.DelayTime,
                BodyScale = define.Options.BodyScaleFromHuman,
                FootIkMode = define.Options.FootIkMode,

                Position = define.Model.Position,
                Rotation = Quaternion.Euler(define.Model.EulerAngles),
                Scale = define.Model.Scale,
            }
            //: null;
            ;

        static async ValueTask<MotionOrderWithAnimationClip> toMotionOrderAwait(
            this DanceMotionDefineJson define, Instance<GameObject>? model, CancellationToken ct)
        =>
            new()
            {
                Model = model,
                FaceRenderer = model.AsUnityNull()?.FindFaceRenderer(),

                // vmd ロード失敗してたら、animation clip をリソースロードする
                AnimationClip = await define.Animation.AnimationFilePath.Paths
                    .DefaultIfEmpty("".ToPath())
                    .First()
                    .ToResourceName()
                    .loadAnimationClipFromResourceAsync(ct),

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
        static Action buildDisposeAction(this (AudioOrder audio, ModelOrder[] bgs, MotionOrderBase[] motions) order) =>
            () =>
            {
                order.audio.AudioClip.Dispose();

                foreach (var bg in order.bgs)
                {
                    //bg.Model.AsUnityNull(o => o?.activeSelf)?.Destroy();
                    bg.Model?.Dispose();
                }

                foreach (var m in order.motions)
                {
                    m.Dispose();
                }
            };


        static Order toOrder(AudioOrder audioOrder, ModelOrder[] bgOrders, MotionOrderBase[] motionOrders, CancellationToken ct)
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
