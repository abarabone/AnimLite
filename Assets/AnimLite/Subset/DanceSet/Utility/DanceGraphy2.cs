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

    using AnimLite.Vmd;
    using AnimLite.Vrm;
    using static AnimLite.DancePlayable.DanceGraphy;
    using static AnimLite.Utility.AudioLoader;

    //using static UnityEditor.Progress;


    public class DanceGraphy2 : IDisposable
    {

        public PlayableGraph graph { get; private set; }

        Action DisposeAction = () => { };


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
            public MotionOrder[] Motions;

            public Action DisposeAction;
        }

        public class AudioOrder
        {
            public AudioSource AudioSource;
            public AudioClipAsDisposable AudioClip;

            public float Volume;
            public float DelayTime;
        }

        public class MotionOrder
        {
            public Animator ModelAnimator;
            public SkinnedMeshRenderer FaceRenderer;

            public VmdStreamData vmddata;

            public TransformHandleMappings bone;
            public VrmExpressionMappings face;

            public float DelayTime;

            public VmdFootIkMode FootIkMode;
            public float BodyScale;

            public bool OverWritePositionAndRotation;
            public Vector3 Position;
            public Quaternion Rotation;
            public float Scale;
        }


        public void Dispose()
        {
            this.graph.Stop();
            this.graph.Destroy();

            this.DisposeAction();
        }


        public static DanceGraphy2 CreateGraphy(Order order)
        {
            var graphy = CreateGraphyWithoutDispose(order.Motions, order.Audio);

            graphy.DisposeAction = order.DisposeAction;

            return graphy;
        }

        public static DanceGraphy2 CreateGraphyWithoutDispose(IEnumerable<MotionOrder> motions, AudioOrder audio)
        {

            var graph = PlayableGraph.Create();


            createMotionPlayables_(graph, motions);

            createAudioPlayable_(graph, audio);

            audio.AudioSource.volume = audio.Volume;// playable の weight で変えるべきとも思うが、audio の playable output にそういう機能はないようなのでとりあえずここで


            return new DanceGraphy2
            {
                graph = graph,

                DisposeAction = () => { }
            };



            static void createAudioPlayable_(PlayableGraph graph, AudioOrder order)
            {
                if (order == null) return;
                if (order.AudioSource.IsUnityNull()) return;

                graph.CreateAudio(order.AudioSource, order.AudioClip, order.DelayTime);
            }


            static void createMotionPlayables_(PlayableGraph graph, IEnumerable<MotionOrder> orders)
            {
                if (orders == null) return;

                foreach (var order in orders)
                {
                    var timer = new StreamingTimer(order.vmddata.RotationStreams.Streams.GetLastKeyTime());

                    createBodyMotion_(order, timer);

                    createFaceMotion_(order, timer);

                    overwritePosition_(order);
                    overwriteScale_(order);
                }

                return;


                void createBodyMotion_(MotionOrder order, StreamingTimer timer)
                {
                    var pkf = order.vmddata.PositionStreams
                        .ToKeyFinderWith<Key4CatmulPos, Clamp>();

                    var rkf = order.vmddata.RotationStreams
                        .ToKeyFinderWith<Key4CatmulRot, Clamp>();

                    var job = order.ModelAnimator.create(order.bone, pkf, rkf, timer, order.FootIkMode, order.BodyScale);

                    graph.CreateVmdAnimationJobWithSyncScript(order.ModelAnimator, job, order.DelayTime);
                }

                void createFaceMotion_(MotionOrder order, StreamingTimer timer)
                {
                    if (order.face.Expressions == default) return;
                    if (order.FaceRenderer.AsUnityNull() == default) return;

                    var fkf = order.vmddata.FaceStreams
                        .ToKeyFinderWith<Key2NearestShift, Clamp>();

                    graph.CreateVmdFaceAnimation(order.ModelAnimator, fkf, order.face, timer, order.DelayTime);
                }

                void overwritePosition_(MotionOrder order)
                {
                    if (!order.OverWritePositionAndRotation) return;

                    var tf = order.ModelAnimator.transform;
                    tf.position = order.Position;
                    tf.rotation = order.Rotation;
                }
                void overwriteScale_(MotionOrder order)
                {
                    if (order.Scale == 0.0f) return;

                    var tf = order.ModelAnimator.transform;
                    tf.localScale = new Vector3(order.Scale, order.Scale, order.Scale);
                }
            }


        }

    }
}