using System.Collections.Generic;
using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace AnimLite.Vmd
{
    using AnimLite;
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;
    using AnimLite.Vrm;


    /// <summary>
    /// ＶＭＤから取得したデータを、回転、移動、表情の StreamData<T> に変換する。
    /// </summary>
    public static class VmdStreamDataBuildExtension
    {


        /// <summary>
        /// StreamData をビルドする。
        /// 任意フォーマットのボーン名から、MmdBodyBones のＩＤにマップされる。
        /// 対応ボーンがない部位は、empty keys が格納される。
        /// </summary>
        public static StreamData<quaternion> CreateRotationData(
            this Dictionary<VmdBoneName, VmdBodyMotionKey[]> nameToStream)
        {
            // dict<name, keys> -> (id, keys)[]
            // 表記ゆれも吸収する（全角半角、漢字違い、表記違いなど）
            var qNameToId =
                from x in nameToStream//.Do(x => Debug.Log($"{x.Key.name}:{x.Count()}"))
                let boneid = VmdBone.MmdBoneNameToId.TryGetOrDefault(x.Key, MmdBodyBones.nobone)
                where boneid != MmdBodyBones.nobone
                //select (boneid, keys: x.Value.OrderBy(x => x.frameno).AsEnumerable())
                select (boneid, keys: x.Value.AsEnumerable())
                ;
            var src = qNameToId
                //.Do(x => Debug.Log($"{x.boneid}={x.keys.Count()}"))
                .ToArray();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(string.Join(", ", src.Select(x => $"{x.boneid}:{x.keys.Count()}")));
#endif

            // MmdBodyBones の順番通りに並び変える。
            // 対応ボーンがない場合は、empty を生成する。
            var qNormalizeOrder =
                from x in Enumerable.Range(0, (int)MmdBodyBones.length)
                join y in src on (MmdBodyBones)x equals y.boneid into ys
                let boneid = (MmdBodyBones)x
                let blankkeys = new EmptyEnumerableStruct<VmdBodyMotionKey>()
                from ny in ys.DefaultIfEmpty((boneid, blankkeys))
                select ny
                ;
            var rotsrc = qNormalizeOrder
                //.Do(x => Debug.Log(x))
                .Select(x => x.keys)
                .ToArray();

            // stream data としてビルドする
            return new StreamData<quaternion>
            {
                KeyStreams = rotsrc.BuildKeyData(key => key.rot, key => key.time, defaultKey: VmdBodyMotionKey.Identity),
                Sections = rotsrc.BuildSectionData(defaultKey: VmdBodyMotionKey.Identity),
            };
        }

        /// <summary>
        /// 
        /// </summary>
        public static StreamData<float4> CreatePositionData(this Dictionary<VmdBoneName, VmdBodyMotionKey[]> nameToStream)
        {
            IEnumerable<VmdBodyMotionKey> tonai_(VmdBoneName name1, VmdBoneName name2 = default) =>
                Enumerable.Concat(
                    nameToStream.TryGetOrBlank(name1),
                    nameToStream.TryGetOrBlank(name2)
                );

            var qPosSrc = new[]
            {
                tonai_("全ての親"),
                tonai_("センター"),
                tonai_("グルーブ"),
                tonai_("下半身"),
                tonai_("左足ＩＫ", "左足IK"),
                tonai_("右足ＩＫ", "右足IK"),
            };

            var sections = qPosSrc.BuildSectionData(defaultKey: VmdBodyMotionKey.Identity);
            var keys = qPosSrc.BuildKeyData(key => key.pos, key => key.time, VmdBodyMotionKey.Identity);

            return new StreamData<float4>
            {
                Sections = sections,
                KeyStreams = keys,
            };
        }






        /// <summary>
        /// ・マッピングテーブルに記載された表情のみ、記載された順に
        /// ・対応がない場合は空
        /// </summary>
        public static StreamData<float> CreateFaceData(
            this Dictionary<VmdFaceName, VmdFaceKey[]> nameToStream, VmdFaceMapping facemap)
        {

            var qSrc =
                from x in facemap
                select nameToStream.TryGetOrBlank(x.Key)
                ;
            var src = qSrc
                //.Do(x => Debug.Log($"{x.boneid}={x.keys.Count()}"))
                .ToArray();

            var sections = src.BuildSectionData();
            var keys = src.BuildKeyData(key => key.weight, key => key.time);

            return new StreamData<float>
            {
                Sections = sections,
                KeyStreams = keys,
            };
        }

    }



    public static class VmdCameraStreamDataBuildExtension
    {


        static VmdCameraMotionKey IdentityMotionKey =>
            new VmdCameraMotionKey
            {
                time = 0.0f,
                fov = 60.0f,
                pos = float4.zero,
                rot = quaternion.identity,
            };


        /// <summary>
        /// 
        /// </summary>
        public static StreamData<T> CreateCameraData<T>(
            this VmdCameraMotionKey[] stream, Func<VmdCameraMotionKey, T> conv)
                where T : unmanaged
        {
            var src = stream.WrapEnumerable();

            return new StreamData<T>
            {
                KeyStreams = src.BuildKeyData(conv, key => key.time, IdentityMotionKey),
                Sections = src.BuildSectionData(defaultKey: IdentityMotionKey),
            };
        }

        //public static StreamData<float4> CreatePositionData(this VmdCameraMotionKey[] stream)
        //{
        //    var src = stream.WrapEnumerable();

        //    return new StreamData<float4>
        //    {
        //        KeyStreams = src.BuildKeyData(key => key.pos, key => key.time, IdentityMotionKey),
        //        Sections = src.BuildSectionData(defaultKey: IdentityMotionKey),
        //    };
        //}

        //public static StreamData<float> CreateCameraParameterData(this VmdCameraMotionKey[] stream)
        //{
        //    var src = stream.WrapEnumerable();

        //    return new StreamData<float>
        //    {
        //        KeyStreams = src.BuildKeyData(key => key.fov, key => key.time, IdentityMotionKey),
        //        Sections = src.BuildSectionData(defaultKey: IdentityMotionKey),
        //    };
        //}



    }
}
