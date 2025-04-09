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

namespace AnimLite.DancePlayable
{
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;

    using AnimLite.Loader;
    using AnimLite.Vmd;
    using AnimLite.Vrm;
    //using static AnimLite.DancePlayable.DanceGraphy;
    //using static AnimLite.Loader.AudioLoader;

    //using static UnityEditor.Progress;


    public class DanceGraphy : IAsyncDisposable
    {

        public PlayableGraph graph { get; private set; }

        Func<ValueTask> DisposeAction = () => new ValueTask();


        public float TotalTime;// 暫定、ちゃんとした方法にしたい



        //public Awaitable WaitForPlayingAsync() => this._waitForPlaying.Awaitable;

        //AwaitableCompletionSource _waitForPlaying = new();


        //public void Play()
        //{
        //    this.graph.Play();
        //    this._waitForPlaying.SetResult();
        //}

        //public void Stop()
        //{
        //    if (this.graph.IsPlaying())
        //    {
        //        this.graph.Stop();

        //        if (!this.graph.IsDone())
        //            this._waitForPlaying.SetCanceled();
        //    }

        //    this._waitForPlaying.Reset();
        //}

        //public void Evaluate(float deltaTime)
        //{
        //    this.graph.Evaluate(deltaTime);
        //}


        public struct Order
        {
            public AudioOrder Audio;
            public ModelOrder[] BackGrouds;
            public MotionOrderBase[] Motions;

            public Func<ValueTask> DisposeAction;
        }

        public class ModelOrder : IAsyncDisposable
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

        public class AudioOrder : IAsyncDisposable
        {
            public AudioSource AudioSource;
            public Instance<AudioClip> AudioClip;

            public float Volume;
            public float DelayTime;

            public virtual ValueTask DisposeAsync()
            {
                return this.AudioClip.DisposeNullableAsync();
            }
        }

        public abstract class MotionOrderBase : ModelOrder
        {
            public SkinnedMeshRenderer FaceRenderer;

            public virtual bool IsMotionBlank => true;
        }
        public class MotionOrder : MotionOrderBase
        {
            public Instance<VmdStreamData> vmd;

            public TransformHandleMappings bone;
            public VrmExpressionMappings face;

            public float DelayTime;

            public VmdFootIkMode FootIkMode;
            public float BodyScale;
            public float FootScale;
            public float MoveScale;

            public override bool IsMotionBlank => this.vmd is null;
            public override async ValueTask DisposeAsync()
            {
                await base.DisposeAsync();

                //this.face.Dispose();
                this.bone.Dispose();
                await this.vmd.DisposeNullableAsync();

                await Awaitable.MainThreadAsync();// 不要かも
                await this.Model.DisposeNullableAsync();
            }
        }
        public class MotionOrderWithAnimationClip : MotionOrderBase
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
        }
        //public class MotionOrder
        //{
        //    public GameObject Model;
        //    public SkinnedMeshRenderer FaceRenderer;

        //    public VmdStreamData vmddata;

        //    public TransformHandleMappings bone;
        //    public VrmExpressionMappings face;

        //    public float DelayTime;

        //    public VmdFootIkMode FootIkMode;
        //    public float BodyScale;

        //    //public bool OverWritePositionAndRotation;
        //    public Vector3 Position;
        //    public Quaternion Rotation;
        //    public float Scale;
        //}


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


            showBackGround_(order.BackGrouds);

            createMotionPlayables_(graph, order.Motions);

            createAudioPlayable_(graph, order.Audio);

            if (order.Audio is not null)
                order.Audio.AudioSource.volume = order.Audio.Volume;// playable の weight で変えるべきとも思うが、audio の playable output にそういう機能はないようなのでとりあえずここで


            var totalTime = graph.GetRootPlayableCount() > 0
                ? (float)graph.GetRootPlayable(0).GetDuration()
                : 0.0f;

            graph.AdjustPlayableLength();

            return new DanceGraphy
            {
                graph = graph,

                DisposeAction = () => new ValueTask(),

                TotalTime = totalTime,//
            };


            static void showBackGround_(ModelOrder[] orders)
            {
                //if (orders == null) return;

                foreach (var order in orders)
                {
                    overwritePosition_(order);
                    overwriteScale_(order);
                }
            }


            static void createAudioPlayable_(PlayableGraph graph, AudioOrder order)
            {
                //if (order == null) return;
                if (order.AudioClip?.Value?.AsUnityNull() is null) return;
                if (order.AudioSource.IsUnityNull()) return;

                graph.CreateAudio(order.AudioSource, order.AudioClip, order.DelayTime);
            }


            static void createMotionPlayables_(PlayableGraph graph, IEnumerable<MotionOrderBase> orders)
            {
                //if (orders == null) return;

                foreach (var order in orders)
                {
                    if (order is MotionOrder mo)
                    {
                        createFaceMotion_(mo);
                        createBodyMotion_(mo);
                    }
                    else if (order is MotionOrderWithAnimationClip moac)
                    {
                        createBodyMotion_withAnimationClip_(moac);
                    }

                    overwritePosition_(order);
                    overwriteScale_(order);
                }

                return;


                void createFaceMotion_(MotionOrder order)
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

                void createBodyMotion_(MotionOrder order)
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
                    var job = anim.create(order.bone, pkf, rkf, timer, order.FootIkMode, order.MoveScale, order.BodyScale, order.FootScale);
                    graph.CreateVmdAnimationJobWithSyncScript(anim, job, timer, order.DelayTime);
                }

                void createBodyMotion_withAnimationClip_(MotionOrderWithAnimationClip order)
                {
                    //if (order is null) return;
                    if (order.AnimationClip is null) return;
                    if (order.Model.IsUnityNull()) return;

                    var anim = order.Model.Value.GetComponent<Animator>();
                    graph.CreateClipAnimation(anim, order.AnimationClip, order.DelayTime);
                }
            }

            static void overwritePosition_(ModelOrder order)
            {
                if (order.Model.IsUnityNull()) return;
                //if (!order.OverWritePositionAndRotation) return;

                var tf = order.Model.Value.transform;
                tf.position = order.Position;
                tf.rotation = order.Rotation;
            }
            static void overwriteScale_(ModelOrder order)
            {
                if (order.Model.IsUnityNull()) return;
                if (order.Scale == 0.0f) return;

                var tf = order.Model.Value.transform;
                tf.localScale = new Vector3(order.Scale, order.Scale, order.Scale);
            }

        }

    }
}