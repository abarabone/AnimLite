using UnityEngine;
using UnityEngine.Playables;

namespace AnimLite.Samples
{
    using AnimLite;
    using AnimLite.IK;

    using AnimLite.Vmd;
    using AnimLite.Vrm;
    using AnimLite.Utility;


    public class VmdPlayerEasyWithCache : MonoBehaviour
    {
        public VmdStreamDataCache Cache;

        [FilePath]
        public PathUnit VmdFilePath;

        [FilePath]
        public PathUnit FaceMappingFilePath;

        public Animator anim;
        public SkinnedMeshRenderer faceRenderer;



        async Awaitable Start()
        {
            // ＶＭＤデータをファイルからパースし、ストリームデータをビルドする
            var vmdpath = this.VmdFilePath;
            var facemappath = this.FaceMappingFilePath;
            var (vmddata, facemap) = await this.Cache.GetOrLoadVmdStreamDataAsync(vmdpath, facemappath, this.destroyCancellationToken);
            using var _ = vmddata;
            
            // ＶＭＤを再生のための情報を構築する
            var bone = this.anim.BuildVmdTransformMappings();
            var face = facemap.BuildStreamingFace();
            //var face = this.anim.FindFaceRendererIfNothing(this.faceRenderer)?.sharedMesh?.BuildStreamingFace(facemap) ?? default;
            var bodyOperator = this.anim.ToVmdBodyTransformMotionOperator(bone);
            var footOperator = this.anim.ToFootIkTransformOperator(bone);
            var faceOperator = this.anim.ToVrmExpressionOperator(face);

            // 時間範囲などの情報を持ったタイマーを作成する
            var timer = new StreamingTimer(vmddata.RotationStreams.Streams.GetLastKeyTime());


            var tfAnim = this.anim.transform;

            for (; ; )
            {
                await Awaitable.NextFrameAsync(this.destroyCancellationToken);


                // タイマーを進める
                timer.ProceedTime(Time.deltaTime);


                // キー検索オブジェクトを構築する
                // ジェネリクスにより「キー補間方式、時間のクリップ方法、検索方法」を指定できる

                var rotKeyFinder = vmddata.RotationStreams
                    .ToKeyFinderWith<Key4CatmulRot, Clamp, Forward>(timer);

                var posKeyFinder = vmddata.PositionStreams
                    .ToKeyFinderWith<Key4CatmulPos, Clamp, Forward>(timer);

                var faceKeyFinder = vmddata.FaceStreams
                    .ToKeyFinderWith<Key2NearestShift, Clamp, Forward>(timer);


                // ＶＭＤを再生する（キーを検索し、計算して Transform に書き出す）
                bodyOperator.SetLocalMotions(posKeyFinder, rotKeyFinder);
                footOperator.SolveLegPositionIk(posKeyFinder, tfAnim.position, tfAnim.rotation);
                footOperator.SolveFootRotationIk(rotKeyFinder, tfAnim.position, tfAnim.rotation);
                faceOperator.SetFaceExpressions(faceKeyFinder);
            }
        }

    }
}