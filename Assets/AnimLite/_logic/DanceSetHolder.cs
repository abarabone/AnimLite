using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using Unity.VisualScripting;

namespace AnimLite.DancePlayable
{
    using AnimLite.Utility;

    public class DanceSetHolder : MonoBehaviour
    {

        [SerializeField]
        public DanceSet dance;

        public ProgressState LoadingState { get; private set; } = new ProgressState();


        DanceGraphy graphy;

        private async Awaitable OnEnable()
        {
            using (this.LoadingState.Start())
            {
                moveChildrenMotionsToDanceSet_();

                getFaceRendererIfNothing_();

                this.graphy = await this.dance.CreateDanceGraphyAsync(this.destroyCancellationToken);

                this.graphy.graph.Play();
            }
            return;


            void getFaceRendererIfNothing_()
            {
                this.dance.Motions
                    .Where(motion => motion.FaceRenderer.IsUnityNull())
                    .ForEach(motion => motion.FaceRenderer = motion.ModelAnimator.FindFaceRenderer());
            }

            void moveChildrenMotionsToDanceSet_()
            {
                var q =
                    from x in this.GetComponentsInChildren<DanceHumanDefine>()
                    let tf = x.transform
                    let pos = tf.position
                    let rot = tf.rotation
                    select motion_(x.Motion, pos, rot)// with ‚ª‚Â‚©‚ê‚Î‚»‚ê‚Å
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
            await this.LoadingState.WaitForCompleteAsync();

            Debug.Log("disable start");
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

    }
}
