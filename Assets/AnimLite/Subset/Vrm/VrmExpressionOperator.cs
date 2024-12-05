using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace AnimLite.Vrm
{

    public struct VrmExpressionOperator
    {
        public VrmExpressionMappings face;

        public UniVRM10.Vrm10RuntimeExpression vrmexp;
    }


    public static class VrmExpressionOperatorExtension
    {

        public static void SetFaceExpressions<TKeyFinder>(this VrmExpressionOperator op, TKeyFinder kf)
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


    public static class VrmExpressionOperatorFactoryExtension
    {

        public static VrmExpressionOperator ToVrmExpressionOperator(this GameObject model, VrmExpressionMappings face) =>
            model.GetComponent<Animator>().ToVrmExpressionOperator(face);

        public static VrmExpressionOperator ToVrmExpressionOperator(this Animator anim, VrmExpressionMappings face)
        {

            return new VrmExpressionOperator
            {
                face = face,

                vrmexp = anim.GetComponent<UniVRM10.Vrm10Instance>()?.Runtime.Expression,
            };
        }
    }

}
