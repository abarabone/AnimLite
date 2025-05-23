﻿using UnityEngine;
using UnityEngine.Playables;

namespace AnimLite.Samples
{
    using AnimLite;
    using AnimLite.IK;

    using AnimLite.Vmd;
    using AnimLite.Vrm;
    using AnimLite.Loader;
    using AnimLite.Utility;


    public class VmdPlayerEasyWithCache : MonoBehaviour
    {
        public PrototypeCacheManager Cache;

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
            await using var vmddata = await this.Cache.Holder.VmdCache.GetOrLoadVmdAsync(vmdpath, facemappath, null, this.destroyCancellationToken);
            await using var facemap = await this.Cache.Holder.VmdCache.facemap.GetOrLoadVmdFaceMappingAsync(facemappath, null, this.destroyCancellationToken);
            var bodyAdjust = await BodyAdjustLoader.DefaultBodyAdjustAsync;

            // ＶＭＤを再生のための情報を構築する
            var bone = this.anim.BuildVmdTransformMappings(bodyAdjust);
            var face = facemap.Value.BuildStreamingFace();
            //var face = this.anim.FindFaceRendererIfNothing(this.faceRenderer)?.sharedMesh?.BuildStreamingFace(facemap) ?? default;
            var bodyOperator = this.anim.ToVmdBodyTransformMotionOperator(bone);
            var footOperator = this.anim.ToVmdFootIkTransformOperator(bone).WithIkUsage(vmddata, VmdFootIkMode.auto);
            var faceOperator = this.anim.ToVrmExpressionOperator(face);

            // 時間範囲などの情報を持ったタイマーを作成する
            var timer = new StreamingTimer(vmddata.Value.RotationStreams.Streams.GetLastKeyTime());


            for (; ; )
            {
                await Awaitable.NextFrameAsync(this.destroyCancellationToken);


                // タイマーを進める
                timer.ProceedTime(Time.deltaTime);


                // キー検索オブジェクトを構築する
                // ジェネリクスにより「キー補間方式、時間のクリップ方法、検索方法」を指定できる

                var rotKeyFinder = vmddata.Value.RotationStreams
                    .ToKeyFinderWith<Key4CatmulRot, Clamp, Forward>(timer);

                var posKeyFinder = vmddata.Value.PositionStreams
                    .ToKeyFinderWith<Key4CatmulPos, Clamp, Forward>(timer);

                var faceKeyFinder = vmddata.Value.FaceStreams
                    //.ToKeyFinderWith<Key2NearestShift, Clamp, Forward>(timer);
                    .ToKeyFinderWith<Key4Catmul, Clamp, Forward>(timer);


                // ＶＭＤを再生する（キーを検索し、計算して Transform に書き出す）
                bodyOperator.SetLocalMotions(posKeyFinder, rotKeyFinder);
                footOperator.SolveLegPositionIk(posKeyFinder);
                footOperator.SolveFootRotationIk(rotKeyFinder);
                faceOperator.SetFaceExpressions(faceKeyFinder);
            }
        }

    }
}