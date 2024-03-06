using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Playables;

namespace AnimLite.Vmd
{

    public struct VmdFaceOperator
    {
        public StreamingFace face;

        public UniVRM10.Vrm10RuntimeExpression vrmexp;
    }


    public static class VmdFaceOperatorExtension
    {

        public static void SetFaceExpressions<TKeyFinder>(this VmdFaceOperator op, TKeyFinder kf)
            where TKeyFinder : IKeyFinder<float>
        {
            var weightbuf = new NativeArray<float>(op.face.Expressions.Length, Allocator.Temp);

            foreach (var x in op.face.FaceReferences)
            {
                var weight = kf.get(x.istream);
                //Debug.Log($"{x.istream} {x.faceIndex} {x.expid} {weight}");

                weightbuf[x.faceIndex] += weight;
            }

            for (var i = 0; i < weightbuf.Length; i++)
            {
                var exp = op.face.Expressions[i];

                //Debug.Log($"{exp} {i} {math.min(weightbuf[i], 1)}");
                op.vrmexp.SetWeight(exp, math.min(weightbuf[i], 1));
            }

            weightbuf.Dispose();
        }
    }


    public static class VmdFaceOperatorFactoryExtension
    {
        public static VmdFaceOperator ToVmdFaceOperator(this Animator anim, StreamingFace face)
        {

            return new VmdFaceOperator
            {
                face = face,

                vrmexp = anim.GetComponent<UniVRM10.Vrm10Instance>()?.Runtime.Expression,
            };
        }
    }

}
