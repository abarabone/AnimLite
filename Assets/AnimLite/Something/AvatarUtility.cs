using System.Linq;
using UnityEngine;

namespace AnimLite.Utility.experimental
{
    public static class AvatarUtility
    {


        public static void RebuildAvatar(this Animator anim)
        {
            //var bonedict = anim.CalculateDeUniformingBoneValues();

            //var a = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
            //Debug.Log(a.localPosition);

            ////var bonedict = anim.GetComponentsInChildren<Transform>()
            var bonedict = anim.GetComponentInChildren<SkinnedMeshRenderer>().bones
                .ToDictionary(x => x.name, x => x);

            //var a = bonedict["foot.L"];
            //Debug.Log(a.newLocalPosition);

            var desc = anim.avatar.humanDescription;

            var wrot = anim.transform.rotation;

            for (var i = 0; i < desc.skeleton.Length; i++)
            {
                var x = desc.skeleton[i];
                var d = bonedict.TryGetOrDefault(x.name);

                if (d == null) continue;
                //Debug.Log($"{x.name} {d.newLocalPosition} {d.newPosition}");
                //Debug.Log($"{x.name} {d.localPosition} {d.localRotation}");

                //x.position = d.newPosition;
                //x.rotation = d.newRotation;
                //x.scale = Vector3.one;
                desc.skeleton[i] = new SkeletonBone
                {
                    name = x.name,
                    //position = d.newLocalPosition,
                    //rotation = d.newLocalRotation,
                    position = d.localPosition,
                    rotation = Quaternion.Inverse(d.parent.rotation) * wrot,
                    //rotation = d.localRotation,
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
