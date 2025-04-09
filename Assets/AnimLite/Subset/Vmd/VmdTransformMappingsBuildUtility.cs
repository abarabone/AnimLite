using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.Jobs;

namespace AnimLite.Vmd
{
    using AnimLite;
    using AnimLite.experimental.a;
    using AnimLite.Utility;
    using AnimLite.Loader;


    /// <summary>
    /// ストリーム（部位およびボーンに等しい）のＩＤと unity Transform をワンセットにした構造体の配列を構築する。
    /// モーションを再生する際は、配列要素を１つずつ適用していけばよい。
    /// </summary>
    public static class TransformMappingsExtension
    {


        public static TransformHandleMappings BuildVmdPlayableJobTransformMappings(this Animator anim, BodyAdjustData adjust = null)
        {
            if (anim.IsUnityNull()) return default;

            var x = anim.buildVmdTfMappings<TfHandle>(adjust);

            return new TransformHandleMappings
            {
                BoneToStreamIndexMappings = x.a.ToNativeArray(),
                InitialPoseRotations = x.b.ToNativeArray(),
                OptionalBones = x.c,
            };
        }

        public static TransformMappings BuildVmdTransformMappings(this Animator anim, BodyAdjustData adjust = null)
        {
            if (anim.IsUnityNull()) return default;

            var x = anim.buildVmdTfMappings<Tf>(adjust);

            return new TransformMappings
            {
                BoneToStreamIndexMappings = x.a,
                InitialPoseRotations = x.b,
                OptionalBones = x.c,
            };
        }

        //public static TransformAccessMappings BuildVmdTransformArrayMappings(this Animator anim, BodyAdjustData adjust = null)
        //{
        //    if (anim.IsUnityNull()) return default;

        //    var x = anim.buildVmdTfMappings<Tf>(adjust);

        //    return new TransformAccessMappings
        //    {
        //        transformAccessArray = new TransformAccessArray(x.a.Select(x => x.TransformHandle.tf).ToArray()),
        //        BoneToStreamIndexMappings = x.a.Select(x => x.ToTfAccess()).ToNativeArray(),
        //        InitialPoseRotations = x.b.ToNativeArray(),
        //        OptionalBones = x.c,
        //    };
        //}



        /// <summary>
        /// 
        /// </summary>
        static (HumanBoneReference<TTf>[] a, BoneRotationInitialPose[] b, OptionalBoneChecker c)
            buildVmdTfMappings<TTf>(this Animator anim, BodyAdjustData adjust)
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
                var htomdict = VmdBone.HumanToMmdBonePrimaryIdList
                    .ToDictionary(x => x.human, x => x.mmd)
                    ;
                //var optdict = VmdBone.ParentOptionBones
                //    .ToDictionary(x => x.baseId, x => x.parentId)
                //    ;

                var q =
                    //from i in Enumerable.Range(0, (int)HumanBodyBones.LastBone)
                    //let humanId = (HumanBodyBones)i
                    from humanId in VmdBone.HumanBoneToParentDict.Keys//.Do(x => Debug.Log(x))
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
                    //TransformHandle = anim.CreateTransformProxy<TTf>(anim.avatarRoot),
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
                var adjustDefault = new BodyAdjust
                {
                    rotation = quaternion.identity,
                };

                var boneExists = Enumerable.ToHashSet(refs.Select(x => x.HumanBoneId));

                var tfnameToSkeltonDict = anim.avatar.humanDescription.skeleton
                    //.Do(x => Debug.Log($"{x.name} {x.rotation}"))
                    .ToDictionary(x => x.name, x => x);

                var adjustDict = adjust is not null
                    ? adjust
                    : new();


                var rotLocalAppliedDict = new Dictionary<HumanBodyBones, quaternion>()
                {
                    {HumanBodyBones.LastBone, quaternion.identity}
                };

                var q =
                    from x in refs.Skip(1)//.Do(x => Debug.Log(x.HumanBoneId))

                    let this_boneid = x.HumanBoneId
                    let parent_bone_id = boneExists.GetParentBone(this_boneid)

                    let tf = anim.GetBoneTransform(this_boneid)
                    let rot_rest = tfnameToSkeltonDict[tf.name].rotation.As_quaternion()

                    let rot_adjust = adjustDict.TryGetOrDefault(this_boneid, adjustDefault).rotation

                    let rot_parent = rotLocalAppliedDict[parent_bone_id]
                    //let rot_this = rotLocalAppliedDict[this_boneid] = mul(rot_parent, rot_adjust)
                    let rot_this = rotLocalAppliedDict[this_boneid] = mul(rot_parent, rot_adjust, rot_rest)
                    let rot_inv_parent = math.inverse(rot_parent)

                    // inv(apose) * anim * apose * rest
                    select new BoneRotationInitialPose
                    {
                        RotGlobalize = rot_inv_parent,          // 左からかける
                        //RotLocalize = mul(rot_this, rot_rest),  // 右からかける
                        RotLocalize = rot_this,                 // 右からかける
                    };

                return q
                    .Prepend(new BoneRotationInitialPose
                    {
                        RotGlobalize = quaternion.identity,
                        RotLocalize = quaternion.identity,
                    })
                    .ToArray();
            }
        }
        static int i;

        static quaternion mul(quaternion r1, quaternion r2) => math.mul(r1, r2);
        static quaternion mul(quaternion r1, quaternion r2, quaternion r3) => math.mul(math.mul(r1, r2), r3);

        static quaternion rotx_(float deg) => quaternion.RotateX(math.radians(deg));
        static quaternion roty_(float deg) => quaternion.RotateY(math.radians(deg));
        static quaternion rotz_(float deg) => quaternion.RotateZ(math.radians(deg));


        // rot_anim     ... アニメーションのローカル回転
        // rot_apose    ... Ａポーズ修正のローカル回転

        // ・式は inv(rot_apose_parent) * rot_anim * rot_apose

        // ・rot_anim はＡポーズを想定して格納されている
        // ・上腕の rot_pose は親子関係により、前腕にも伝わる
        // ・rot_apose -> rot_anim の順で適用する必要あり

        // ・各ボーンで rot_anim の前に rot_apose を適用する必要がある
        // ・各ボーンで rot_apose を適用すると、子の rot_apose と重複してしまう
        // ・親ボーンの rot_apose を打ち消す必要がある（ inv(rot_apose) ）



        public static HumanBodyBones GetParentBone(this HashSet<HumanBodyBones> boneExists, HumanBodyBones thisbone)
        {
            var parentbone = VmdBone.HumanBoneToParentDict[thisbone];
            if (parentbone == HumanBodyBones.LastBone)
            {
                return parentbone;
            }

            if (boneExists.Contains(parentbone))
            {
                return parentbone;
            }

            return boneExists.GetParentBone(parentbone);
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
