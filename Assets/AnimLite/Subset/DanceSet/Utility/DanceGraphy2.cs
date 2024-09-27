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
            public ModelOrder[] BackGrouds;
            public MotionOrder[] Motions;

            public Action DisposeAction;
        }

        public class ModelOrder
        {
            public GameObject Model;

            public Vector3 Position;
            public Quaternion Rotation;
            public float Scale;
        }

        public class AudioOrder
        {
            public AudioSource AudioSource;
            public AudioClipAsDisposable AudioClip;

            public float Volume;
            public float DelayTime;
        }

        public class MotionOrder : ModelOrder
        {
            public SkinnedMeshRenderer FaceRenderer;

            //public VmdStreamData vmddata;
            public VmdStreamData vmddata;

            public TransformHandleMappings bone;
            public VrmExpressionMappings face;

            public float DelayTime;

            public VmdFootIkMode FootIkMode;
            public float BodyScale;
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


        public void Dispose()
        {
            this.graph.Stop();
            this.graph.Destroy();

            this.DisposeAction();
        }


        public static DanceGraphy2 CreateGraphy(Order order)
        {
            var graphy = CreateGraphyWithoutDispose(order);

            graphy.DisposeAction = order.DisposeAction;

            return graphy;
        }

        public static DanceGraphy2 CreateGraphyWithoutDispose(Order order)
        {

            var graph = PlayableGraph.Create();


            showBackGround_(order.BackGrouds);

            createMotionPlayables_(graph, order.Motions);

            createAudioPlayable_(graph, order.Audio);

            order.Audio.AudioSource.volume = order.Audio.Volume;// playable の weight で変えるべきとも思うが、audio の playable output にそういう機能はないようなのでとりあえずここで


            return new DanceGraphy2
            {
                graph = graph,

                DisposeAction = () => { }
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
                if (order.AudioClip.clip.IsUnityNull()) return;
                if (order.AudioSource.IsUnityNull()) return;

                graph.CreateAudio(order.AudioSource, order.AudioClip, order.DelayTime);
            }


            static void createMotionPlayables_(PlayableGraph graph, IEnumerable<MotionOrder> orders)
            {
                //if (orders == null) return;

                foreach (var order in orders)
                {
                    var timer = new StreamingTimer(order?.vmddata?.RotationStreams.Streams.GetLastKeyTime() ?? default);

                    createBodyMotion_(order, timer);

                    createFaceMotion_(order, timer);

                    overwritePosition_(order);
                    overwriteScale_(order);
                }

                return;


                void createBodyMotion_(MotionOrder order, StreamingTimer timer)
                {
                    //if (order is null) return;
                    if (order.vmddata is null) return;
                    if (order.Model.IsUnityNull()) return;

                    var pkf = order.vmddata.PositionStreams
                        .ToKeyFinderWith<Key4CatmulPos, Clamp>();

                    var rkf = order.vmddata.RotationStreams
                        .ToKeyFinderWith<Key4CatmulRot, Clamp>();

                    var anim = order.Model.GetComponent<Animator>();
                    var job = anim.create(order.bone, pkf, rkf, timer, order.FootIkMode, order.BodyScale);
                    graph.CreateVmdAnimationJobWithSyncScript(anim, job, timer, order.DelayTime);
                }

                void createFaceMotion_(MotionOrder order, StreamingTimer timer)
                {
                    if (!order.face.IsCreated) return;
                    if (order.FaceRenderer.IsUnityNull()) return;
                    if (order.vmddata is null) return;

                    var fkf = order.vmddata.FaceStreams
                        //.ToKeyFinderWith<Key2NearestShift, Clamp>();
                        .ToKeyFinderWith<Key4Catmul, Clamp>();

                    graph.CreateVmdFaceAnimation(order.Model, fkf, order.face, timer, order.DelayTime);
                }
            }

            static void overwritePosition_(ModelOrder order)
            {
                if (order.Model.IsUnityNull()) return;
                //if (!order.OverWritePositionAndRotation) return;

                var tf = order.Model.transform;
                tf.position = order.Position;
                tf.rotation = order.Rotation;
            }
            static void overwriteScale_(ModelOrder order)
            {
                if (order.Model.IsUnityNull()) return;
                if (order.Scale == 0.0f) return;

                var tf = order.Model.transform;
                tf.localScale = new Vector3(order.Scale, order.Scale, order.Scale);
            }

        }

    }
}