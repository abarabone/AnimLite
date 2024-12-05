using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using Unity.VisualScripting;
using UnityEngine.Playables;
using System;

namespace AnimLite.DancePlayable
{
    using AnimLite.Utility;
    using AnimLite.Vrm;
    using AnimLite.Vmd;


    public class DanceSetHolder : MonoBehaviour
    {



        public Transform LookAtTarget;

        public VmdStreamDataCache Cache;

        [SerializeField]
        public DanceSet dance;


        public DanceGraphy Graphy { get; private set; }

        //public PlayableGraph Graph => this.graphy.graph;


        public SemaphoreSlim DanceSemapho { get; } = new SemaphoreSlim(1, 1);

        CancellationTokenSource cts;


        private void OnDestroy()
        {
            this.DanceSemapho.Dispose();
        }

        void changeVisivility(bool isVisible)
        {
            this.dance.Motions
                .ForEach(x => x.Model.SetActive(isVisible));
        }

        private async Awaitable OnEnable()
        {
            this.cts = CancellationTokenSource.CreateLinkedTokenSource(this.destroyCancellationToken);
            var ct = this.cts.Token;

            try
            {
                "load start".ShowDebugLog();
                using (await this.DanceSemapho.WaitAsyncDisposable(default))
                {
                    changeVisivility(false);

                    moveChildrenMotionsToDanceSet_();
                    getFaceRendererIfNothing_();
                    adjustModel_();

                    var (motions, audioOrder) = await Task.Run(async () =>
                    {
                        var motionResources = await this.dance.BuildMotionResourcesAsync(this.Cache, ct);
                        var motionOrders = buildMotionOrder_();
                        var audioOrder = buildAudioOrder_();
                        var motions = Enumerable.Zip(motionOrders, motionResources, (x, y) => (x, y));

                        return (motions, audioOrder);
                    });

                    this.Graphy = DanceGraphy.CreateGraphy(motions, audioOrder);
                    this.Graphy?.graph.Play();

                    changeVisivility(true);
                }
                "load end".ShowDebugLog();
            }
            catch (OperationCanceledException e)
            {
                e.Message.ShowDebugLog();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                this.cts.Dispose();
                this.cts = null;
                "canceller disposed".ShowDebugLog();
            }

            return;


            DanceGraphy.MotionOrder[] buildMotionOrder_()
            {
                var q =
                    from m in this.dance.Motions
                    select new DanceGraphy.MotionOrder
                    {
                        Model = m.Model,
                        FaceRenderer = m.FaceRenderer,
                        
                        DelayTime = m.DelayTime,
                        BodyScale = m.BodyScale,
                        FootIkMode = m.FootIkMode,

                        OverWritePositionAndRotation = m.OverWritePositionAndRotation,
                        Position = m.Position,
                        Rotation = m.Rotation,
                    };
                return q.ToArray();
            }
            DanceGraphy.AudioOrder buildAudioOrder_()
            {
                var audio = this.dance.Audio;
                return new DanceGraphy.AudioOrder
                {
                    AudioSource = audio.AudioSource,
                    AudioClip = audio.AudioClip,
                    DelayTime = audio.DelayTime,
                };
            }

            void getFaceRendererIfNothing_()
            {
                this.dance.Motions
                    .Where(motion => motion.FaceRenderer.IsUnityNull())
                    .ForEach(motion => motion.FaceRenderer = motion.Model.FindFaceRenderer());
            }

            void adjustModel_()
            {
                this.dance.Motions
                    .ForEach(x =>
                    {
                        x.Model.GetComponent<UniVRM10.Vrm10Instance>().AdjustLootAt(Camera.main.transform);
                        x.FaceRenderer.AdjustBbox(x.Model.GetComponent<Animator>());
                    });
            }

            void moveChildrenMotionsToDanceSet_()
            {
                var q =
                    from x in this.GetComponentsInChildren<DanceHumanDefine>()
                    let tf = x.transform
                    let pos = tf.position
                    let rot = tf.rotation
                    select motion_(x.Motion, pos, rot)// with がつかればそれで
                    ;

                this.dance.Motions = this.dance.Motions.Concat(q).ToArray();

                this.transform.DetachChildren();

                return;


                DanceMotionDefine motion_(DanceMotionDefine m, Vector3 pos, Quaternion rot)
                {
                    m.OverWritePositionAndRotation = true;
                    m.Position = pos;
                    m.Rotation = rot;
                    return m;
                }
            }
        }

        async Awaitable OnDisable()
        {
            this.cts?.Cancel();

            "disable start".ShowDebugLog();
            using (await this.DanceSemapho.WaitAsyncDisposable(default))// ゲームオブジェクトが破棄されても、解放はやり切ってほしいので Token は default
            {
                this.Graphy?.Dispose();
                this.Graphy = null;

                this.dance.Motions
                    .Where(motion => !motion.Model.IsUnityNull())
                    .ForEach(motion =>
                    {
                        var anim = motion.Model.GetComponent<Animator>();
                        anim.UnbindAllStreamHandles();
                        anim.ResetPose();
                    });
            }
            "disable end".ShowDebugLog();

            changeVisivility(false);
        }

    }

}
