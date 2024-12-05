using UnityEngine;
using UnityEngine.Playables;
using System;

namespace AnimLite.Samples
{
    using AnimLite;
    using AnimLite.Utility;
    using AnimLite.DancePlayable;

    using AnimLite.Vmd;
    using AnimLite.Vrm;


    public class VmdPlayerWithPlayableJob : MonoBehaviour
    {

        [FilePath]
        public PathUnit VmdFilePath;

        [FilePath]
        public PathUnit FaceMappingFilePath;

        public Animator anim;
        public SkinnedMeshRenderer faceRenderer;



        async Awaitable Start()
        {
            // ファイルからデータを読み下す
            var vmdpath = this.VmdFilePath;
            var facemap = this.FaceMappingFilePath;
            var vmdStreamData = await VmdLoader.LoadVmdExAsync(vmdpath, this.destroyCancellationToken);
            var faceMapping = await VrmLoader.LoadFaceMapExAsync(facemap, this.destroyCancellationToken);

            // データを利用できる形式に変換する
            using var rot_data = vmdStreamData.bodyKeyStreams.CreateRotationData();
            using var pos_data = vmdStreamData.bodyKeyStreams.CreatePositionData();
            using var face_data = vmdStreamData.faceKeyStreams.CreateFaceData(faceMapping);

            // データアクセスを高速化するための索引を作成する
            using var rot_index = rot_data.CreateIndex(indexBlockLength: 100);
            using var pos_index = pos_data.CreateIndex(indexBlockLength: 100);
            using var face_index = face_data.CreateIndex(indexBlockLength: 100);

            // 時間範囲などの情報を持ったタイマーを作成する
            var timer = new StreamingTimer(rot_data.GetLastKeyTime());

            // Forward で利用するキーキャッシュバッファを生成する
            using var rot_cache = rot_data.ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulRot>(timer);
            using var pos_cache = pos_data.ToKey4CacheFactory().CreateCacheWithInitialize<Clamp, Key4CatmulPos>(timer);
            using var face_cache = face_data.ToKey2CacheFactory().CreateCacheWithInitialize<Clamp, Key2NearestShift>(timer);


            // ヒューマノイドモデルの情報を構築する
            using var bone = this.anim.BuildVmdPlayableJobTransformMappings();
            var face = faceMapping.BuildStreamingFace();
            //var face = this.faceRenderer.sharedMesh.BuildStreamingFace(faceMapping);




            // キー検索オブジェクトを構築する
            // ジェネリクスにより「キー補間方式、時間のクリップ方法」を指定できる
            // （検索方法は job の中で、Absolute と Forward を適宜使い分けるようになっている）

            var rotKeyFinder = rot_data
                .ToKeyFinder(rot_cache, rot_index)
                .With<Key4CatmulRot, Clamp>();

            var posKeyFinder = pos_data
                .ToKeyFinder(pos_cache, pos_index)
                .With<Key4CatmulPos, Clamp>();

            var faceKeyFinder = face_data
                .ToKeyFinder(face_cache, face_index)
                .With<Key2NearestShift, Clamp>();



            // プレイアブルのグラフを構築して再生する

            var graph = PlayableGraph.Create(name + " anim");


            var job = this.anim.create(bone, posKeyFinder, rotKeyFinder, timer);

            graph.CreateVmdAnimationJobWithSyncScript(this.anim, job, timer, delay: 1);

            graph.CreateVmdFaceAnimation(anim.gameObject, faceKeyFinder, face, timer, delay: 1);


            graph.Play();


            var cts = this.destroyCancellationToken.Register(() => graph.Destroy());

            await Awaitable.WaitForSecondsAsync(float.PositiveInfinity, cts.Token);

        }

    }
}