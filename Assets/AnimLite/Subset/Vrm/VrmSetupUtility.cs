using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVRM10;
using System.Linq;

namespace AnimLite.Vrm
{
    public static class VrmSetupUtility
    {
        public static void AdjustBbox(this SkinnedMeshRenderer face, Animator anim)
        {
            var tfprevbone = face.rootBone;
            var tfaftrbone = anim.GetBoneTransform(HumanBodyBones.Head);

            face.rootBone = tfaftrbone;
            var bbox = face.localBounds;
            bbox.center = bbox.center - tfprevbone.InverseTransformPoint(tfaftrbone.position);
            face.localBounds = bbox;
        }

        public static void AdjustBbox(this Animator anim)
        {
            var tfRoot = anim.GetBoneTransform(HumanBodyBones.Hips).parent;
            var tfHead = anim.GetBoneTransform(HumanBodyBones.Head);

            var smrs = anim.GetComponentsInChildren<SkinnedMeshRenderer>();
            smrs.ForEach(smr =>
                {
                    smr.rootBone = smr.sharedMesh.blendShapeCount > 0
                        ? tfHead
                        : tfRoot
                        ;
                });
        }


        //public static Bounds EncapsulateTo(this Bounds self, Bounds other)
        //{
        //    self.Encapsulate(other);
        //    return self;
        //}

        //public static Bounds CalcLocalBounds(this IEnumerable<Bounds> others) =>
        //    others.Aggregate(new Bounds(), (pre, next) => pre.EncapsulateTo(next));

        //public static Bounds CalcLocalBounds(this IEnumerable<SkinnedMeshRenderer> others) =>
        //    //others.Select(x => x.sharedMesh.bounds).CalcLocalBounds();
        //    others.Select(x => x.localBounds).CalcLocalBounds();

        //public static Bounds CalcLocalBounds(this IEnumerable<SkinnedMeshRenderer> others, Transform tfbase)
        //{
        //    var mtInv = tfbase?.worldToLocalMatrix ?? Matrix4x4.identity;
        //    //others.Select(x => x.sharedMesh.bounds).CalcLocalBounds();
        //    //others.Select(x => x.localBounds).CalcLocalBounds();
        //    return others
        //        .Where(x => x.rootBone is not null)
        //        .Select(x =>
        //        {
        //            var bounds = x.localBounds;
        //            bounds.center = (x.rootBone.localToWorldMatrix * mtInv).MultiplyPoint(bounds.center);
        //            return bounds;
        //        })
        //        .CalcLocalBounds();
        //}




        public static void AdjustLootAt(this Vrm10Instance vrm, Transform tfTarget)
        {
            vrm.LookAtTargetType = VRM10ObjectLookAt.LookAtTargetTypes.SpecifiedTransform;
            vrm.LookAtTarget = tfTarget;
        }
    }
}
