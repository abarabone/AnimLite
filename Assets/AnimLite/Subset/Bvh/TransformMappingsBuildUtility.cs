using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Unity.VisualScripting;

namespace AnimLite
{
    using AnimLite;
    using AnimLite.experimental.a;
    using AnimLite.Utility;
    //using static Unity.VisualScripting.Metadata;


    /// <summary>
    /// ストリーム（部位およびボーンに等しい）のＩＤと unity Transform をワンセットにした構造体の配列を構築する。
    /// モーションを再生する際は、配列要素を１つずつ適用していけばよい。
    /// </summary>
    public static class TransformMappingsExtension
    {


        public static TransformHandleMappings BuildPlayableJobTransformMappings(this Animator anim)
        {
            if (anim.IsUnityNull()) return default;

            var x = anim.buildTfMappings<TfHandle>();

            return new TransformHandleMappings
            {
                BoneToStreamIndexMappings = x.a.ToNativeArray(),
                InitialPoseRotations = x.b.ToNativeArray(),
                OptionalBones = x.c,
            };
        }

        public static TransformMappings BuildTransformMappings(this Animator anim)
        {
            if (anim.IsUnityNull()) return default;

            var x = anim.buildTfMappings<Tf>();

            return new TransformMappings
            {
                BoneToStreamIndexMappings = x.a,
                InitialPoseRotations = x.b,
                OptionalBones = x.c,
            };
        }




        /// <summary>
        /// 
        /// </summary>
        static (HumanBoneReference<TTf>[] a, BoneRotationInitialPose[] b, OptionalBoneChecker c)
            buildTfMappings<TTf>(this Animator anim)
                where TTf : ITransformProxy, new()
        {
            var refs = buildRefereces_();
            var rots = buildInitialPoses_(refs);

            return
            (
                refs,
                rots,

                new OptionalBoneChecker
                {
                    //HasChest = anim.GetBoneTransform(HumanBodyBones.Chest) != null,
                    HasChest = anim.GetBoneTransform(HumanBodyBones.UpperChest) != null,
                    HasLeftSholder = anim.GetBoneTransform(HumanBodyBones.LeftShoulder) != null,
                    HasRightSholder = anim.GetBoneTransform(HumanBodyBones.RightShoulder) != null,
                }
            );


            HumanBoneReference<TTf>[] buildRefereces_()
            {
                var q =
                    from i in Enumerable.Range(0, (int)HumanBodyBones.LastBone)
                    let humanId = (HumanBodyBones)i
                    let tf = anim.GetBoneTransform(humanId)
                    where tf != null
                    select new HumanBoneReference<TTf>
                    {
                        TransformHandle = anim.CreateTransformProxy<TTf>(tf),

                        HumanBoneId = humanId,
                        StreamId = (int)humanId,
                    };

                var root = new HumanBoneReference<TTf>
                {
                    TransformHandle = anim.CreateTransformProxy<TTf>(anim.GetBoneTransform(HumanBodyBones.Hips).parent),
                    //TransformHandle = anim.CreateTransformProxy<TTf>(anim.avatarRoot),
                    HumanBoneId = HumanBodyBones.LastBone,
                    StreamId = (int)HumanBodyBones.LastBone,
                };

                return q
                    //.Prepend(root)
                    //.Do(x => Debug.Log($"{x.HumanBoneId}"))
                    .ToArray();
            }

            BoneRotationInitialPose[] buildInitialPoses_(IEnumerable<HumanBoneReference<TTf>> refs)
            {
                var q =
                    from x in refs

                    select new BoneRotationInitialPose
                    {
                        RotLocalize = quaternion.identity,
                        RotGlobalize = quaternion.identity,
                    };

                return q
                    .ToArray();
            }
        }


        /////// <summary>
        /////// スプリングボーンの位置を、ボーンの deuniform 前のメッシュとの相対関係位置に戻す
        /////// </summary>
        ////public static void DeUniformSpringBones(
        ////    this Animator anim, Dictionary<string, DeUniformingBone> dict)
        ////{

        ////    var q =
        ////        from tf in anim.GetBoneTransform(HumanBodyBones.Hips).parent.GetComponentsInChildren<Transform>()
        ////        from c in tf.GetComponents<UniVRM10.VRM10SpringBoneCollider>()
        ////        select c
        ////        ;
        ////    foreach (var c in q)
        ////    {
        ////        var unit = dict.TryGetOrDefault(c.name);
        ////        if (unit.tf == default) continue;

        ////        var tfsrc = c.transform;
        ////        var wpos = tfsrc.TransformPoint(c.Offset);
        ////        var lpos = Quaternion.Inverse(unit.newRotation) * (wpos - unit.newPosition);

        ////        c.Offset = lpos;
        ////    }
        ////}
    }

}
