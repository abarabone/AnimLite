using UnityEngine;
using UnityEngine.Playables;

namespace AnimLite.DancePlayable
{
    using AnimLite;
    using AnimLite.Vrm;

    public class FaceShifterPlayable : PlayableBehaviour
    {

        IKeyFinderWithoutProcedure<float> kf;

        VrmExpressionOperator opface;


        StreamingTimer timer;
        float previousTime;
        float indexBlockTime;


        public static ScriptPlayable<FaceShifterPlayable> Create(
            PlayableGraph graph, Animator anim, IKeyFinderWithoutProcedure<float> kf, VrmExpressionMappings face, StreamingTimer timer)
        {
            var playable = ScriptPlayable<FaceShifterPlayable>.Create(graph);

            playable.GetBehaviour().Initialize(anim, kf, face, timer);

            return playable;
        }


        public void Initialize(Animator anim, IKeyFinderWithoutProcedure<float> kf, VrmExpressionMappings face, StreamingTimer timer)
        {
            this.kf = kf;
            this.timer = timer;
            this.indexBlockTime = kf.IndexBlockTimeRange;
            this.opface = anim.ToVrmExpressionOperator(face);
        }

        public override void OnGraphStart(Playable playable)
        {
            this.previousTime = (float)playable.GetTime();
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (this.opface.vrmexp == null) return;


            var currentTime = (float)playable.GetTime();
            var previousTime = this.timer.CurrentTime;

            this.timer.UpdateTime(currentTime);


            if (currentTime >= previousTime - this.indexBlockTime)
            {
                var kf = this.kf.With<float, IKeyFinderWithoutProcedure<float>, Forward>(this.timer);

                this.opface.SetFaceExpressions(kf);
            }
            else
            {
                //"absolute face".ShowDebugLog();
                var kf = this.kf.With<float, IKeyFinderWithoutProcedure<float>, Absolute>(this.timer);

                this.opface.SetFaceExpressions(kf);
            }

            this.previousTime = previousTime;
        }

    }

}