using System.Linq;
using UnityEngine;
using UniVRM10;

namespace AnimLite.Vrm
{
    public static class VrmExpressionUtility
    {
        public static void ApplyBlendShapeToVrmExpression(this Transform tfBase, string targetObjectName)
        {
            var dst_instance = tfBase.GetComponent<UniVRM10.Vrm10Instance>();
            if (dst_instance is null) return;

            var dst_smr = tfBase.Find(targetObjectName)?.GetComponent<SkinnedMeshRenderer>();
            if (dst_smr is null) return;

            var dst_mesh = dst_smr.sharedMesh;
            var q =
                from src_x in dst_instance.Vrm.Expression.Clips
                select (
                    src_expression:
                        src_x,
                    dst_morphs:
                        from src_morph in src_x.Clip.MorphTargetBindings
                        select new MorphTargetBinding
                        {
                            Index = dst_mesh.GetBlendShapeIndex(src_morph.getShapeName(tfBase)),
                            RelativePath = dst_smr.name,
                            Weight = src_morph.Weight,
                        }
                );


            var src_vrm = dst_instance.Vrm;
            var dst_vrm = new VRM10Object
            {
                Expression = new VRM10ObjectExpression(),
                Meta = src_vrm.Meta,
                FirstPerson = src_vrm.FirstPerson,
                LookAt = src_vrm.LookAt,
            };

            // ↓新しいインスタンスを作成することにしたので、下記は不要と思われる
            //// カスタムは重複してしまうので登録から削除する
            //// （プリセットは置き換わるので問題ない）
            //dst_instance.Vrm.Expression.CustomClips.Clear();

            foreach (var x in q)
            {
                var dst_clip = Object.Instantiate(x.src_expression.Clip);
                dst_clip.MorphTargetBindings = x.dst_morphs.ToArray();
                dst_vrm.Expression.AddClip(x.src_expression.Preset, dst_clip);
            }

            dst_instance.Vrm = dst_vrm;
        }

        static string getShapeName(this MorphTargetBinding mtb, Transform tfRoot) =>
            tfRoot
                .Find(mtb.RelativePath)
                .GetComponent<SkinnedMeshRenderer>().sharedMesh
                .GetBlendShapeName(mtb.Index);
    }
}