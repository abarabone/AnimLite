using UnityEngine.Playables;
using UnityEngine;

namespace AnimLite.DancePlayable
{

    /// <summary>
    /// ���L�����ŁA�Đ��𑣂��i�T�E���h�ȂǁA�����I�ɃV�[�N�����f����Ȃ����̂ɓ��������čĐ��𑣂��j
    /// �E�V�[�N�����Ƃ��i graph.Evalute() �����Ƃ��j
    /// �E�}�C�i�X���� 0 �𒴂����Ƃ�
    /// �E�Đ����Ԃ𒴂�����������A�Đ����ԓ��ɖ߂��Ă����Ƃ�
    /// �Ă����������Ƃ����Ƃ��������m�肽���Bplayable �Ӗ��킩��Ȃ���
    /// </summary>
    public class SyncTimeOnSeek : PlayableBehaviour
    {

        public static ScriptPlayable<SyncTimeOnSeek> Create(PlayableGraph graph)//, AudioSource asrc)
        {
            var playable = ScriptPlayable<SyncTimeOnSeek>.Create(graph);

            //playable.SetInputCount(1);
            //playable.GetBehaviour().asrc = asrc;

            return playable;
        }


        IPlayable target;

        double preFrameTime;

        //public AudioSource asrc;


        public override void OnGraphStart(Playable playable)
        {
            ////playable.SetInputWeight(0, 0.0f);
            ////playable.SetInputWeight(0, 1.0f);

            //var src = playable.GetInput(0);
            //this.preFrameTime = src.GetTime();
            ////playable.SetDuration(src.GetDuration());

            //playable.SetInputWeight(0, 0.0f);
            this.preFrameTime = playable.GetTime();
        }

        //public override void PrepareFrame(Playable playable, FrameData info)
        //{
        //    var src = playable.GetInput(0);

        //    var currentTime = src.GetTime();//this.preFrameTime + info.deltaTime;//playable.GetTime();
        //    var endTime = src.GetDuration();

        //    var isOverZero = this.preFrameTime <= 0.0 && 0.0 < currentTime;
        //    var isBackFromEnd = currentTime < endTime && endTime <= this.preFrameTime;
        //    var isEvaluted = info.evaluationType == FrameData.EvaluationType.Evaluate;

        //    //Debug.Log($"{preFrameTime} : {currentTime} / {endTime} ... {info.evaluationType}");
        //    if (isEvaluted || isOverZero || isBackFromEnd)
        //    {
        //        src.SetTime(playable.GetTime());// ������Ɖ����o��i�����Ƃ����Ƃ��������m�肽���j
        //        //Debug.Log($"set time : {src.GetTime()}");
        //    }

        //    this.preFrameTime = currentTime;
        //}
        public override void PrepareFrame(Playable playable, FrameData info)
        {
            //var currentTime = this.preFrameTime + info.deltaTime;
            var currentTime = playable.GetTime();
            var endTime = playable.GetDuration();

            var isOverZero = this.preFrameTime <= 0.0 && 0.0 < currentTime;
            var isBackFromEnd = currentTime < endTime && endTime <= this.preFrameTime;
            var isEvaluted = info.evaluationType == FrameData.EvaluationType.Evaluate;

            //Debug.Log($"{preFrameTime} : {currentTime} / {endTime} ... {info.evaluationType}");
            if (isEvaluted || isOverZero || isBackFromEnd)
            {
                var src = playable.GetInput(0);
                src.SetTime(currentTime);// ������Ɖ����o��i�����Ƃ����Ƃ��������m�肽���j
                //Debug.Log($"set time : {currentTime}");
                playable.SetInputWeight(0, 1.0f);
                //this.asrc.time = (float)currentTime * 0.5f;
            }

            this.preFrameTime = currentTime;
        }
    }
}