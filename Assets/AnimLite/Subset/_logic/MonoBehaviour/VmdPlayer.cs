using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using Unity.VisualScripting;
using System;
using System.Threading;

namespace AnimLite.Samples
{
    using AnimLite;
    using AnimLite.IK;
    using AnimLite.Loader;
    using AnimLite.Utility;

    using AnimLite.Vmd;
    using AnimLite.Vrm;


    public class VmdPlayer : MonoBehaviour
    {

        [FilePath]
        public PathUnit VmdFilePath;

        [FilePath]
        public PathUnit FaceMappingFilePath;

        public Animator anim;

        public SkinnedMeshRenderer FaceMeshRenderer;


        TransformMappings bone;
        VrmExpressionMappings face;


        DisposableBag disposabes;

        Key4StreamCache<quaternion> rot_cache;
        Key4StreamCache<float4> pos_cache;
        Key2StreamCache<float> face_cache;

        StreamIndex rot_index;
        StreamIndex pos_index;
        StreamIndex face_index;

        StreamData<quaternion> rot_data;
        StreamData<float4> pos_data;
        StreamData<float> face_data;



        StreamingTimer timer;


        VmdBodyMotionOperator<TransformMappings, Tf> bodyOperator;
        FootIkOperator<Tf> footOperator;
        VrmExpressionOperator faceOperator;



        private void OnDisable()
        {
            if (this.disposabes == null) return;
            this.disposabes.Dispose();
            this.disposabes = null;

            if (this.anim.IsUnityNull()) return;
            this.anim.UnbindAllStreamHandles();
            this.anim.ResetPose();
        }



        async Awaitable OnEnable()
        {
            // ファイルからデータを読み下す
            var vmdStreamData = await VmdLoader.LoadVmdAsync(this.VmdFilePath, default);
            var faceMapping = await VrmLoader.LoadFaceMapAsync(this.FaceMappingFilePath, default);
            var bodyAdjust = await BodyAdjustLoader.DefaultBodyAdjustAsync;

            // データを利用できる形式に変換する
            this.rot_data = vmdStreamData.bodyKeyStreams.CreateRotationData();
            this.pos_data = vmdStreamData.bodyKeyStreams.CreatePositionData();
            this.face_data = vmdStreamData.faceKeyStreams.CreateFaceData(faceMapping);

            // データアクセスを高速化するための索引を作成する
            this.rot_index = rot_data.CreateIndex(indexBlockLength: 100);
            this.pos_index = pos_data.CreateIndex(indexBlockLength: 100);
            this.face_index = face_data.CreateIndex(indexBlockLength: 100);

            // Forward で利用するキーキャッシュバッファを生成する
            this.rot_cache = rot_data.ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulRot>(timer);
            this.pos_cache = pos_data.ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulPos>(timer);
            this.face_cache = face_data.ToKey2CacheFactory().CreateCacheWithInitialize<Clamp, Key2NearestShift>(timer);

            // 破棄用にまとめておく
            this.disposabes = new DisposableBag
            {
                this.rot_data.ToHolderWith(this.rot_cache, this.rot_index),
                this.pos_data.ToHolderWith(this.pos_cache, this.pos_index),
                this.face_data.ToHolderWith(this.face_cache, this.face_index),
            };

            // 時間範囲などの情報を持ったタイマーを作成する
            this.timer = new StreamingTimer(rot_data.GetLastKeyTime());
            
            // ヒューマノイドモデルの情報を構築する
            this.bone = this.anim.BuildVmdTransformMappings(bodyAdjust);
            this.face = faceMapping.BuildStreamingFace();
            //this.face = this.anim.FindFaceRendererIfNothing(this.FaceMeshRenderer)?.sharedMesh?.BuildStreamingFace(faceMapping) ?? default;

            // ＶＭＤを再生のための情報を構築する
            this.bodyOperator = this.anim.ToVmdBodyTransformMotionOperator(this.bone);
            this.footOperator = this.anim.ToFootIkTransformOperator(this.bone);
            this.faceOperator = this.anim.ToVrmExpressionOperator(this.face);

            if (!this.enabled) this.OnDisable();
        }


        void Update()
        {
            if (this.disposabes == null) return;


            // タイマーを進める
            this.timer.ProceedTime(Time.deltaTime);


            // キー検索オブジェクトを構築する
            // ジェネリクスにより「キー補間方式、時間のクリップ方法、検索方法」を指定できる

            var posKeyFinder = this.pos_data
                .ToKeyFinder(this.pos_cache, this.pos_index)
                .With<Key4CatmulPos, Clamp, Forward>(this.timer);

            var rotKeyFinder = this.rot_data
                .ToKeyFinder(this.rot_cache, this.rot_index)
                .With<Key4CatmulRot, Clamp, Forward>(this.timer);

            var faceKeyFinder = this.face_data
                .ToKeyFinder(this.face_cache, this.face_index)
                .With<Key2NearestShift, Clamp, Forward>(this.timer);


            // ＶＭＤを再生する（キーを検索し、計算して Transform に書き出す）
            var tfAnim = this.anim.transform;
            this.bodyOperator.SetLocalMotions(posKeyFinder, rotKeyFinder);
            this.footOperator.SolveLegPositionIk(posKeyFinder, tfAnim.position, tfAnim.rotation);
            this.footOperator.SolveFootRotationIk(rotKeyFinder, tfAnim.position, tfAnim.rotation);
            this.faceOperator.SetFaceExpressions(faceKeyFinder);
        }

    }
}