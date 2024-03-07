using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
//using static Unity.VisualScripting.AnnotationUtility;

namespace AnimLite.Utility
{


    public static class StreamUtility
    {

        public static void ResetPose(this Animator anim)//, PlayableGraph graph = default)
        {
            var graph = PlayableGraph.Create();

            create_playable_();

            graph.Evaluate(0);

            graph.Destroy();

            return;


            void create_playable_()
            {
                var output_anim = AnimationPlayableOutput.Create(graph, "", anim);

                var playable_job = AnimationScriptPlayable.Create(graph, new ResetPoseJob());
                playable_job.SetInputCount(1);
                playable_job.SetOutputCount(1);
                playable_job.SetInputWeight(0, 1);

                output_anim.SetSourcePlayable(playable_job);
            }
        }

        struct ResetPoseJob : IAnimationJob
        {
            public void ProcessRootMotion(AnimationStream stream)
            {
                //stream.AsHuman().ResetToStancePose();
            }

            public void ProcessAnimation(AnimationStream stream)
            {
                stream.AsHuman().ResetToStancePose();

            }
        }
    }


}
