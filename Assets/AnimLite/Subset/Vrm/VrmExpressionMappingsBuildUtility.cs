using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnimLite.Vrm
{
    using AnimLite.Utility;
    using AnimLite.Vmd;

    public static class VrmExpressionExtension
    {


        //public static VrmExpressionMappings BuildStreamingFace(this Mesh mesh, VmdFaceMapping facemap)
        public static VrmExpressionMappings BuildStreamingFace(this VmdFaceMapping facemap)
        {
            if (!facemap.IsCreated) return default;

            var qGr =
                from x in facemap.VmdToVrmMaps.Select((x, istream) => (x, istream))
                let id = VrmFace.FaceNameToExpressionId.TryGet(x.x.Value)
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string.Join(", ", refs.Select(x => $"{x.istream}=>{x.faceIndex}:{x.expid}")).ShowDebugLog();
#endif

            return new VrmExpressionMappings
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
