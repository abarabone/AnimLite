using System;
using UnityEngine.Playables;
using UnityEngine;
using Unity.Jobs;

namespace AnimLite.DancePlayable
{
    public class SyncJobTimerPlayable : PlayableBehaviour
    {

        public static ScriptPlayable<SyncJobTimerPlayable> Create(PlayableGraph graph, Func<float, JobHandle, JobHandle> updateTimerAction)
        {
            var playable = ScriptPlayable<SyncJobTimerPlayable>.Create(graph);

            playable.GetBehaviour().updateTimer = updateTimerAction;
            
            return playable;
        }


        Func<float, JobHandle, JobHandle> updateTimer;

        JobHandle prevJob;

        //public override void OnGraphStart(Playable playable)
        //{
        //    var cur = playable;
        //    var src = cur.GetInput(0);
        //    var dst = cur.GetOutput(0);

        //    cur.SetDuration(src.GetDuration());
        //}

        public override void PrepareFrame(Playable playable, FrameData info)// これだと来る、output の種類による？
        //public override void ProcessFrame(Playable playable, FrameData info, object playerData)// 来ない、なんで？
        {

            //var currentTime = playable.GetInput(0).GetTime();
            var currentTime = playable.GetTime();

            this.prevJob = this.updateTimer((float)currentTime, this.prevJob);

            //for (var i = 0; i < playable.GetOutputCount(); i++)
            //{
            //    var dst = playable.GetOutput(i);

            //    dst.SetTime(currentTime);
            //}
        }
    }
}
