using System;
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


    public class DanceGraphy : IDisposable
    {

        public PlayableGraph graph { get; private set; }

        MotionResouce[] resources;


        class MotionResouce : IDisposable
        {

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

            this.resources.ForEach(x => x.Dispose());
        }




        public static async Task<DanceGraphy> CreateDanceGraphyAsync(DanceSet dance, CancellationToken ct)
        {
            var graph = PlayableGraph.Create();
            using var registed = ct.Register(() => graph.Destroy());
            
            createAudioPlayable_(graph, dance.Audio);

            var resources = await buildMotionResourcesAsync_(dance.Motions, ct);

            createMotionPlayables_(graph, dance.Motions, resources);

            return new DanceGraphy
            {
                graph = graph,

                resources = resources,
            };



            static void createAudioPlayable_(PlayableGraph graph, AudioDefine audio)
            {
                if (audio.AudioSource.IsUnityNull()) return;

                graph.CreateAudio(audio.AudioSource, audio.AudioClip, audio.DelayTime);
            }



            static async Task<MotionResouce[]> buildMotionResourcesAsync_(DanceMotionDefine[] motions, CancellationToken ct)
            {

                var resources = await motions
                    .Select(motion => buildAsync_(motion))
                    .WhenAll();

                return resources;


                async Task<MotionResouce> buildAsync_(DanceMotionDefine motion)
                {
                    var (vmddata, facemap) = await VmdData.BuildVmdStreamDataAsync(motion.VmdFilePath, motion.FaceMappingFilePath, ct);

                    return new MotionResouce
                    {
                        vmddata = vmddata,

                        bone = motion.ModelAnimator.BuildVmdPlayableJobTransformMappings(),
                        face = motion.FaceRenderer?.sharedMesh?.BuildStreamingFace(facemap) ?? default,
                    };
                }
            }



            static void createMotionPlayables_(PlayableGraph graph, DanceMotionDefine[] motions, MotionResouce[] resources)
            {

                foreach (var (motion, res) in (motions, resources).Zip())
                {
                    var timer = new StreamingTimer(res.vmddata.RotationStreams.Streams.GetLastKeyTime());

                    createBodyMotion_(motion, res, timer);

                    createFaceMotion_(motion, res,timer);

                    overwritePosition(motion);
                }

                return;


                void createBodyMotion_(DanceMotionDefine motion, MotionResouce res, StreamingTimer timer)
                {
                    var pkf = res.vmddata.PositionStreams
                        .ToKeyFinderWith<Key4CatmulPos, Clamp>();

                    var rkf = res.vmddata.RotationStreams
                        .ToKeyFinderWith<Key4CatmulRot, Clamp>();

                    var job = motion.ModelAnimator.create(res.bone, pkf, rkf, timer, motion.FootIkMode, motion.BodyScale);

                    graph.CreateVmdAnimationJobWithSyncScript(motion.ModelAnimator, job, motion.DelayTime);
                }

                void createFaceMotion_(DanceMotionDefine motion, MotionResouce res, StreamingTimer timer)
                {
                    if (motion.FaceRenderer.AsUnityNull() == null) return;

                    var fkf = res.vmddata.FaceStreams
                        .ToKeyFinderWith<Key2NearestShift, Clamp>();

                    graph.CreateVmdFaceAnimation(motion.ModelAnimator, fkf, res.face, timer, motion.DelayTime);
                }

                void overwritePosition(DanceMotionDefine motion)
                {
                    if (!motion.OverWritePositionAndRotation) return;

                    var tf = motion.ModelAnimator.transform;
                    tf.position = motion.Position;
                    tf.rotation = motion.Rotation;
                }
            }
        }

    }
}