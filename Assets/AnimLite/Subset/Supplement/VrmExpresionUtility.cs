using System.Linq;
using UnityEngine;
using UniVRM10;

namespace AnimLite.Vrm
{
    public static class VrmExpressionUtility
    {
        public static void ApplyBlendShapeToVrmExpression(this Transform tfBase, string targetObjectName)
        {
            var instance = tfBase.GetComponent<UniVRM10.Vrm10Instance>();
            if (instance is null) return;

            var smr = tfBase.Find(targetObjectName)?.GetComponent<SkinnedMeshRenderer>();
            if (smr is null) return;

            var mesh = smr.sharedMesh;
            var qClipMorph =
                from x in instance.Vrm.Expression.Clips
                select (
                    expression:
                        x,
                    morphs:
                        from morph in x.Clip.MorphTargetBindings
                        select new MorphTargetBinding
                        {
                            Index = mesh.GetBlendShapeIndex(morph.getShapeName(tfBase)),
                            RelativePath = smr.name,
                            Weight = morph.Weight,
                        }
                );

            // �J�X�^���͏d�����Ă��܂��̂œo�^����폜����
            // �i�v���Z�b�g�͒u�������̂Ŗ��Ȃ��j
            instance.Vrm.Expression.CustomClips.Clear();

            foreach (var x in qClipMorph)
            {
                var clip = x.expression.Clip;
                clip.MorphTargetBindings = x.morphs.ToArray();
                instance.Vrm.Expression.AddClip(x.expression.Preset, clip);
            }
        }

        static string getShapeName(this MorphTargetBinding mtb, Transform tfRoot) =>
            tfRoot
                .Find(mtb.RelativePath)
                .GetComponent<SkinnedMeshRenderer>().sharedMesh
                .GetBlendShapeName(mtb.Index);
    }
}