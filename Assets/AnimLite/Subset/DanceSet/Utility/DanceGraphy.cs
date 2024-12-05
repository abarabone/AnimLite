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
    //using static UnityEditor.Progress;


    public class DanceGraphy : IDisposable
    {

        public PlayableGraph graph { get; private set; }

        //MotionResource[] resources;
        Action DisposeAction = () => { };



        public class AudioOrder
        {
            public AudioSource AudioSource;
            public AudioClipAsDisposable AudioClip;

            public float DelayTime;
        }

        public class MotionOrder
        {
            public GameObject Model;
            public SkinnedMeshRenderer FaceRenderer;

            public float DelayTime;

            public VmdFootIkMode FootIkMode;
            public float BodyScale;

            public bool OverWritePositionAndRotation;
            public Vector3 Position;
            public Quaternion Rotation;
        }

        public class AudioResource : IDisposable
        {
            public AudioClip clip;

            public void Dispose()
            {
                UnityEngine.Object.Destroy(this.clip);
            }
        }

        public class MotionResource : IDisposable
        {
            public Animator model;

            public VmdStreamData vmddata;

            public TransformHandleMappings bone;
            public VrmExpressionMappings face;


            public void Dispose()
            {
                this.vmddata.Dispose();
                this.bone.Dispose();
                //this.face.Dispose();
            }
        }


        public void Dispose()
        {
            this.graph.Stop();
            this.graph.Destroy();

            this.DisposeAction();
        }


        public static DanceGraphy CreateGraphy(
            IEnumerable<(MotionOrder, MotionResource)> motions, AudioOrder audio)
        {
            var graphy = CreateGraphyWithoutDispose(motions, audio);

            graphy.DisposeAction = () =>
            {
                motions
                    .ForEach(x => x.Item2.Dispose());

                "resources disposed".ShowDebugLog();
            };
            return graphy;
        }

        public static DanceGraphy CreateGraphyWithoutDispose(
            IEnumerable<(MotionOrder, MotionResource)> motions, AudioOrder audio)
        {

            var graph = PlayableGraph.Create();


            createMotionPlayables_(graph, motions);

            createAudioPlayable_(graph, audio);


            return new DanceGraphy
            {
                graph = graph,

                DisposeAction = () => { }
            };


            static void createAudioPlayable_(PlayableGraph graph, AudioOrder audio)
            {
                if (audio == null) return;
                if (audio.AudioSource.IsUnityNull()) return;

                graph.CreateAudio(audio.AudioSource, audio.AudioClip, audio.DelayTime);
            }


            static void createMotionPlayables_(
                PlayableGraph graph, IEnumerable<(MotionOrder, MotionResource)> motions)
            {
                if (motions == null) return;

                foreach (var (order, res) in motions)
                {
                    var timer = new StreamingTimer(res.vmddata.RotationStreams.Streams.GetLastKeyTime());

                    createBodyMotion_(order, res, timer);

                    createFaceMotion_(order, res, timer);

                    overwritePosition_(order);
                }

                return;


                void createBodyMotion_(MotionOrder order, MotionResource res, StreamingTimer timer)
                {
                    var pkf = res.vmddata.PositionStreams
                        .ToKeyFinderWith<Key4CatmulPos, Clamp>();

                    var rkf = res.vmddata.RotationStreams
                        .ToKeyFinderWith<Key4CatmulRot, Clamp>();

                    var anim = order.Model.GetComponent<Animator>();
                    var job = anim.create(res.bone, pkf, rkf, timer, order.FootIkMode, order.BodyScale);
                    graph.CreateVmdAnimationJobWithSyncScript(anim, job, timer, order.DelayTime);
                }

                void createFaceMotion_(MotionOrder order, MotionResource res, StreamingTimer timer)
                {
                    if (res.face.Expressions == default) return;
                    if (order.FaceRenderer.AsUnityNull() == default) return;

                    var fkf = res.vmddata.FaceStreams
                        //.ToKeyFinderWith<Key2NearestShift, Clamp>();
                        .ToKeyFinderWith<Key4Catmul, Clamp>();

                    graph.CreateVmdFaceAnimation(order.Model, fkf, res.face, timer, order.DelayTime);
                }

                void overwritePosition_(MotionOrder motion)
                {
                    if (!motion.OverWritePositionAndRotation) return;

                    var tf = motion.Model.transform;
                    tf.position = motion.Position;
                    tf.rotation = motion.Rotation;
                }
            }


        }

    }
}