using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Unity.VisualScripting;
using System.Linq;

namespace AnimLite.Utility
{
    using AnimLite.Utility.Linq;

    public static class StreamUtility
    {

        // ResetToStancePose() の後に hips だけ local をセットする、でも大丈夫だったが、常に大丈夫か信じきれないので全部やる 

        public static void ResetPose(this Animator anim)
        {
            var avatar = anim.avatar;
            var desc = avatar.humanDescription;

            var skeltondict = desc.skeleton
                //.Do(x => Debug.Log(x.name))
                .ToDictionary(x => x.name, x => x);

            var q = Enumerable.Range(0, (int)HumanBodyBones.LastBone)
                .Select(i => anim.GetBoneTransform((HumanBodyBones)i))
                .Append(anim.GetBoneTransform(HumanBodyBones.Hips).parent)
                .Where(tf => !tf.IsUnityNull());
            foreach (var tf in q)
            {
                //Debug.Log($"tf {tf.name}");//
                var isExists = skeltondict.TryGetValue(tf.name, out var skelton);
                if (!isExists) continue;

                //skelton.name.ShowDebugLog();//
                tf.SetLocalPositionAndRotation(skelton.position, skelton.rotation);// 再計算とか走るんだろうか…
            }
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
