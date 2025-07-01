using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using Unity.Mathematics;

namespace AnimLite.DancePlayable
{
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;

    using AnimLite.Loader;
    using AnimLite.Vmd;
    using AnimLite.Vrm;
    using AnimLite.Vmd.experimental;
    using AnimLite.Vmd.experimental.Job;

    //using static AnimLite.DancePlayable.DanceGraphy;
    //using static AnimLite.Loader.AudioLoader;

    //using static UnityEditor.Progress;
    using VmdPosFinder = KeyFinderWithoutProcedure<float4, Key4CatmulPos, Clamp, Key4StreamCache<float4>, StreamIndex>;
    using VmdRotFinder = KeyFinderWithoutProcedure<quaternion, Key4CatmulRot, Clamp, Key4StreamCache<quaternion>, StreamIndex>;

    public class DanceGraphy : IAsyncDisposable
    {

        public PlayableGraph graph { get; private set; }

        Func<ValueTask> DisposeAction = () => new ValueTask();


        //public float TotalTime;// 暫定、ちゃんとした方法にしたい
        public DanceTimeKeeper timekeeper { get; private set; }



        public JobBuffers<VmdPosFinder, VmdRotFinder> jobbuf;


        

        public struct Order
        {
            public AudioOrder Audio;
            public BgModelOrder[] BackGrouds;
            public MotionOrderBase[] Motions;

            public Func<ValueTask> DisposeAction;
        }


        interface IOrderUnit
        {
            float TotalTime { get; }
        }

        public class ModelOrderBase : IAsyncDisposable
        {
            public Instance<GameObject> Model;

            public Vector3 Position;
            public Quaternion Rotation;
            public float Scale;

            public virtual ValueTask DisposeAsync()
            {
                return this.Model.DisposeNullableAsync();
            }
        }
        public class BgModelOrder : ModelOrderBase
        {

        }

        public class AudioOrder : IAsyncDisposable, IOrderUnit
        {
            public AudioSource AudioSource;
            public Instance<AudioClip> AudioClip;

            public float Volume;
            public float DelayTime;

            public virtual ValueTask DisposeAsync()
            {
                return this.AudioClip.DisposeNullableAsync();
            }
            public float TotalTime => this.AudioClip.Value is not null
                ? this.AudioClip.Value.length - this.DelayTime
                : 0.0f;
        }

        public abstract class MotionOrderBase : ModelOrderBase
        {
            public SkinnedMeshRenderer FaceRenderer;

            public virtual bool IsMotionBlank => true;
        }
        public class MotionOrder : MotionOrderBase, IOrderUnit
        {
            public Instance<VmdStreamData> vmd;

            public TransformMappings bone;
            public VrmExpressionMappings face;

            public float DelayTime;

            //public VmdFootIkMode FootIkMode;
            //public float BodyScale;
            //public float FootScale;
            //public float MoveScale;
            public MotionOptionsJson Options;

            public override bool IsMotionBlank => this.vmd is null;
            public override async ValueTask DisposeAsync()
            {
                await base.DisposeAsync();

                //this.face.Dispose();
                //this.bone.Dispose();
                await this.vmd.DisposeNullableAsync();

                await Awaitable.MainThreadAsync();// 不要かも
                await this.Model.DisposeNullableAsync();
            }
            public float TotalTime => this.vmd.Value.RotationStreams.Streams.GetLastKeyTime() - this.DelayTime;
        }
        public class MotionOrderOld : MotionOrder
        {
            public new TransformHandleMappings bone;

            public override async ValueTask DisposeAsync()
            {
                await base.DisposeAsync();
                this.bone.Dispose();
            }
        }
        public class MotionOrderWithAnimationClip : MotionOrderBase, IOrderUnit
        {
            public Instance<AnimationClip> AnimationClip;
            public float DelayTime;

            public override bool IsMotionBlank => this.AnimationClip is null;
            public override async ValueTask DisposeAsync()
            {
                await base.DisposeAsync();

                await Awaitable.MainThreadAsync();// 不要かも
                await this.AnimationClip.DisposeNullableAsync();
                await this.Model.DisposeNullableAsync();
            }
            public float TotalTime => this.AnimationClip is not null
                ? this.AnimationClip.Value.length - this.DelayTime
                : 0.0f;
        }


        public async ValueTask DisposeAsync()
        {
            this.graph.Stop();
            this.graph.Destroy();

            await this.DisposeAction();
        }


        public static DanceGraphy CreateGraphy(Order order)
        {
            var graphy = CreateGraphyWithoutDispose(order);

            graphy.DisposeAction = order.DisposeAction;

            return graphy;
        }

        public static DanceGraphy CreateGraphyWithoutDispose(Order order)
        {

            var graph = PlayableGraph.Create();


            createBackGroundCollider_(order.BackGrouds, order.Motions);
            showBackGround_(order.BackGrouds);

            var jobbuf = createMotionPlayables_(graph, order.Motions);

            createAudioPlayable_(graph, order.Audio);

            if (order.Audio?.AudioSource.AsUnityNull() is not null)
                order.Audio.AudioSource.volume = order.Audio.Volume;// playable の weight で変えるべきとも思うが、audio の playable output にそういう機能はないようなのでとりあえずここで

            var timeKeeper = new DanceTimeKeeper(graph);
            graph.AdjustPlayableLength();

            return new DanceGraphy
            {
                graph = graph,

                DisposeAction = () => new ValueTask(),

                //TotalTime = totalTime,//
                //timekeeper = new DanceTimeKeeper(graph),
                timekeeper = timeKeeper,

                jobbuf = jobbuf,
            };



            static void showBackGround_(BgModelOrder[] orders)
            {
                //if (orders == null) return;

                foreach (var order in orders)
                {
                    overwritePosition_(order);
                    overwriteScale_(order);
                }
            }
            static void createBackGroundCollider_(BgModelOrder[] orders, MotionOrderBase[] motions)
            {
                var useCollider = motions
                    .OfType<MotionOrder>()
                    .Any(x => (x.Options.FootIkMode & VmdFootIkMode.off_with_ground) != 0);

                if (!useCollider) return;


                var qmf = orders
                    .Where(order => order.Model is not null)
                    .SelectMany(order => order.Model.Value.GetComponentsInChildren<MeshFilter>())
                    .Where(m => !m.IsUnityNull())
                    .Select(x => (go:x.gameObject, mesh:x.sharedMesh));
                var qsm = orders
                    .Where(order => order.Model is not null)
                    .SelectMany(order => order.Model.Value.GetComponentsInChildren<SkinnedMeshRenderer>())
                    .Where(m => !m.IsUnityNull())
                    .Select(x => (go: x.gameObject, mesh: x.sharedMesh));

                Enumerable.Concat(qmf, qsm)
                    .ForEach(x =>
                    {
                        var mc = x.go.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = x.mesh;
                        mc.gameObject.layer = LayerMask.NameToLayer(FootIkOperator.defaultHitLayer);
                    });
            }


            static void createAudioPlayable_(PlayableGraph graph, AudioOrder order)
            {
                //if (order == null) return;
                if (order.AudioClip?.Value?.AsUnityNull() is null) return;
                if (order.AudioSource.IsUnityNull()) return;

                graph.CreateAudio(order.AudioSource, order.AudioClip, order.DelayTime);
            }


            static JobBuffers<VmdPosFinder, VmdRotFinder> createMotionPlayables_(PlayableGraph graph, IEnumerable<MotionOrderBase> orders)
            {
                //if (orders == null) return;

                var modelParams = new List<ModelParams<VmdPosFinder, VmdRotFinder>>();

                foreach (var order in orders)
                {
                    overwritePosition_(order);
                    overwriteScale_(order);

                    if (order is MotionOrderOld mo_old)
                    {
                        createFaceMotionPlayable_(mo_old);
                        createBodyMotionPlayable_AnimationJob_(mo_old);
                    }
                    else if (order is MotionOrder mo)
                    {
                        createFaceMotionPlayable_(mo);
                        buildModelParameters_(mo).AddTo(modelParams);
                        mo.Model.Value.GetComponent<Animator>().AdjustBbox();
                    }
                    else if (order is MotionOrderWithAnimationClip moac)
                    {
                        createBodyMotionPlayable_AnimationClip_(moac);
                    }
                }

                return createBodyMotionPlayable_(modelParams);



                void createFaceMotionPlayable_(MotionOrder order)
                {
                    if (!order.face.IsCreated) return;
                    if (order.FaceRenderer.IsUnityNull()) return;
                    if (order.vmd is null) return;

                    var timer = new StreamingTimer(order?.vmd?.Value?.RotationStreams.Streams.GetLastKeyTime() ?? default);

                    var fkf = order.vmd.Value.FaceStreams
                        //.ToKeyFinderWith<Key2NearestShift, Clamp>();
                        .ToKeyFinderWith<Key4Catmul, Clamp>();

                    graph.CreateVmdFaceAnimation(order.Model, fkf, order.face, timer, order.DelayTime);
                }

                JobBuffers<VmdPosFinder, VmdRotFinder> createBodyMotionPlayable_(
                    IEnumerable<ModelParams<VmdPosFinder, VmdRotFinder>> modelparams)
                {
                    if (modelparams.IsEmpty()) return default;

                    var countlist = modelparams.CountParams();
                    var buf = modelparams.BuildJobBuffers(countlist);

                    var qTotalTime =
                        from p in modelparams
                        from t in p.model_timeOptions
                        select t.timer.TotalTime - t.delayTime
                        ;

                    var objs = modelparams.Select(x => x.model_data.anim);
                    graph.CreateVmdMotionJobWithSyncScript(objs, buf, qTotalTime.Max());

                    return buf;
                }
                ModelParams<VmdPosFinder, VmdRotFinder> buildModelParameters_(MotionOrder order)
                {
                    //if (order is null) return;
                    if (order.vmd is null) return null;
                    if (order.Model.IsUnityNull()) return null;

                    //var timer = new StreamingTimer(order?.vmd?.Value?.RotationStreams.Streams.GetLastKeyTime() ?? default);

                    var pkf = order.vmd.Value.PositionStreams
                        .ToKeyFinderWith<Key4CatmulPos, Clamp>();

                    var rkf = order.vmd.Value.RotationStreams
                        .ToKeyFinderWith<Key4CatmulRot, Clamp>();

                    var anim = order.Model.Value.GetComponent<Animator>();
                    var bodyop = anim.ToVmdBodyTransformMotionOperator(order.bone)
                        .WithScales(anim, order.Options.MoveScaleFromHuman, order.Options.BodyScaleFromHuman);
                    var footop = anim.ToVmdFootIkTransformOperator(order.bone)
                        .WithScales(anim, order.Options.MoveScaleFromHuman, order.Options.FootScaleFromHuman)
                        .WithIkUsage(order.vmd, order.Options.FootIkMode, order.Options.GroundHitDistance, order.Options.GroundHitOriginOffset);
                    var param = anim.BuildJobParams(order.bone, pkf, rkf, bodyop, footop, order.DelayTime);
                    return param;
                }
                void createBodyMotionPlayable_AnimationJob_(MotionOrderOld order)
                {
                    //if (order is null) return;
                    if (order.vmd is null) return;
                    if (order.Model.IsUnityNull()) return;

                    var timer = new StreamingTimer(order?.vmd?.Value?.RotationStreams.Streams.GetLastKeyTime() ?? default);

                    var pkf = order.vmd.Value.PositionStreams
                        .ToKeyFinderWith<Key4CatmulPos, Clamp>();

                    var rkf = order.vmd.Value.RotationStreams
                        .ToKeyFinderWith<Key4CatmulRot, Clamp>();

                    var anim = order.Model.Value.GetComponent<Animator>();
                    var bodyop = anim.ToVmdBodyMotionOperator<TransformHandleMappings, TfHandle>(order.bone)
                        .WithScales(anim, order.Options.MoveScaleFromHuman, order.Options.BodyScaleFromHuman);
                    var footop = anim.ToVmdFootIkOperator<TransformHandleMappings, TfHandle>(order.bone)
                        .WithScales(anim, order.Options.MoveScaleFromHuman, order.Options.FootScaleFromHuman)
                        .WithIkUsage(order.vmd, order.Options.FootIkMode, order.Options.GroundHitDistance, order.Options.GroundHitOriginOffset);
                    var job = anim.create(order.bone, pkf, rkf, timer, bodyop, footop);
                    graph.CreateVmdAnimationJobWithSyncScript(anim, job, timer, order.DelayTime);
                }

                void createBodyMotionPlayable_AnimationClip_(MotionOrderWithAnimationClip order)
                {
                    //if (order is null) return;
                    if (order.AnimationClip is null) return;
                    if (order.Model.IsUnityNull()) return;

                    var anim = order.Model.Value.GetComponent<Animator>();
                    graph.CreateClipAnimation(anim, order.AnimationClip, order.DelayTime);
                }
            }

            static void overwritePosition_(ModelOrderBase order)
            {
                if (order.Model.IsUnityNull()) return;
                //if (!order.OverWritePositionAndRotation) return;

                var tf = order.Model.Value.transform;
                tf.position += order.Position;
                tf.rotation *= order.Rotation;
            }
            static void overwriteScale_(ModelOrderBase order)
            {
                if (order.Model.IsUnityNull()) return;
                if (order.Scale == 0.0f) return;

                var tf = order.Model.Value.transform;
                tf.localScale = (float3)tf.localScale * order.Scale;
            }

        }


        public class DanceTimeKeeper
        {
            public float CurrentTime => (float)this.timerPlayable.GetTime() - this.offset;
            public float TotalTime => (float)this.timerPlayable.GetDuration() - this.offset;

            Playable timerPlayable;
            float offset;

            public DanceTimeKeeper(PlayableGraph graph)
            {
                if (graph.GetOutputCount() == 0) return;

                this.timerPlayable = Enumerable.Range(0, graph.GetOutputCount())
                    //.Select(i => graph.GetRootPlayable(i))
                    .Select(i => graph.GetOutput(i).GetSourcePlayable())
                    .Select(p =>
                    {
                        for (; p.GetInputCount() > 0; p = p.GetInput(0)) ;
                        return p;
                    })
                    .Where(p => !double.IsInfinity(p.GetDuration()))
                    //.Do(x => Debug.Log($"playable {x.GetTime()} {x.GetDuration()} {x.GetPlayableType()}"))
                    .MaxBy(x => x.GetDuration() - x.GetTime())
                    .FirstOrDefault()
                    ;
                this.offset = (float)this.timerPlayable.GetTime();
            }

            public async Awaitable WaitForEndAsync(CancellationToken ct)
            {
                for (; ; )
                {
                    if (!this.timerPlayable.IsValid()) return;
                    if (this.timerPlayable.GetTime() >= this.timerPlayable.GetDuration()) break;
                    
                    await Awaitable.NextFrameAsync(ct);
                }
            }
        }
    }

}