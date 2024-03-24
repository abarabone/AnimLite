using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
//using static Unity.VisualScripting.AnnotationUtility;
using System.Linq;

namespace AnimLite.Utility
{
    using AnimLite.Utility.Linq;

    public static class StreamUtility
    {

        // ResetToStancePose() �̌�� hips ���� local ���Z�b�g����A�ł����v���������A��ɑ��v���M������Ȃ��̂őS����� 

        public static void ResetPose(this Animator anim)
        {
            var bonedict = anim.GetComponentsInChildren<SkinnedMeshRenderer>()
                .SelectMany(x => x.bones)
                .Distinct(x => x.name)
                .Do(x => Debug.Log(x.name))
                .ToDictionary(x => x.name, x => x);

            var avatar = anim.avatar;
            var desc = avatar.humanDescription;
            foreach (var s in desc.skeleton)
            {
                var tf = bonedict.TryGetOrDefault(s.name);
                if (tf == null) continue;

                tf.SetLocalPositionAndRotation(s.position, s.rotation);// �Čv�Z�Ƃ�����񂾂낤���c
            }

            anim.GetBoneTransform(HumanBodyBones.Hips).parent
                .SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);// ���b��A���[�g�̓[���Ɖ���c
        }

        //public static void ResetPose(this Animator anim)
        //{
        //    var s = new AnimationStream();
        //    var o = anim.OpenAnimationStream(ref s);
        //    s.AsHuman().ResetToStancePose();
        //    anim.CloseAnimationStream(ref s);
        //}


        //public static void ResetPose(this Animator anim)//, PlayableGraph graph = default)
        //{
        //    var graph = PlayableGraph.Create();

        //    create_playable_();

        //    graph.Evaluate(0);

        //    graph.Destroy();

        //    return;


        //    void create_playable_()
        //    {
        //        var output_anim = AnimationPlayableOutput.Create(graph, "", anim);

        //        var playable_job = AnimationScriptPlayable.Create(graph, new ResetPoseJob());
        //        playable_job.SetInputCount(0);
        //        playable_job.SetOutputCount(1);

        //        output_anim.SetSourcePlayable(playable_job);
        //    }
        //}

        //struct ResetPoseJob : IAnimationJob
        //{
        //    public void ProcessRootMotion(AnimationStream stream)
        //    {
        //        //stream.AsHuman().ResetToStancePose();
        //    }

        //    public void ProcessAnimation(AnimationStream stream)
        //    {
        //        stream.AsHuman().ResetToStancePose();
        //    }
        //}
    }


}
