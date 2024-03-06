using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace AnimLite.Vmd
{
    using AnimLite;
    using AnimLite.experimental.a;
    using AnimLite.Utility;
    //using static Unity.VisualScripting.Metadata;


    public static class StreamingBoneExtension
    {


        public static JobPlayableStreamingBone BuildVmdJobStreamingBone(this Animator anim)
        {
            var x = anim.buildVmdStreamingBone<TfHandle>();

            return new JobPlayableStreamingBone
            {
                BoneToStreamIndexMappings = x.a.ToNativeArray(),
                InitialPoseRotations = x.b.ToNativeArray(),
                OptionalBones = x.c,
            };
        }

        public static TransformStreamingBone BuildVmdTransformStreamingBone(this Animator anim)
        {
            var x = anim.buildVmdStreamingBone<Tf>();

            return new TransformStreamingBone
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
            buildVmdStreamingBone<TTf>(this Animator anim)
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
                    HasChest = anim.GetBoneTransform(HumanBodyBones.Chest) != null,
                    HasLeftSholder = anim.GetBoneTransform(HumanBodyBones.LeftShoulder) != null,
                    HasRightSholder = anim.GetBoneTransform(HumanBodyBones.RightShoulder) != null,
                }
            );


            HumanBoneReference<TTf>[] buildRefereces_()
            {
                var htomdict = VmdBone.HumanToMmdBonePrimaryIdList
                    .ToDictionary(x => x.human, x => x.mmd)
                    ;
                //var optdict = VmdBone.ParentOptionBones
                //    .ToDictionary(x => x.baseId, x => x.parentId)
                //    ;

                var q =
                    from i in Enumerable.Range(0, (int)HumanBodyBones.LastBone)
                    let humanId = (HumanBodyBones)i
                    let tf = anim.GetBoneTransform(humanId)
                    where tf != null
                    let vmdId = htomdict.TryGet(humanId)
                    where vmdId.isExists
                    select new HumanBoneReference<TTf>
                    {
                        TransformHandle = anim.CreateTransformProxy<TTf>(tf),

                        HumanBoneId = humanId,
                        StreamId = (int)vmdId.value,
                    };

                var root = new HumanBoneReference<TTf>
                {
                    TransformHandle = anim.CreateTransformProxy<TTf>(anim.GetBoneTransform(HumanBodyBones.Hips).parent),
                    //TransformHandle = anim.BindStreamTransform(anim.avatarRoot),
                    HumanBoneId = HumanBodyBones.LastBone,
                    StreamId = (int)MmdBodyBones.全ての親,
                };

                return q
                    .Prepend(root)
                    //.Do(x => Debug.Log($"{x.HumanBoneId}"))
                    .ToArray();
            }

            BoneRotationInitialPose[] buildInitialPoses_(IEnumerable<HumanBoneReference<TTf>> refs)
            {
                var default_rotation = (world: quaternion.identity, local: quaternion.identity);

                var q =
                    from x in refs
                    let tf = x.HumanBoneId == HumanBodyBones.LastBone
                        ? anim.GetBoneTransform(HumanBodyBones.Hips).parent//anim.avatarRoot
                        : anim.GetBoneTransform(x.HumanBoneId)

                    let rot = VmdBone.HumanBodyToAdjustRotation.TryGetOrDefault(x.HumanBoneId, default_rotation)

                    select new BoneRotationInitialPose
                    {
                        RotLocalize = rot.local * Quaternion.Inverse(rot.world),    // 左からかける
                        RotGlobalize = rot.world,                                   // 右からかける
                    };

                return q
                    //.Do(x => Debug.Log($"{x.RotGlobalize}"))
                    .ToArray();
            }
        }


        /// <summary>
        /// スプリングボーンの位置を、ボーンの deuniform 前のメッシュとの相対関係位置に戻す
        /// </summary>
        public static void DeUniformSpringBones(
            this Animator anim, Dictionary<string, DeUniformingBone> dict)
        {

            var q =
                from tf in anim.GetBoneTransform(HumanBodyBones.Hips).parent.GetComponentsInChildren<Transform>()
                from c in tf.GetComponents<UniVRM10.VRM10SpringBoneCollider>()
                select c
                ;
            foreach (var c in q)
            {
                var unit = dict.TryGetOrDefault(c.name);
                if (unit.tf == default) continue;

                var tfsrc = c.transform;
                var wpos = tfsrc.TransformPoint(c.Offset);
                var lpos = Quaternion.Inverse(unit.newRotation) * (wpos - unit.newPosition);

                c.Offset = lpos;
            }
        }
    }

}
