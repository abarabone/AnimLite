using System;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using Unity.Jobs;
using Unity.Mathematics;

using AnimLite.Loader;
using AnimLite.Utility;
using AnimLite.Vmd;
using AnimLite.Vrm;
using AnimLite;
using AnimLite.Utility.Linq;
using AnimLite.Vmd.experimental;

public class VmdJobPlayer : MonoBehaviour
{

    [System.Serializable]
    public class TargetUnit
    {
        [FilePath]
        public PathUnit VmdFilePath;

        public VmdFootIkMode FootIkMode = VmdFootIkMode.auto;

        public Animator Animator;

        // このスクリプトでは、フェイシャルは対象外とした
        //public SkinnedMeshRenderer faceRenderer;
    }

    public TargetUnit[] targets;



    public PrototypeCacheManager Cache;



    async Awaitable Start()
    {
        try
        {

            await using var vmdbag = new AsyncDisposableBag { };

            // モデル１体ずつ並行してロードする
            var q = await Task.WhenAll(this.targets.Select(async t =>
            {
                // ＶＭＤデータをファイルからパースし、ストリームデータをビルドする
                var vmd = await this.Cache?.Holder.VmdCache
                    .GetOrLoadVmdAsync(t.VmdFilePath, "", null, this.destroyCancellationToken);
                vmdbag.Add(vmd);

                // キー検索オブジェクトを構築する
                // ジェネリクスにより「キー補間方式、時間のクリップ方法、検索方法」を指定できる

                var rotKeyFinder = vmd.Value.RotationStreams
                    .ToKeyFinderWith<Key4CatmulRot, Clamp>();

                var posKeyFinder = vmd.Value.PositionStreams
                    .ToKeyFinderWith<Key4CatmulPos, Clamp>();

                // ＶＭＤを再生するための情報を構築する
                var bodyAdjust = await BodyAdjustLoader.DefaultBodyAdjustAsync;
                var bones = t.Animator.BuildVmdTransformMappings(bodyAdjust);

                // モデルごとのジョブパラメータを構築する。今回は足ＩＫの設定だけ指定し、体の設定は自動生成に任せる
                var footop = t.Animator.ToVmdFootIkTransformOperator(bones).WithIkUsage(vmd, t.FootIkMode);
                var param = t.Animator.BuildJobParams(bones, posKeyFinder, rotKeyFinder, footop);
                return param;
            }));


            // ジョブ用のバッファを構築する。アニメーションのためのジョブは、複数モデルをまとめて処理する
            var countlist = q.CountParams();
            using var buf = q.BuildJobBuffers(countlist);


            for (; ; )
            {
                // ジョブのスケジュールは、Update() の度に必要
                using var dep = buf.BuildMotionJobsAndSchedule(Time.deltaTime)
                    .AsDisposable(dep => dep.Complete());

                // animator をジョブに依存させる
                q.ForEach((p, i) =>
                {
                    p.model_data.anim.AddJobDependency(dep);
                });

                await Awaitable.NextFrameAsync(this.destroyCancellationToken);
            }
        }
        catch (OperationCanceledException e)
        {
            Debug.LogWarning(e);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
    }

}
