using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using Unity.Jobs;

using AnimLite.Vmd.experimental;
using AnimLite.Loader;
using AnimLite.Utility;
using AnimLite.Vmd;
using AnimLite.Vrm;
using AnimLite;
using AnimLite.Utility.Linq;

using System.Security.Claims;
using Unity.Mathematics;
using System;

public class VmdJobPlayer : MonoBehaviour
{

    [FilePath]
    public PathUnit VmdFilePath;

    public Animator[] anims;

    // このスクリプトでは、フェイシャルは対象外とした
    //public SkinnedMeshRenderer faceRenderer;

    public VmdFootIkMode ikmode;

    public PrototypeCacheManager Cache;


    PlayableGraph graph;


    async Awaitable Start()
    {
        try
        {

            await using var vmdbag = new AsyncDisposableBag { };

            // モデル１体ずつ並行してロードする
            var q = await Task.WhenAll(this.anims.Select(async anim =>
            {
                // ＶＭＤデータをファイルからパースし、ストリームデータをビルドする
                var vmdpath = this.VmdFilePath;
                var vmd = await this.Cache.Holder.VmdCache.GetOrLoadVmdAsync(vmdpath, "", null, this.destroyCancellationToken);
                vmdbag.Add(vmd);

                // キー検索オブジェクトを構築する
                // ジェネリクスにより「キー補間方式、時間のクリップ方法、検索方法」を指定できる

                var rotKeyFinder = vmd.Value.RotationStreams
                    .ToKeyFinderWith<Key4CatmulRot, Clamp>();

                var posKeyFinder = vmd.Value.PositionStreams
                    .ToKeyFinderWith<Key4CatmulPos, Clamp>();

                // ＶＭＤを再生するための情報を構築する
                var bodyAdjust = await BodyAdjustLoader.DefaultBodyAdjustAsync;
                var bones = anim.BuildVmdTransformMappings(bodyAdjust);

                // モデルごとのジョブパラメータを構築する。今回は足ＩＫの設定だけ指定し、体の設定は自動生成に任せる
                var footop = anim.ToVmdFootIkTransformOperator(bones).WithIkUsage(vmd, this.ikmode);
                var param = anim.BuildJobParams(bones, posKeyFinder, rotKeyFinder, footop);
                return param;
            }));


            // ジョブ用のバッファを構築する。アニメーションのためのジョブは、複数モデルをまとめて処理する
            var countlist = q.CountParams();
            using var buf = q.BuildJobBuffers(countlist);


            this.graph = PlayableGraph.Create();

            this.graph.Play();


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

    private void OnDestroy()
    {
        graph.Destroy();
    }
}
