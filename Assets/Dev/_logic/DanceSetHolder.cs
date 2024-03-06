using System.Linq;
using UnityEngine;

namespace AnimLite.DancePlayable
{
    public class DanceSetHolder : MonoBehaviour
    {

        [SerializeField]
        public DanceSet dance;


        DanceGraphy graphy;

        private async Awaitable OnEnable()
        {

            addChildrenMotionsToDanceSet_();

            this.graphy = await this.dance.CreateDanceGraphyAsync(this.destroyCancellationToken);

            this.graphy.graph.Play();

            return;


            void addChildrenMotionsToDanceSet_()
            {
                var q =
                    from x in this.GetComponentsInChildren<DanceHumanDefine>()
                    let tf = x.transform
                    let pos = tf.position
                    let rot = tf.rotation
                    select motion_(x.Motion, pos, rot)// with ‚ª‚Â‚©‚ê‚Î‚»‚ê‚Å
                    ;

                this.dance.Motions = this.dance.Motions.Concat(q).ToArray();
            }
            DanceMotionDefine motion_(DanceMotionDefine m, Vector3 pos, Quaternion rot)
            {
                m.OverWritePositionAndRotation = true;
                m.Position = pos;
                m.Rotation = rot;
                return m;
            }
        }

        void OnDisable()
        {
            if (this.graphy == null) return;
            //if (this.graphy.graph.IsValid()) return;

            this.graphy.Dispose();
        }

    }
}
