using UnityEngine.Playables;

namespace AnimLite.DancePlayable
{

    /// <summary>
    /// 下記条件で、再生を促す（サウンドなど、自動的にシークが反映されないものに働きかけて再生を促す）
    /// ・シークしたとき（ graph.Evalute() したとき）
    /// ・マイナスから 0 を超えたとき
    /// ・再生時間を超えた部分から、再生時間内に戻ってきたとき
    /// ていうかもっとちゃんとしたやり方知りたい。playable 意味わかんなすぎ
    /// </summary>
    public class SyncTimeOnSeek : PlayableBehaviour
    {

        public static ScriptPlayable<SyncTimeOnSeek> Create(PlayableGraph graph)
        {
            var playable = ScriptPlayable<SyncTimeOnSeek>.Create(graph);

            playable.SetInputCount(1);

            return playable;
        }


        IPlayable target;

        double preFrameTime;




        public override void OnGraphStart(Playable playable)
        {
            playable.SetInputWeight(0, 0.0f);

            this.preFrameTime = playable.GetInput(0).GetTime();
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            var src = playable.GetInput(0);

            var currentTime = src.GetTime();//this.preFrameTime + info.deltaTime;//playable.GetTime();
            var endTime = src.GetDuration();

            var isOverZero = this.preFrameTime <= 0.0 && 0.0 < currentTime;
            var isBackFromEnd = currentTime < endTime && endTime <= this.preFrameTime;
            var isEvaluted = info.evaluationType == FrameData.EvaluationType.Evaluate;

            if (isEvaluted | isOverZero | isBackFromEnd)
            {
                playable.SetInputWeight(0, 1.0f);
                src.SetTime(currentTime);// これやると音が出る（もっとちゃんとしたやり方知りたい）
                //src.Play();// これだとダメ
            }

            this.preFrameTime = currentTime;
        }
    }
}