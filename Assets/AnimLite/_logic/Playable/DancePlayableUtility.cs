using AnimLite.Vmd;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Audio;
using UnityEngine.Playables;
//using SimpleDance.FacialAnimation;
//using SimpleDance;


namespace AnimLite.DancePlayable
{
    using AnimLite.Vrm;


    public static class DancePlayableUtility
    {

        /// <summary>
        /// �P��̂u�l�c�{�f�B�A�j���[�V�������Đ����� playable �O���t���쐬����B
        /// �Đ��� Job ���g�p����BJob �� playable ���玞���Ȃǂ̏��𒼐ړI�Ɏ擾�ł��Ȃ��̂ŁA
        /// �⏕�Ƃ��Ď��Ԃ��X�V���� playable ���쐬����B
        /// </summary>
        public static void CreateVmdAnimationJobWithSyncScript<TJob>(
            this PlayableGraph graph,
            Animator anim, TJob job, float delay = 0, VmdFootIkMode footIkMode = VmdFootIkMode.auto)
                where TJob : struct, IAnimationJob, IVmdAnimationJob
            // �ϐ��œn���ꂽ job �́A�W�F�l���N�X�̏ꍇ burst �R���p�C���Ɏ��s����Ƃ������Ă������C���������ǁA���v������
        {
            var name_anim = $"{anim.name} Body Animator";
            var name_script = $"{anim.name} Body Script";


            var output_anim = AnimationPlayableOutput.Create(graph, name_anim, anim);


            var playable_job = AnimationScriptPlayable.Create(graph, job);
            playable_job.SetInputCount(1);
            playable_job.SetOutputCount(1);
            playable_job.SetInputWeight(0, 1);

            var playable_sync = SyncJobTimerPlayable.Create(graph, currentTime =>
            {
                job.UpdateTimer(currentTime);
                playable_job.SetJobData(job);
            });
            playable_sync.SetInputCount(1);
            playable_sync.SetOutputCount(1);
            playable_sync.SetInputWeight(0, 1);


            playable_job.SetTime(-delay);

            graph.Connect(playable_job, 0, playable_sync, 0);
            output_anim.SetSourcePlayable(playable_sync);
        }

        /// <summary>
        /// �P��̂u�l�c�t�F�C�X�A�j���[�V�������Đ����� playable �O���t���쐬����B
        /// </summary>
        public static void CreateVmdFaceAnimation(
            this PlayableGraph graph,
            Animator anim, IKeyFinderWithoutProcedure<float> kf, VrmExpressionMappings face, StreamingTimer timer, float delay = 0)
        {
            var name = $"{anim.name} Facial";


            var output = ScriptPlayableOutput.Create(graph, name);


            var playable_face = FaceShifterPlayable.Create(graph, anim, kf, face, timer);
            playable_face.SetInputCount(1);
            playable_face.SetOutputCount(1);
            playable_face.SetInputWeight(0, 1);


            playable_face.SetTime(-delay);

            output.SetSourcePlayable(playable_face);
        }

        /// <summary>
        /// �P��̃A�j���[�V�����N���b�v���Đ����� playable �O���t���쐬����B
        /// </summary>
        public static void CreateClipAnimation(
            this PlayableGraph graph,
            Animator anim, AnimationClip clip, float delay = 0)
        {
            var name = $"{anim.name} Animation";


            var output = AnimationPlayableOutput.Create(graph, name, anim);// Debug.Log(clip.length);


            var playable_anim = AnimationClipPlayable.Create(graph, clip);
            playable_anim.SetInputCount(1);
            playable_anim.SetOutputCount(1);
            playable_anim.SetInputWeight(0, 1);


            playable_anim.SetTime(-delay);

            graph.Connect(playable_anim, 0, playable_anim, 0);
            output.SetSourcePlayable(playable_anim);
        }

        /// <summary>
        /// �I�[�f�B�I�N���b�v���Đ����� playable �O���t���쐬����B
        /// </summary>
        public static void CreateAudio(
            this PlayableGraph graph,
            AudioSource audio, AudioClip clip, float delay = 0)
        {
            var name = $"{audio.name} Audio";
            var name_script = $"{audio.name} Audio Script";


            var output = AudioPlayableOutput.Create(graph, name, audio);


            var playable_audio = AudioClipPlayable.Create(graph, clip, looping: false);
            playable_audio.SetInputCount(1);
            playable_audio.SetOutputCount(1);
            playable_audio.SetInputWeight(0, 1);

            var playable_reseter = SyncTimeOnSeek.Create(graph);
            playable_reseter.SetInputCount(1);
            playable_reseter.SetOutputCount(1);
            playable_reseter.SetInputWeight(0, 1);


            playable_audio.SetTime(-delay);

            graph.Connect(playable_audio, 0, playable_reseter, 0);
            output.SetSourcePlayable(playable_reseter, 0);

            output.SetEvaluateOnSeek(true);// �Ȃ񂾂낤����
        }
    }
}
