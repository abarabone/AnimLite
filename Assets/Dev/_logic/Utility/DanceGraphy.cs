using AnimLite.Vmd;
using System;
using System.Linq;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;


namespace AnimLite.DancePlayable
{
    using AnimLite.Utility.Linq;


    [System.Serializable]
    public struct DanceSet
    {
        public AudioDefine Audio;

        public DanceMotionDefine[] Motions;
    }

    [System.Serializable]
    public struct AudioDefine
    {
        public AudioSource AudioSource;
        public AudioClip AudioClip;

        public float DelayTime;
    }

    [System.Serializable]
    public struct DanceMotionDefine
    {
        [FilePath]
        public string VmdFilePath;
        [FilePath]
        public string FaceMappingFilePath;

        public Animator ModelAnimator;
        public SkinnedMeshRenderer FaceRenderer;

        public float DelayTime;

        [HideInInspector] public bool OverWritePositionAndRotation;
        [HideInInspector] public Vector3 Position;
        [HideInInspector] public Quaternion Rotation;
    }


    public static class DanceGraphyExtension
    {

        public static async Awaitable<DanceGraphy> CreateDanceGraphyAsync(this DanceSet dance, CancellationToken ct)
        {
            return await DanceGraphy.CreateDanceGraphyAsync(dance, ct);
        }

    }


    public class DanceGraphy : IDisposable
    {

        public PlayableGraph graph { get; private set; }

        MotionResouce[] resources;


        class MotionResouce : IDisposable
        {

            public VmdStreamData vmddata;

            public JobPlayableStreamingBone bone;
            public StreamingFace face;


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




        public static async Awaitable<DanceGraphy> CreateDanceGraphyAsync(DanceSet dance, CancellationToken ct)
        {
            var graph = PlayableGraph.Create();


            createAudioPlayable(graph, dance.Audio);


            var resources = await dance.Motions
                .Select(async motion => await buildMotionResourcesAsync(motion, ct))
                .WhenAll();

            foreach (var (motion, res) in (dance.Motions, resources).Zip())
            {
                createMotionPlayables(graph, motion, res);

                overwritePosition(motion);
            }


            return new DanceGraphy
            {
                graph = graph,

                resources = resources,
            };


            void overwritePosition(DanceMotionDefine motion)
            {
                if (!motion.OverWritePositionAndRotation) return;

                var tf = motion.ModelAnimator.transform;
                tf.position = motion.Position;
                tf.rotation = motion.Rotation;
            }


            // Task.Run() ‰»‚µ‚½‚¢‚©‚à
            async Awaitable<MotionResouce> buildMotionResourcesAsync(DanceMotionDefine motion, CancellationToken ct)
            {
                var vmdStreamData = await VmdParser.ParseVmdAsync(motion.VmdFilePath, ct);
                var faceMapping = await VmdParser.ParseFaceMapAsync(motion.FaceMappingFilePath, ct);


                var rot_data = vmdStreamData.bodyKeyStreams.CreateRotationData();
                var pos_data = vmdStreamData.bodyKeyStreams.CreatePositionData();
                var face_data = vmdStreamData.faceKeyStreams.CreateFaceData(faceMapping);

                var rot_index = rot_data.CreateIndex(indexBlockLength: 100);
                var pos_index = pos_data.CreateIndex(indexBlockLength: 100);
                var face_index = face_data.CreateIndex(indexBlockLength: 100);

                var timer = new StreamingTimer(rot_data.GetLastKeyTime());

                var rot_cache = rot_data.ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulRot>(timer);
                var pos_cache = pos_data.ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulPos>(timer);
                var face_cache = face_data.ToKey2CacheFactory().CreateCacheWithInitialize<Clamp, Key2NearestShift>(timer);

                var vmddata = new VmdStreamData
                {
                    RotationStreams = rot_data.ToHolderWith(rot_cache, rot_index),
                    PositionStreams = pos_data.ToHolderWith(pos_cache, pos_index),
                    FaceStreams = face_data.ToHolderWith(face_cache, face_index),
                };

                return new MotionResouce
                {
                    vmddata = vmddata,

                    bone = motion.ModelAnimator.BuildVmdJobStreamingBone(),
                    face = motion.FaceRenderer?.sharedMesh.BuildStreamingFace(faceMapping) ?? default,
                };
            }

            void createMotionPlayables(PlayableGraph graph, DanceMotionDefine motion, MotionResouce res)
            {
                var timer = new StreamingTimer(res.vmddata.RotationStreams.Streams.GetLastKeyTime());

                {
                    var pkf = res.vmddata.PositionStreams
                        .ToKeyFinderWith<Key4CatmulPos, Clamp>();

                    var rkf = res.vmddata.RotationStreams
                        .ToKeyFinderWith<Key4CatmulRot, Clamp>();

                    var job = motion.ModelAnimator.create(res.bone, pkf, rkf, timer);

                    graph.CreateVmdAnimationJobWithSyncScript(motion.ModelAnimator, job, motion.DelayTime);
                }

                if (motion.FaceRenderer.AsUnityNull() != null)
                {
                    var fkf = res.vmddata.FaceStreams
                        .ToKeyFinderWith<Key2NearestShift, Clamp>();

                    graph.CreateVmdFaceAnimation(motion.ModelAnimator, fkf, res.face, timer, motion.DelayTime);
                }
            }

            void createAudioPlayable(PlayableGraph graph, AudioDefine audio)
            {
                graph.CreateAudio(audio.AudioSource, audio.AudioClip, audio.DelayTime);
            }
        }

    }
}