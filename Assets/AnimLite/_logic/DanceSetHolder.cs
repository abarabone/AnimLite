using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Animations;
using Unity.VisualScripting;
using UnityEngine.Playables;
using System;

namespace AnimLite.DancePlayable
{
    using AnimLite.Utility;
    using AnimLite.vrm;
    using AnimLite.Vrm;

    public class DanceSetHolder : MonoBehaviour
    {
        public Transform LookAtTarget;

        [SerializeField]
        public DanceSet dance;


        DanceGraphy graphy;

        public PlayableGraph Graph => this.graphy.graph;


        public SemaphoreSlim DanceSemapho { get; } = new SemaphoreSlim(1, 1);

        CancellationTokenSource cts;


        private void OnDestroy()
        {
            this.DanceSemapho.Dispose();
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
                    moveChildrenMotionsToDanceSet_();
                    getFaceRendererIfNothing_();
                    adjustModel_();

                    this.graphy = await this.dance.CreateDanceGraphyAsync(ct);
                    this.graphy?.graph.Play();
                }
                "load end".ShowDebugLog();
            }
            catch (OperationCanceledException e)
            {
                e.Message.ShowDebugLog();
            }
            finally
            {
                this.cts.Dispose();
                this.cts = null;
                "canceller disposed".ShowDebugLog();
            }

            return;


            void getFaceRendererIfNothing_()
            {
                this.dance.Motions
                    .Where(motion => motion.FaceRenderer.IsUnityNull())
                    .ForEach(motion => motion.FaceRenderer = motion.ModelAnimator.FindFaceRenderer());
            }

            void adjustModel_()
            {
                this.dance.Motions
                    .ForEach(x =>
                    {
                        x.ModelAnimator.GetComponent<UniVRM10.Vrm10Instance>().AdjustLootAt(Camera.main.transform);
                        x.FaceRenderer.AdjustBbox(x.ModelAnimator);
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
                this.graphy?.Dispose();
                this.graphy = null;

                this.dance.Motions
                    .Where(motion => !motion.ModelAnimator.IsUnityNull())
                    .ForEach(motion =>
                    {
                        motion.ModelAnimator.UnbindAllStreamHandles();
                        motion.ModelAnimator.ResetPose();
                    });
            }
            "disable end".ShowDebugLog();
        }

    }
}
