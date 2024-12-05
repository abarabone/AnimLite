using System.Collections.Generic;
using UnityEngine;

namespace AnimLite.experimental.a
{
    using AnimLite.Utility;

    public static class AvatarUtility
    {


        public static void RebuildAvatar(this Animator anim, Dictionary<string, DeUniformingBone> bonedict)
        {
            //var a = bonedict["foot.L"];
            //Debug.Log(a.newLocalPosition);

            var desc = anim.avatar.humanDescription;

            var wrot = anim.transform.rotation;

            for (var i = 0; i < desc.skeleton.Length; i++)
            {
                var x = desc.skeleton[i];
                var d = bonedict.TryGetOrDefault(x.name);

                if (d.tf == null) continue;
                //Debug.Log($"{x.name} {d.newLocalPosition} {d.newPosition}");
                //Debug.Log($"{x.name} {d.localPosition} {d.localRotation}");

                desc.skeleton[i] = new SkeletonBone
                {
                    name = x.name,
                    position = d.newLocalPosition,
                    rotation = Quaternion.identity,//d.newLocalRotation,
                    scale = Vector3.one,
                };
            }


            var newavatar = AvatarBuilder.BuildHumanAvatar(anim.avatarRoot.gameObject, desc);
            newavatar.name = anim.avatar.name + "_new";
            //Debug.Log($"{newavatar.isValid} {newavatar.isHuman}");

            anim.avatar = newavatar;
        }


    }
}
