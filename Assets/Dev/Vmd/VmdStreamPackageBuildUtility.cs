using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace AnimLite.Vmd
{
    using AnimLite;
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;


    public static class VmdStreamPackageBuildExtension
    {

        /// <summary>
        /// 
        /// </summary>
        public static StreamData<quaternion> CreateRotationData(this Dictionary<VmdBoneName, VmdBodyMotionKey[]> nameToStream)//, int indexBlockLength = 100)
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
#if UNITY_EDITOR
            Debug.Log(string.Join(", ", src.Select(x => $"{x.boneid}:{x.keys.Count()}")));
#endif

            var qRotSrc =
                from x in Enumerable.Range(0, (int)MmdBodyBones.length)
                join y in src on (MmdBodyBones)x equals y.boneid into ys
                let boneid = (MmdBodyBones)x
                let blankkeys = new BlankEnumerableStruct<VmdBodyMotionKey>()
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
            this Dictionary<VmdFaceName, VmdFaceKey[]> nameToStream, Dictionary<VmdFaceName, VrmFaceName> vmdToVrm)
        {

            var qSrc =
                from x in vmdToVrm
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

        public static StreamingFace BuildStreamingFace(this Mesh mesh, Dictionary<VmdFaceName, VrmFaceName> vmdToVrm)
        {
            var qGr =
                from x in vmdToVrm.Select((x, istream) => (x, istream))
                let id = VmdFace.FaceNameToId.TryGet(x.x.Value)
                where id.isExists
                group x.istream by id.value
                ;
            var q =
                from gr in qGr.Select((x, ikey) => (x, x.Key, ikey))
                from istream in gr.x
                select new VrmFaceReference
                {
                    expid = gr.Key,
                    faceIndex = gr.ikey,
                    istream = istream,
                };

            var refs = q.ToArray();

#if UNITY_EDITOR
            string.Join(", ", refs.Select(x => $"{x.istream}=>{x.faceIndex}:{x.expid}")).ShowDebugLog();
#endif

            return new StreamingFace
            {
                FaceReferences = refs,

                Expressions = refs
                    .OrderBy(x => x.faceIndex)
                    .Select(x => x.expid)
                    .Distinct()
                    .ToArray(),
            };
        }
    }
}
