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
using Unity.Mathematics;

namespace AnimLite.Utility
{
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;

    using AnimLite.Vrm;
    using AnimLite.Vmd;
    using static AnimLite.DancePlayable.DanceGraphy2;


    public static class DanceSceneLoader
    {

        public static bool UseSeaquentialLoading = false;

        public static ZipMode ZipLoaderMode = ZipMode.ParallelOpenMultiFiles; 
        public enum ZipMode
        {
            Sequential,
            ParallelOpenSingleFile,
            ParallelOpenMultiFiles,
        }


        /// <summary>
        /// FileStream で完全な非同期モードを使用する。ただしサイズが 3MB 以上のファイルのみ。
        /// </summary>
        public static bool UseAsyncModeForFileStreamApi = false;



        /// <summary>
        /// 
        /// </summary>
        public static async ValueTask<IArchive> OpenArchiveAsync(this PathUnit archivepath, IArchive fallback, CancellationToken ct)
        {
            if (archivepath.IsBlank()) return fallback;

            var (fullpath, queryString) = archivepath.ToFullPath().DividToPathAndQueryString();
            fullpath.ThrowIfAccessedOutsideOfParentFolder();

            return fullpath switch
            {
                _ when fullpath.IsZipArchive() || fullpath.IsZipEntry() =>
                    DanceSceneLoader.ZipLoaderMode switch
                    {
                        ZipMode.Sequential when DanceSceneLoader.UseSeaquentialLoading =>
                            await fullpath.OpenZipArchiveSequentialAsync(queryString, fallback, ct),
                        ZipMode.Sequential =>
                            await fullpath.OpenZipArchiveSequentialConcurrentAsync(queryString, fallback, ct),
                        ZipMode.ParallelOpenSingleFile =>
                            await fullpath.OpenZipArchiveParallelAsync(queryString, fallback, ct),
                        ZipMode.ParallelOpenMultiFiles =>
                            await fullpath.OpenDummyArchiveParallelAsync(queryString, fallback, ct),
                        _ =>
                            default,
                    },
                _ =>
                    fullpath.OpenFolderArchive(fallback, ct),
            };
         }

        public static ValueTask<IArchive> OpenArchiveAsync(this PathUnit path, CancellationToken ct) =>
            path.OpenArchiveAsync(null, ct);




        public static (PathUnit archivePath, PathUnit entryPath, QueryString queryString) DividPath(
            this PathUnit fullpath, string extensionList)
        {
            if (fullpath.IsResource()) return ("", fullpath, "");
            // いずれアセットバンドル？的なもののパスをかえせるようになりたい


            var (path, queryString) = fullpath.DividToPathAndQueryString();


            if (path.IsZipArchive())
            {
                return (path, "", queryString);
            }


            var isFile = extensionList.Split(';')
                .Where(ext => path.Value.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase))
                .Any();

            // フォルダ
            if (!isFile)
            {
                return (fullpath, "", "");
            }

            // ファイル
            {
                // 下記だと http://.../xx.xxx のときに / が \ に返られてしまうので使えない。しかも // は \ になる
                //var archivePath = Path.GetDirectoryName(path);
                //var entryPath = Path.GetFileName(path).ToPath();

                // / と \ が混在しているかも知れないので両方やる
                var ix = path.Value.LastIndexOf('/');
                var iy = path.Value.LastIndexOf('\\');
                var i = math.max(ix, iy);
                var archivePath = path.Value[..i];
                var entryPath = path.Value[(i+1)..];

                return (archivePath, entryPath, queryString);
            }
        }



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

        public static ValueTask<ArchiveDanceScene> LoadDanceSceneAsync(
            this IEnumerable<PathUnit> jsonpaths, CancellationToken ct) =>
                jsonpaths.LoadDaceSceneAsync(null, null, ct);

        public static async ValueTask<ArchiveDanceScene> LoadDaceSceneAsync(
            this IEnumerable<PathUnit> jsonpaths, IArchive fallbackArchive, DanceSetJson dancescene, CancellationToken ct)
        {
            IArchive ac = fallbackArchive;
            DanceSetJson ds = dancescene;

            foreach (var path in jsonpaths.Where(x => !x.IsBlank()))
            {
                var (archpath, entpath, qstr) = path.DividPath(".json");

                ac = await (archpath + qstr).OpenArchiveAsync(ac, ct);

                ds = await ac.LoadJsonAsync<DanceSetJson>(entpath, ds, ct);
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
            DanceSceneLoader.UseSeaquentialLoading
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
            this DanceSetJson ds, VmdStreamDataCache cache, IArchive? archive, AudioSource audioSource, CancellationToken ct)
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
                // animation clip が face やブレンドを整備するまでの暫定
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
            // animation clip が face やブレンドを整備するまでの暫定
            vmddata is not null
            ? new()
            //new()
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
            : null;
            //;
            
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
                        .LoadAnimationClipAsync(PrototypeReleaseMode.AutoRelease, ct))
                    .NullableAsync(x => x.InstantiateAsync()),

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
                await order.audio.AudioClip.DisposeAsync();

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
