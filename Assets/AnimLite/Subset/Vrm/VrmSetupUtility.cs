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

        public static void AdjustLootAt(this Vrm10Instance vrm, Transform tfTarget)
        {
            vrm.LookAtTargetType = VRM10ObjectLookAt.LookAtTargetTypes.SpecifiedTransform;
            vrm.LookAtTarget = tfTarget;
        }
    }
}
