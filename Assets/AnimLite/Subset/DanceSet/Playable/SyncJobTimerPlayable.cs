using System;
using UnityEngine.Playables;

namespace AnimLite.DancePlayable
{
    public class SyncJobTimerPlayable : PlayableBehaviour
    {

        public static ScriptPlayable<SyncJobTimerPlayable> Create(PlayableGraph graph, Action<float> updateTimerAction)
        {
            var playable = ScriptPlayable<SyncJobTimerPlayable>.Create(graph);

            playable.GetBehaviour().UpdateTimer = updateTimerAction;
            
            return playable;
        }


        Action<float> UpdateTimer;

        //public override void OnGraphStart(Playable playable)
        //{
        //    var cur = playable;
        //    var src = cur.GetInput(0);
        //    var dst = cur.GetOutput(0);

        //    cur.SetDuration(src.GetDuration());
        //}

        public override void PrepareFrame(Playable playable, FrameData info)// ���ꂾ�Ɨ���Aoutput �̎�ނɂ��H
        //public override void ProcessFrame(Playable playable, FrameData info, object playerData)// ���Ȃ��A�Ȃ�ŁH
        {
            var currentTime = playable.GetInput(0).GetTime();

            this.UpdateTimer((float)currentTime);

            //for (var i = 0; i < playable.GetOutputCount(); i++)
            //{
            //    var dst = playable.GetOutput(i);

            //    dst.SetTime(currentTime);
            //}
        }
    }
}
