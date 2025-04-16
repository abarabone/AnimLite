using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using Unity.VisualScripting;
using System;
using System.Threading;

namespace AnimLite.Samples
{
    using AnimLite;
    using AnimLite.Utility;

    using AnimLite.Bvh;
    using AnimLite.Vmd;


    public class BvhPlayer : MonoBehaviour
    {

        [FilePath]
        public PathUnit BvhFilePath;


        public Animator anim;

        public SkinnedMeshRenderer FaceMeshRenderer;


        TransformMappings bone;


        DisposableBag disposabes;

        Key4StreamCache<quaternion> rot_cache;
        Key4StreamCache<float4> pos_cache;

        StreamIndex rot_index;
        StreamIndex pos_index;

        StreamData<quaternion> rot_data;
        StreamData<float4> pos_data;



        StreamingTimer timer;


        BodyMotionOperator<TransformMappings, Tf> bodyOperator;



        private void OnDisable()
        {
            if (this.disposabes == null) return;
            this.disposabes.Dispose();
            this.disposabes = null;

            if (this.anim.IsUnityNull()) return;
            this.anim.UnbindAllStreamHandles();
            this.anim.ResetPose();
        }



        void OnEnable()
        {
            try
            {
                // ファイルからデータを読み下す
                var bvh = this.BvhFilePath.ParseBvh();
                var vmdStreamData = Bvh.BvhParser.BvhToVmdMotionData(bvh);

                // データを利用できる形式に変換する
                this.rot_data = vmdStreamData.bodyKeyStreams.CreateRotationData2(BvhParser.LogicalToPhysicalBones);
                this.pos_data = vmdStreamData.bodyKeyStreams.CreatePositionData2();

                // データアクセスを高速化するための索引を作成する
                this.rot_index = rot_data.CreateIndex(indexBlockLength: 100);
                this.pos_index = pos_data.CreateIndex(indexBlockLength: 100);

                // Forward で利用するキーキャッシュバッファを生成する
                this.rot_cache = rot_data.ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulRot>(timer);
                this.pos_cache = pos_data.ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulPos>(timer);

                // 破棄用にまとめておく
                this.disposabes = new DisposableBag
                {
                    this.rot_data.ToHolderWith(this.rot_cache, this.rot_index),
                    this.pos_data.ToHolderWith(this.pos_cache, this.pos_index),
                };

                // 時間範囲などの情報を持ったタイマーを作成する
                this.timer = new StreamingTimer(rot_data.GetLastKeyTime());

                // ヒューマノイドモデルの情報を構築する
                this.bone = this.anim.BuildTransformMappings();

                //// ＶＭＤを再生のための情報を構築する
                this.bodyOperator = this.anim.ToBodyTransformMotionOperator(this.bone);
                //this.footOperator = this.anim.ToFootIkTransformOperator(this.bone);
                //this.faceOperator = this.anim.ToVrmExpressionOperator(this.face);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
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


            // ＶＭＤを再生する（キーを検索し、計算して Transform に書き出す）
            var tfAnim = this.anim.transform;
            this.bodyOperator.SetLocalMotions(posKeyFinder, rotKeyFinder);
        }

    }
}