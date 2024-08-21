using System.Collections.Generic;
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
        /// 
        /// </summary>
        public static StreamData<quaternion> CreateRotationData(
            this Dictionary<VmdBoneName, VmdBodyMotionKey[]> nameToStream)//, int indexBlockLength = 100)
        {
            var qSrc =
                from x in nameToStream//.Do(x => Debug.Log($"{x.Key.name}:{x.Count()}"))
                let boneid = VmdBone.MmdBoneNameToId.TryGetOrDefault(x.Key, MmdBodyBones.nobone)
                where boneid != MmdBodyBones.nobone
                //select (boneid, keys: x.Value.OrderBy(x => x.frameno).AsEnumerable())
                select (boneid, keys: x.Value.AsEnumerable())
                ;
            var src = qSrc
                //.Do(x => Debug.Log($"{x.boneid}={x.keys.Count()}"))
                .ToArray();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(string.Join(", ", src.Select(x => $"{x.boneid}:{x.keys.Count()}")));
#endif

            var qRotSrc =
                from x in Enumerable.Range(0, (int)MmdBodyBones.length)
                join y in src on (MmdBodyBones)x equals y.boneid into ys
                let boneid = (MmdBodyBones)x
                let blankkeys = new EmptyEnumerableStruct<VmdBodyMotionKey>()
                from ny in ys.DefaultIfEmpty((boneid, blankkeys))
                    //orderby x
                select ny
                ;
            var rotsrc = qRotSrc

                //.Do(x => Debug.Log(x))
                .Select(x => x.keys)
                .ToArray();

            return new StreamData<quaternion>
            {
                KeyStreams = rotsrc.BuildKeyData(key => key.rot, key => key.time, IdentityMotionKey),
                Sections = rotsrc.BuildSectionData(IdentityMotionKey),
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

            var sections = qPosSrc.BuildSectionData(IdentityMotionKey);
            var keys = qPosSrc.BuildKeyData(key => key.pos, key => key.time, IdentityMotionKey);

            return new StreamData<float4>
            {
                Sections = sections,
                KeyStreams = keys,
            };
        }


        static VmdBodyMotionKey IdentityMotionKey =>
            new VmdBodyMotionKey
            {
                time = 0.0f,
                pos = float4.zero,
                rot = quaternion.identity,
            };







        /// <summary>
        /// ・マッピングテーブルに記載された表情のみ、記載された順に
        /// ・対応がない場合は空
        /// </summary>
        public static StreamData<float> CreateFaceData(
            this Dictionary<VmdFaceName, VmdFaceKey[]> nameToStream, VmdFaceMapping facemap)
        {

            var qSrc =
                from x in facemap.VmdToVrmMaps
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
}
