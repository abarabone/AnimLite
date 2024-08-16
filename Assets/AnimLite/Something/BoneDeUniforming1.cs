using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace AnimLite.experimental.a
{
    using AnimLite.Utility;
    using AnimLite.Vmd;


    public struct DeUniformingBone
    {
        public Transform tf;

        public Vector3 newPosition;
        public Quaternion newRotation;

        public Vector3 newLocalPosition;
        public Quaternion newLocalRotation;
    }


    public static class DeUniformHumanBone
    {

        public static void DeUniformingSkinMeshes(
            this Animator anim, Dictionary<string, DeUniformingBone> dict, bool needCloneMesh = true)
        {
            var savelocation = moveToZero_RootPositionAndRotation_();


            dict.deUniformBone();


            var renderers = anim.GetComponentsInChildren<SkinnedMeshRenderer>();

            if (needCloneMesh)
                renderers.rebuildBindposes_WithCloneMesh();// dict);
            else
                renderers.rebuildBindposes();


            restore_RootPositionAndRotation_(savelocation);
            return;


            (Vector3, quaternion) moveToZero_RootPositionAndRotation_()
            {
                var rootpos = anim.transform.position;
                var rootrot = anim.transform.rotation;

                anim.transform.position = Vector3.zero;
                anim.transform.rotation = quaternion.identity;
                //Debug.Log($"{rootpos} {rootrot}");

                return (rootpos, rootrot);
            }
            void restore_RootPositionAndRotation_((Vector3 pos, quaternion rot) root)
            {
                anim.transform.SetPositionAndRotation(root.pos, root.rot);
            }
        }

        static void rebuildBindposes_WithCloneMesh(
            this SkinnedMeshRenderer[] renderers, Dictionary<string, DeUniformingBone> dict)
        {
            //var bindposes = renderers.First().sharedMesh.bindposes;
            //var bones = renderers.First().bones;

            //for (var i = 0; i < bindposes.Length; i++)
            //{
            //    var unit = dict.TryGet(bones[i].name);
            //    if (!unit.isExists) continue;

            //    bindposes[i] = Matrix4x4.TRS(unit.value.newPosition, unit.value.newRotation, Vector3.one).inverse;
            //}
            var qBindpose =
                from tf in renderers.First().bones
                let unit = dict.GetOrDefault(tf.name)
                select unit.tf != null
                    ? Matrix4x4.TRS(unit.newPosition, unit.newRotation, Vector3.one).inverse
                    : tf.worldToLocalMatrix
                ;
            var bindposes = qBindpose.ToArray();

            foreach (var r in renderers)
            {
                var mesh = Mesh.Instantiate(r.sharedMesh);
                mesh.bindposes = bindposes;
                r.sharedMesh = mesh;
            }
        }
        static void rebuildBindposes_WithCloneMesh(this SkinnedMeshRenderer[] renderers)
        {
            var bindposes = renderers.First().bones
                .Select(tf => tf.worldToLocalMatrix)
                .ToArray();

            foreach (var r in renderers)
            {
                var mesh = Mesh.Instantiate(r.sharedMesh);
                mesh.bindposes = bindposes;
                r.sharedMesh = mesh;
            }
        }
        static void rebuildBindposes(this SkinnedMeshRenderer[] renderers)
        {
            var bindposes = renderers.First().bones
                .Select(tf => tf.worldToLocalMatrix)
                .ToArray();

            foreach (var r in renderers)
            {
                r.sharedMesh.bindposes = bindposes;
            }
        }

        static void deUniformBone(this Dictionary<string, DeUniformingBone> dict)
        {
            foreach (var x in dict.Values)
            {
                if (x.tf == null) continue;

                var tf = x.tf;


                var tfs = getChildren_();
                tf.DetachChildren();

                tf.SetLocalPositionAndRotation(x.newLocalPosition, x.newLocalRotation);
                Debug.Log($"{x.tf.name} {x.newRotation} {x.newLocalRotation}");

                re_atach_(tfs);
                //Debug.Log($"{tfCurr.name}->{tfNext.name} : {wcpos.AsVector3()}->{wnpos.AsVector3()} =>> {tfCurr.position}->{tfNext.position}");


                Transform[] getChildren_() =>
                    Enumerable.Range(0, tf.childCount)
                        .Select(x => tf.GetChild(x))
                        .ToArray();

                void re_atach_(Transform[] tfs) =>
                    tfs.ForEach(x => x.SetParent(tf, worldPositionStays: true));
            }

        }







        public static Dictionary<string, DeUniformingBone> CalculateDeUniformingBonePose(this Animator anim)
        {

            var dict = new Dictionary<string, DeUniformingBone>();

            var tfroot = anim.GetBoneTransform(HumanBodyBones.Hips).parent;
            dict.Add(tfroot.name, new DeUniformingBone
            {
                newPosition = tfroot.position,
                newRotation = tfroot.rotation,
            });


            anim.re_transform_(dict, HumanBodyBones.Hips, Vector3.up);
            anim.re_transform_(dict, HumanBodyBones.Spine, Vector3.up);
            anim.re_transform_(dict, HumanBodyBones.Chest, Vector3.up);
            anim.re_transform_(dict, HumanBodyBones.UpperChest, Vector3.up);
            anim.re_transform_(dict, HumanBodyBones.Neck, Vector3.up);
            anim.re_transform_ident_(dict, HumanBodyBones.Head);

            //{ HumanBodyBones.LeftEye, HumanBodyBones.LeftEye},//
            //{ HumanBodyBones.RightEye, HumanBodyBones.RightEye},//
            //{ HumanBodyBones.Jaw, HumanBodyBones.Jaw},//


            anim.re_transform_(dict, HumanBodyBones.LeftUpperLeg, Vector3.down);
            anim.re_transform_(dict, HumanBodyBones.LeftLowerLeg, Vector3.down);
            anim.re_transform_xz_(dict, HumanBodyBones.LeftFoot, Vector3.forward);
            anim.re_transform_from_parent_xz_(dict, HumanBodyBones.LeftToes);

            anim.re_transform_(dict, HumanBodyBones.RightUpperLeg, Vector3.down);
            anim.re_transform_(dict, HumanBodyBones.RightLowerLeg, Vector3.down);
            anim.re_transform_xz_(dict, HumanBodyBones.RightFoot, Vector3.forward);
            anim.re_transform_from_parent_xz_(dict, HumanBodyBones.RightToes);


            //anim.re_transform_(dict, HumanBodyBones.LeftShoulder, Vector3.left);
            anim.re_transform_ident_(dict, HumanBodyBones.LeftShoulder);//肩は正規化のままでよいのかも
            anim.re_transform_(dict, HumanBodyBones.LeftUpperArm, Vector3.left);
            anim.re_transform_(dict, HumanBodyBones.LeftLowerArm, Vector3.left);
            //anim.re_transform_terminal_ident_(dict, HumanBodyBones.LeftHand);
            anim.re_transform_(dict, HumanBodyBones.LeftHand, Vector3.left);//中指につなげる

            //anim.re_transform_(dict, HumanBodyBones.RightShoulder, Vector3.right);
            anim.re_transform_ident_(dict, HumanBodyBones.RightShoulder);//肩は正規化のままでよいのかも
            anim.re_transform_(dict, HumanBodyBones.RightUpperArm, Vector3.right);
            anim.re_transform_(dict, HumanBodyBones.RightLowerArm, Vector3.right);
            //anim.re_transform_terminal_ident_(dict, HumanBodyBones.RightHand);
            anim.re_transform_(dict, HumanBodyBones.RightHand, Vector3.right);//中指につなげる


            //anim.re_transform_(dict, HumanBodyBones.LeftThumbProximal, math.normalize(Vector3.left + Vector3.forward));
            anim.re_transform_ident_(dict, HumanBodyBones.LeftThumbProximal);
            anim.re_transform_(dict, HumanBodyBones.LeftThumbIntermediate, math.normalize(Vector3.left + Vector3.forward));
            //anim.re_transform_(dict, HumanBodyBones.LeftThumbIntermediate, Vector3.left);
            anim.re_transform_from_parent_(dict, HumanBodyBones.LeftThumbDistal);

            anim.re_transform_(dict, HumanBodyBones.LeftIndexProximal, Vector3.left);
            anim.re_transform_(dict, HumanBodyBones.LeftIndexIntermediate, Vector3.left);
            anim.re_transform_from_parent_(dict, HumanBodyBones.LeftIndexDistal);

            anim.re_transform_(dict, HumanBodyBones.LeftMiddleProximal, Vector3.left);
            anim.re_transform_(dict, HumanBodyBones.LeftMiddleIntermediate, Vector3.left);
            anim.re_transform_from_parent_(dict, HumanBodyBones.LeftMiddleDistal);

            anim.re_transform_(dict, HumanBodyBones.LeftRingProximal, Vector3.left);
            anim.re_transform_(dict, HumanBodyBones.LeftRingIntermediate, Vector3.left);
            anim.re_transform_from_parent_(dict, HumanBodyBones.LeftRingDistal);

            anim.re_transform_(dict, HumanBodyBones.LeftLittleProximal, Vector3.left);
            anim.re_transform_(dict, HumanBodyBones.LeftLittleIntermediate, Vector3.left);
            anim.re_transform_from_parent_(dict, HumanBodyBones.LeftLittleDistal);


            //anim.re_transform_(dict, HumanBodyBones.RightThumbProximal, math.normalize(Vector3.right + Vector3.forward));
            anim.re_transform_ident_(dict, HumanBodyBones.RightThumbProximal);
            anim.re_transform_(dict, HumanBodyBones.RightThumbIntermediate, math.normalize(Vector3.right + Vector3.forward));
            //anim.re_transform_(dict, HumanBodyBones.RightThumbIntermediate, Vector3.right);
            anim.re_transform_from_parent_(dict, HumanBodyBones.RightThumbDistal);

            anim.re_transform_(dict, HumanBodyBones.RightIndexProximal, Vector3.right);
            anim.re_transform_(dict, HumanBodyBones.RightIndexIntermediate, Vector3.right);
            anim.re_transform_from_parent_(dict, HumanBodyBones.RightIndexDistal);

            anim.re_transform_(dict, HumanBodyBones.RightMiddleProximal, Vector3.right);
            anim.re_transform_(dict, HumanBodyBones.RightMiddleIntermediate, Vector3.right);
            anim.re_transform_from_parent_(dict, HumanBodyBones.RightMiddleDistal);

            anim.re_transform_(dict, HumanBodyBones.RightRingProximal, Vector3.right);
            anim.re_transform_(dict, HumanBodyBones.RightRingIntermediate, Vector3.right);
            anim.re_transform_from_parent_(dict, HumanBodyBones.RightRingDistal);

            anim.re_transform_(dict, HumanBodyBones.RightLittleProximal, Vector3.right);
            anim.re_transform_(dict, HumanBodyBones.RightLittleIntermediate, Vector3.right);
            anim.re_transform_from_parent_(dict, HumanBodyBones.RightLittleDistal);


            return dict;
        }

        static DeUniformingBone add(this Dictionary<string, DeUniformingBone> dict, Transform tf, Vector3 wpos, Quaternion wrot)
        {
            var p = dict[tf.parent.name];
            var invprot = Quaternion.Inverse(p.newRotation);

            return dict[tf.name] = new DeUniformingBone
            {
                tf = tf,

                newPosition = wpos,
                newRotation = wrot,

                newLocalPosition = invprot * (wpos - p.newPosition),
                newLocalRotation = invprot * wrot,
            };
        }

        static DeUniformingBone re_transform_(
            this Animator anim, Dictionary<string, DeUniformingBone> dict, HumanBodyBones bone, float3 normaldir)
        {
            return anim._re_transform_(dict, bone, normaldir, x => math.normalize(x));
        }
        static DeUniformingBone re_transform_xz_(
            this Animator anim, Dictionary<string, DeUniformingBone> dict, HumanBodyBones bone, float3 normaldir)
        {
            return anim._re_transform_(dict, bone, normaldir, x => xz_(x));

            static float3 xz_(float3 v) => math.normalize(new float3(v.x, 0.0f, v.z));
        }
        static DeUniformingBone _re_transform_(
            this Animator anim, Dictionary<string, DeUniformingBone> dict, HumanBodyBones bone, float3 normaldir, Func<float3, float3> f)
        {
            var curr = bone;
            var tfCurr = anim.GetBoneTransform(curr);
            if (tfCurr == null) return default;

            var next = NextBoneDictinary[curr];
            var tfNext = anim.GetBoneTransform(next) ?? anim.GetBoneTransform(NextBoneDictinary[next]);
            if (tfNext == null) return default;

            var wnpos = tfNext.position.As_float3();
            var wcpos = tfCurr.position.As_float3();
            var wcrot = calc_wrot_(f(wnpos - wcpos), normaldir);

            return dict.add(tfCurr, wcpos, wcrot);


            static quaternion calc_wrot_(float3 dir, float3 normaldir)
            {
                var angle = math.acos(math.dot(normaldir, dir));
                var axis = math.normalize(math.cross(normaldir, dir));
                return quaternion.AxisAngle(axis, angle);
            }
        }

        static DeUniformingBone re_transform_ident_(
            this Animator anim, Dictionary<string, DeUniformingBone> dict, HumanBodyBones bone)
        {
            var curr = bone;
            var tfCurr = anim.GetBoneTransform(curr);
            if (tfCurr == null) return default;

            return dict.add(tfCurr, tfCurr.position, Quaternion.identity);
        }
        static DeUniformingBone re_transform_from_parent_(
            this Animator anim, Dictionary<string, DeUniformingBone> dict, HumanBodyBones bone)
        {
            var curr = bone;
            var tfCurr = anim.GetBoneTransform(curr);
            if (tfCurr == null) return default;

            return dict.add(tfCurr, tfCurr.position, tfCurr.parent.rotation);
        }
        static DeUniformingBone re_transform_from_parent_xz_(
            this Animator anim, Dictionary<string, DeUniformingBone> dict, HumanBodyBones bone)
        {
            var curr = bone;
            var tfCurr = anim.GetBoneTransform(curr);
            if (tfCurr == null) return default;

            var dirxz = xz_(tfCurr.parent.forward);
            var wrot = quaternion.LookRotation(dirxz, Vector3.up);
            return dict.add(tfCurr, tfCurr.position, wrot);

            static float3 xz_(float3 v) => math.normalize(new float3(v.x, 0.0f, v.z));
        }



        static Dictionary<HumanBodyBones, HumanBodyBones> NextBoneDictinary = new()
        {
            {HumanBodyBones.Hips, HumanBodyBones.Spine},
            {HumanBodyBones.Spine, HumanBodyBones.Chest},
            {HumanBodyBones.Chest, HumanBodyBones.UpperChest},
            {HumanBodyBones.UpperChest, HumanBodyBones.Neck},
            {HumanBodyBones.Neck, HumanBodyBones.Head},
            {HumanBodyBones.Head, HumanBodyBones.Head},//

            {HumanBodyBones.LeftEye, HumanBodyBones.LeftEye},//
            {HumanBodyBones.RightEye, HumanBodyBones.RightEye},//
            {HumanBodyBones.Jaw, HumanBodyBones.Jaw},//
            
            {HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg},
            {HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot},
            {HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes},
            {HumanBodyBones.LeftToes, HumanBodyBones.LeftToes},//

            {HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg},
            {HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot},
            {HumanBodyBones.RightFoot, HumanBodyBones.RightToes},
            {HumanBodyBones.RightToes, HumanBodyBones.RightToes},//

            {HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm},
            {HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm},
            {HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand},
            //{HumanBodyBones.LeftHand, HumanBodyBones.LeftHand},//
            {HumanBodyBones.LeftHand, HumanBodyBones.LeftMiddleProximal},//中指につなげる

            {HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm},
            {HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm},
            {HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand},
            //{HumanBodyBones.RightHand, HumanBodyBones.RightHand},//
            {HumanBodyBones.RightHand, HumanBodyBones.RightMiddleProximal},//中指につなげる
            
            {HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftThumbIntermediate},
            {HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbDistal},
            {HumanBodyBones.LeftThumbDistal, HumanBodyBones.LeftThumbDistal},//

            {HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftIndexIntermediate},
            {HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexDistal},
            {HumanBodyBones.LeftIndexDistal, HumanBodyBones.LeftIndexDistal},//

            {HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate},
            {HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleDistal},
            {HumanBodyBones.LeftMiddleDistal, HumanBodyBones.LeftMiddleDistal},//

            {HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftRingIntermediate},
            {HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingDistal},
            {HumanBodyBones.LeftRingDistal, HumanBodyBones.LeftRingDistal},//

            {HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftLittleIntermediate},
            {HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleDistal},
            {HumanBodyBones.LeftLittleDistal, HumanBodyBones.LeftLittleDistal},//

            {HumanBodyBones.RightThumbProximal, HumanBodyBones.RightThumbIntermediate},
            {HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbDistal},
            {HumanBodyBones.RightThumbDistal, HumanBodyBones.RightThumbDistal},//

            {HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate},
            {HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal},
            {HumanBodyBones.RightIndexDistal, HumanBodyBones.RightIndexDistal},//

            {HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate},
            {HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal},
            {HumanBodyBones.RightMiddleDistal, HumanBodyBones.RightMiddleDistal},//

            {HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate},
            {HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal},
            {HumanBodyBones.RightRingDistal, HumanBodyBones.RightRingDistal},//

            {HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate},
            {HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal},
            {HumanBodyBones.RightLittleDistal, HumanBodyBones.RightLittleDistal},//
            
            //{HumanBodyBones.LastBone, HumanBodyBones.LastBone},//
        };


    }

    public static class a
    {


        /// <summary>
        /// ＶＭＤからモデルとの比率を割り出せないかと思ったが、手足のローカル位置はすべて 0,0,0 のようで、失敗
        /// 
        /// この関数は正しく機能しない。
        /// </summary>
        public static float CalculateBodyLengthAverage(this Animator anim, Dictionary<VmdBoneName, VmdBodyMotionKey[]> vmddata)
        {
            var human = new[]
            {
                HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.LeftFoot,
                HumanBodyBones.LeftLowerArm,
                HumanBodyBones.LeftHand,
            };
            var vmd = new[]
            {
                "左ひざ",
                "左足首",
                "左ひじ",
                "左手首",
            };

            //var q =
            //    from x in human
            //    join y in VmdBone.HumanToMmdBonePrimaryIdList
            //        on x equals y.human
            //    select getfromhuman_(x) / getfromvmd_(y.mmd)
            //    ;
            return Enumerable
                .Zip(human, vmd, (h, v) => getfromhuman_(h) / getfromvmd_(v))
                .Do(x => Debug.Log(x))
                .Average();

            float getfromhuman_(HumanBodyBones i)
            {
                Debug.Log($"human : {i} {anim.GetBoneTransform(i).localPosition}");
                return math.length(anim.GetBoneTransform(i).localPosition);
            }
            //float getfromvmd_(MmdBodyBones i)
            //{
            //    return math.length(streams.GetKeyDirect((int)i, 0.0f).value.As3());
            //}
            float getfromvmd_(VmdBoneName name)
            {
                Debug.Log($"vmd : {name.name} {vmddata[name].First().time} {vmddata[name].First().pos} {vmddata[name].First().rot}");
                return math.length(vmddata[name].First().pos.As3());
            }
        }

    }
}

