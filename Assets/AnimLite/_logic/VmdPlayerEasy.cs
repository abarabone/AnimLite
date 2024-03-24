using UnityEngine;
using UnityEngine.Playables;

namespace AnimLite.Samples
{
    using AnimLite;
    using AnimLite.IK;

    using AnimLite.Vmd;
    using AnimLite.Vrm;
    using AnimLite.Utility;


    public class VmdPlayerEasy : MonoBehaviour
    {

        [FilePath]
        public PathUnit VmdFilePath;

        [FilePath]
        public PathUnit FaceMappingFilePath;

        public Animator anim;
        public SkinnedMeshRenderer faceRenderer;



        async Awaitable Start()
        {
            // 
            var vmd = await VmdData.BuildVmdStreamDataAsync(this.VmdFilePath.ToFullPath(), this.FaceMappingFilePath.ToFullPath(), this.destroyCancellationToken);
            using var vmddata = vmd.data;

            // ＶＭＤを再生のための情報を構築する
            var bone = this.anim.BuildVmdTransformMappings();
            var face = this.anim.FindFaceRendererIfNothing(this.faceRenderer)?.sharedMesh?.BuildStreamingFace(vmd.facemap) ?? default;
            var bodyOperator = this.anim.ToVmdBodyTransformMotionOperator(bone);
            var footOperator = this.anim.ToFootIkTransformOperator(bone);
            var faceOperator = this.anim.ToVrmExpressionOperator(face);


            // 時間範囲などの情報を持ったタイマーを作成する
            var timer = new StreamingTimer(vmd.data.RotationStreams.Streams.GetLastKeyTime());


            var tfAnim = this.anim.transform;

            for (; ; )
            {
                await Awaitable.NextFrameAsync(this.destroyCancellationToken);


                // タイマーを進める
                timer.ProceedTime(Time.deltaTime);


                // キー検索オブジェクトを構築する
                // ジェネリクスにより「キー補間方式、時間のクリップ方法、検索方法」を指定できる

                var rotKeyFinder = vmd.data.RotationStreams
                    .ToKeyFinderWith<Key4CatmulRot, Clamp, Forward>(timer);

                var posKeyFinder = vmd.data.PositionStreams
                    .ToKeyFinderWith<Key4CatmulPos, Clamp, Forward>(timer);

                var faceKeyFinder = vmd.data.FaceStreams
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