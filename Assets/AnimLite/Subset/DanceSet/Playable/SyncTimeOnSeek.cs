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

            var src = playable.GetInput(0);
            this.preFrameTime = src.GetTime();
            //playable.SetDuration(src.GetDuration());
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            var src = playable.GetInput(0);

            var currentTime = src.GetTime();//this.preFrameTime + info.deltaTime;//playable.GetTime();
            var endTime = src.GetDuration();

            var isOverZero = this.preFrameTime <= 0.0 && 0.0 < currentTime;
            var isBackFromEnd = currentTime < endTime && endTime <= this.preFrameTime;
            var isEvaluted = info.evaluationType == FrameData.EvaluationType.Evaluate;

            //Debug.Log($"{preFrameTime} : {currentTime} / {endTime} - {info.evaluationType}");
            if (isEvaluted || isOverZero || isBackFromEnd)
            {
                src.Pause();
                src.SetTime(currentTime);// ������Ɖ����o��i�����Ƃ����Ƃ��������m�肽���j
                src.Play();
                //src.Play();// ���ꂾ�ƃ_��
                //playable.SetDuration(endTime);
                //Debug.Log("set time");
                playable.SetInputWeight(0, 1.0f);
            }

            this.preFrameTime = currentTime;
        }
    }
}