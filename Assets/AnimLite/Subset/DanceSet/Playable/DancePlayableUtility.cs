using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Audio;
using UnityEngine.Playables;
using Unity.VisualScripting;
//using SimpleDance.FacialAnimation;
//using SimpleDance;
using System.Collections.Generic;
using System.Linq;


namespace AnimLite.DancePlayable
{
    using AnimLite.Vrm;
    using AnimLite.Vmd;


    public static class DancePlayableUtility
    {

        /// <summary>
        /// 単一のＶＭＤボディアニメーションを再生する playable グラフを作成する。
        /// 再生は Job を使用する。Job は playable から時刻などの情報を直接的に取得できないので、
        /// 補助として時間を更新する playable も作成する。
        /// </summary>
        public static void CreateVmdAnimationJobWithSyncScript<TJob>(
            this PlayableGraph graph,
            Animator anim, TJob job, StreamingTimer timer, float delay = 0, VmdFootIkMode footIkMode = VmdFootIkMode.auto)
                where TJob : struct, IAnimationJob, IVmdAnimationJob
            // 変数で渡された job は、ジェネリクスの場合 burst コンパイルに失敗するとか書いてあった気がしたけど、大丈夫だった
        {
            if (anim.IsUnityNull()) return;
            var name_anim = $"{anim.name} Body Animator";
            var name_script = $"{anim.name} Body Script";


            var output_anim = AnimationPlayableOutput.Create(graph, name_anim, anim);


            var playable_job = AnimationScriptPlayable.Create(graph, job);
            //playable_job.SetInputCount(1);
            playable_job.SetOutputCount(1);
            //playable_job.SetInputWeight(0, 1);

            var playable_sync = SyncJobTimerPlayable.Create(graph, currentTime =>
            {
                job.UpdateTimer(currentTime);
                playable_job.SetJobData(job);
            });
            playable_sync.SetInputCount(1);
            playable_sync.SetOutputCount(1);
            playable_sync.SetInputWeight(0, 1);


            playable_job.SetTime(-delay);
            playable_sync.SetTime(-delay);
            //playable_job.SetDuration(timer.TotalTime + delay);
            //playable_sync.SetDuration(timer.TotalTime + delay);
            playable_job.SetDuration(timer.TotalTime);
            playable_sync.SetDuration(timer.TotalTime);

            graph.Connect(playable_job, 0, playable_sync, 0);
            output_anim.SetSourcePlayable(playable_sync);
        }

        /// <summary>
        /// 単一のＶＭＤフェイスアニメーションを再生する playable グラフを作成する。
        /// </summary>
        public static void CreateVmdFaceAnimation(
            this PlayableGraph graph,
            GameObject model, IKeyFinderWithoutProcedure<float> kf, VrmExpressionMappings face, StreamingTimer timer, float delay = 0)
        {
            if (model.IsUnityNull()) return;
            var name = $"{model.name} Facial";


            var output = ScriptPlayableOutput.Create(graph, name);


            var playable_face = FaceShifterPlayable.Create(graph, model, kf, face, timer);
            //playable_face.SetInputCount(1);
            playable_face.SetOutputCount(1);
            //playable_face.SetInputWeight(0, 1);


            playable_face.SetTime(-delay);
            //playable_face.SetDuration(timer.TotalTime + delay);
            playable_face.SetDuration(timer.TotalTime);

            output.SetSourcePlayable(playable_face);
        }

        /// <summary>
        /// 単一のアニメーションクリップを再生する playable グラフを作成する。
        /// </summary>
        public static void CreateClipAnimation(
            this PlayableGraph graph,
            Animator anim, AnimationClip clip, float delay = 0)
        {
            if (anim.IsUnityNull()) return;
            if (clip.IsUnityNull()) return;
            var name = $"{anim.name} Animation";


            var output = AnimationPlayableOutput.Create(graph, name, anim);// Debug.Log(clip.length);


            var playable_anim = AnimationClipPlayable.Create(graph, clip);
            //playable_anim.SetInputCount(1);
            playable_anim.SetOutputCount(1);
            //playable_anim.SetInputWeight(0, 1);


            playable_anim.SetTime(-delay);
            //playable_anim.SetDuration(clip.length + delay);
            playable_anim.SetDuration(clip.length);

            //graph.Connect(playable_anim, 0, playable_anim, 0);
            output.SetSourcePlayable(playable_anim);
        }

        /// <summary>
        /// オーディオクリップを再生する playable グラフを作成する。
        /// </summary>
        public static void CreateAudio(
            this PlayableGraph graph,
            AudioSource audio, AudioClip clip, float delay = 0)
        {
            if (audio.IsUnityNull()) return;
            if (clip.IsUnityNull()) return;
            var name = $"{audio.name} Audio";
            var name_script = $"{audio.name} Audio Script";


            var output = AudioPlayableOutput.Create(graph, name, audio);
            output.SetEvaluateOnSeek(false);


            var playable_audio = AudioClipPlayable.Create(graph, clip, looping: false);
            //playable_audio.SetInputCount(1);
            playable_audio.SetOutputCount(1);
            //playable_audio.SetInputWeight(0, 1f);

            var playable_reseter = SyncTimeOnSeek.Create(graph);//, audio);
            playable_reseter.SetInputCount(1);
            playable_reseter.SetOutputCount(1);
            playable_reseter.SetInputWeight(0, 1f);


            playable_audio.SetTime(-delay);
            playable_reseter.SetTime(-delay);
            //playable_audio.SetDuration(clip.length + delay);
            //playable_reseter.SetDuration(clip.length + delay);
            playable_audio.SetDuration(clip.length);
            playable_reseter.SetDuration(clip.length);

            graph.Connect(playable_audio, 0, playable_reseter, 0);
            output.SetSourcePlayable(playable_reseter, 0);

            //output.SetEvaluateOnSeek(true);// なんだろうこれ
        }


        public static void AdjustPlayableLength(this PlayableGraph graph)
        {
            
            var q =
                from ir in Enumerable.Range(0, graph.GetRootPlayableCount())
                let rp = graph.GetRootPlayable(ir)
                //from p in next_(rp)
                //select p
                select rp
                ;
            if (q.IsEmpty()) return;

            //var maxlength = q.Max(p => p.GetDuration() + -p.GetTime());
            
            q.ForEach(p =>
            {
                //Debug.Log(p.GetDuration());
                //var offset = -p.GetTime();
                //var duration = p.GetDuration();
                //var total = duration + offset;
                //var distance = maxlength - total;
                //p.SetDuration(duration - distance);
                p.SetDuration(double.PositiveInfinity);
            });


            IEnumerable<Playable> next_(Playable rootplayable)
            {

                yield return rootplayable;

                for (var p = rootplayable; p.GetInputCount() != 0; p = p.GetInput(0))
                //for (var p = rootplayable; p.GetOutputCount() != 0; p = p.GetOutput(0))
                {
                    yield return p;
                }
            }
        }
    }
}
