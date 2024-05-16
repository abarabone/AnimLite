using System;
using System.IO;
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

        MotionResource[] resources;


        public class MotionResource : IDisposable
        {

            public VmdStreamData vmddata;

            public TransformHandleMappings bone;
            public VrmExpressionMappings face;


            public Action<MotionResource> DisposeAction;

            public void Dispose() => this.DisposeAction(this);
        }


        public void Dispose()
        {
            this.graph.Stop();
            this.graph.Destroy();

            this.resources.ForEach(x => x.Dispose());

            "resources disposed".ShowDebugLog();
        }




        public static async Task<DanceGraphy> CreateDanceGraphyAsync(
            DanceSet dance, VmdStreamDataCache cache, CancellationToken ct)
        {

            var motions = dance.Motions
                //.Where(motion => motion.FaceMappingFilePath != null && File.Exists(motion.FaceMappingFilePath))
                //.Where(motion => motion.VmdFilePath != null && File.Exists(motion.VmdFilePath))
                //.ToArray()
                ;

            var resources = cache.IsUnityNull()
                ? await buildMotionResourcesAsync_(motions, ct)
                : await cache.GetOrBuildMotionResourcesAsync(motions, ct);


            await Awaitable.MainThreadAsync();

            var graph = PlayableGraph.Create();
            using var registed = registCancel_(resources, graph, ct);

            createMotionPlayables_(graph, motions, resources);

            createAudioPlayable_(graph, dance.Audio);

            return new DanceGraphy
            {
                graph = graph,

                resources = resources,
            };


            static CancellationTokenRegistration registCancel_(
                MotionResource[] resources, PlayableGraph graph, CancellationToken ct)
            =>
                ct.Register(() =>
                {
                    resources.ForEach(x => x.Dispose());
                    graph.Destroy();

                    "create canceled".ShowDebugLog();
                });


            static void createAudioPlayable_(PlayableGraph graph, AudioDefine audio)
            {
                if (audio.AudioSource.IsUnityNull()) return;

                graph.CreateAudio(audio.AudioSource, audio.AudioClip, audio.DelayTime);
            }


            static async Task<MotionResource[]> buildMotionResourcesAsync_(DanceMotionDefine[] motions, CancellationToken ct)
            {

                var defaultFaceMap = motions.Any(x => x.FaceMappingFilePath.IsBlank())
                    ? await "".ToPath().LoadFaceMapExAsync(ct)
                    : default;

                var resources = await motions
                    .Select(motion => buildAsync_(motion))
                    .WhenAll();

                return resources;


                async Task<MotionResource> buildAsync_(DanceMotionDefine motion)
                {
                    var vmdfullpath = motion.VmdFilePath.ToFullPath();
                    var facefullpath = motion.FaceMappingFilePath.ToFullPath();
                    var (vmddata, facemap) = defaultFaceMap.VmdToVrmMaps == default
                        ? (await VmdData.LoadVmdStreamDataAsync(vmdfullpath, facefullpath, ct))
                        : (await VmdData.LoadVmdStreamDataAsync(vmdfullpath, defaultFaceMap, ct), defaultFaceMap);

                    return new MotionResource
                    {
                        vmddata = vmddata,

                        bone = motion.ModelAnimator.BuildVmdPlayableJobTransformMappings(),
                        face = motion.FaceRenderer?.sharedMesh?.BuildStreamingFace(facemap) ?? default,

                        DisposeAction = (MotionResource mr) =>
                        {
                            mr.vmddata.Dispose();
                            mr.bone.Dispose();
                            //mr.face.Dispose();
                        },
                    };
                }
            }



            static void createMotionPlayables_(
                PlayableGraph graph, DanceMotionDefine[] motions, MotionResource[] resources)
            {

                foreach (var (motion, res) in (motions, resources).Zip())
                {
                    var timer = new StreamingTimer(res.vmddata.RotationStreams.Streams.GetLastKeyTime());

                    createBodyMotion_(motion, res, timer);

                    createFaceMotion_(motion, res, timer);

                    overwritePosition_(motion);
                }

                return;


                void createBodyMotion_(DanceMotionDefine motion, MotionResource res, StreamingTimer timer)
                {
                    var pkf = res.vmddata.PositionStreams
                        .ToKeyFinderWith<Key4CatmulPos, Clamp>();

                    var rkf = res.vmddata.RotationStreams
                        .ToKeyFinderWith<Key4CatmulRot, Clamp>();

                    var job = motion.ModelAnimator.create(res.bone, pkf, rkf, timer, motion.FootIkMode, motion.BodyScale);

                    graph.CreateVmdAnimationJobWithSyncScript(motion.ModelAnimator, job, motion.DelayTime);
                }

                void createFaceMotion_(DanceMotionDefine motion, MotionResource res, StreamingTimer timer)
                {
                    if (res.face.Expressions == default) return;
                    if (motion.FaceRenderer.AsUnityNull() == default) return;

                    var fkf = res.vmddata.FaceStreams
                        .ToKeyFinderWith<Key2NearestShift, Clamp>();

                    graph.CreateVmdFaceAnimation(motion.ModelAnimator, fkf, res.face, timer, motion.DelayTime);
                }

                void overwritePosition_(DanceMotionDefine motion)
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