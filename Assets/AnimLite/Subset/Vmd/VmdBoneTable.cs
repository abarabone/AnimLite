using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace AnimLite.Vmd
{
    public enum MmdBodyBones
    {
        全ての親,
        センター,
        グルーブ,
        下半身,
        左足ＩＫ,
        右足ＩＫ,
        左つま先ＩＫ,
        右つま先ＩＫ,
        上半身,
        //下半身2,
        上半身2,
        首,
        頭,
        左肩P,
        左肩,
        //左肩2,//暫定追加
        左肩C,
        左腕,
        左腕捩,
        左腕捩1,
        左腕捩2,
        左腕捩3,
        左ひじ,
        左手捩,
        左手捩1,
        左手捩2,
        左手捩3,
        左手首,
        左親指０,
        左親指１,
        左親指２,
        左人指１,
        左人指２,
        左人指３,
        左中指１,
        左中指２,
        左中指３,
        左薬指１,
        左薬指２,
        左薬指３,
        左小指１,
        左小指２,
        左小指３,
        右肩P,
        右肩,
        //右肩2,//暫定追加
        右肩C,
        右腕,
        右腕捩,
        右腕捩1,
        右腕捩2,
        右腕捩3,
        右ひじ,
        右手捩,
        右手捩1,
        右手捩2,
        右手捩3,
        右手首,
        右親指０,
        右親指１,
        右親指２,
        右人指１,
        右人指２,
        右人指３,
        右中指１,
        右中指２,
        右中指３,
        右薬指１,
        右薬指２,
        右薬指３,
        右小指１,
        右小指２,
        右小指３,
        左足,
        左ひざ,
        左足首,
        左つま先,
        右足,
        右ひざ,
        右足首,
        右つま先,
        length,
        nobone,
    };

    public static class VmdBone
    {

        public static (HumanBodyBones human, MmdBodyBones mmd)[] HumanToMmdBonePrimaryIdList = new[]
        {
            //(HumanBodyBones.Root,                    MmdBodyBones.全ての親),

            //(HumanBodyBones.Hips,                       MmdBodyBones.センター),
            //(HumanBodyBones.Hips,                       MmdBodyBones.グルーブ),
            (HumanBodyBones.Hips,                       MmdBodyBones.下半身),
            //(HumanBodyBones.Hips,                       MmdBodyBones.下半身2),
            //(HumanBodyBones.Spine,                      MmdBodyBones.下半身),
            (HumanBodyBones.Spine,                      MmdBodyBones.上半身),
            (HumanBodyBones.Chest,                      MmdBodyBones.上半身2),
            ////(HumanBodyBones.UpperChest),               MmdBodyBones.なさそう        // opt
            (HumanBodyBones.Neck,                       MmdBodyBones.首),
            (HumanBodyBones.Head,                       MmdBodyBones.頭),
            ////(HumanBodyBones.Jaw,                        MmdBodyBones.なさそう
            //(HumanBodyBones.LeftFoot,                   MmdBodyBones.左足ＩＫ),
            //(HumanBodyBones.LeftUpperLeg,               MmdBodyBones.下半身),
            //(HumanBodyBones.LeftUpperLeg,               MmdBodyBones.下半身2),
            (HumanBodyBones.LeftUpperLeg,               MmdBodyBones.左足),
            (HumanBodyBones.LeftLowerLeg,               MmdBodyBones.左ひざ),
            (HumanBodyBones.LeftFoot,                   MmdBodyBones.左足首),
            (HumanBodyBones.LeftToes,                   MmdBodyBones.左つま先),        // opt
            //(HumanBodyBones.RightFoot,                  MmdBodyBones.右足ＩＫ),
            //(HumanBodyBones.RightUpperLeg,              MmdBodyBones.下半身),
            //(HumanBodyBones.RightUpperLeg,              MmdBodyBones.下半身2),
            (HumanBodyBones.RightUpperLeg,              MmdBodyBones.右足),
            (HumanBodyBones.RightLowerLeg,              MmdBodyBones.右ひざ),
            (HumanBodyBones.RightFoot,                  MmdBodyBones.右足首),
            (HumanBodyBones.RightToes,                  MmdBodyBones.右つま先),        // opt
            //(HumanBodyBones.LeftShoulder,               MmdBodyBones.左肩P),
            (HumanBodyBones.LeftShoulder,               MmdBodyBones.左肩),          // opt
            //(HumanBodyBones.LeftShoulder,               MmdBodyBones.左肩C),
            //(HumanBodyBones.RightShoulder,              MmdBodyBones.右肩P),
            (HumanBodyBones.RightShoulder,              MmdBodyBones.右肩),          // opt
            //(HumanBodyBones.RightShoulder,              MmdBodyBones.右肩C),
            (HumanBodyBones.LeftUpperArm,               MmdBodyBones.左腕),
            //(HumanBodyBones.LeftLowerArm,               MmdBodyBones.左腕捩),
            //(HumanBodyBones.LeftLowerArm,               MmdBodyBones.左腕捩1),
            //(HumanBodyBones.LeftLowerArm,               MmdBodyBones.左腕捩2),
            //(HumanBodyBones.LeftLowerArm,               MmdBodyBones.左腕捩3),
            (HumanBodyBones.RightUpperArm,              MmdBodyBones.右腕),
            //(HumanBodyBones.RightLowerArm,              MmdBodyBones.右腕捩),
            //(HumanBodyBones.RightLowerArm,              MmdBodyBones.右腕捩1),
            //(HumanBodyBones.RightLowerArm,              MmdBodyBones.右腕捩2),
            //(HumanBodyBones.RightLowerArm,              MmdBodyBones.右腕捩3),
            (HumanBodyBones.LeftLowerArm,               MmdBodyBones.左ひじ),
            //(HumanBodyBones.LeftLowerArm,               MmdBodyBones.左手捩),
            (HumanBodyBones.LeftHand,                   MmdBodyBones.左手首),
            (HumanBodyBones.RightLowerArm,              MmdBodyBones.右ひじ),
            //(HumanBodyBones.RightLowerArm,              MmdBodyBones.右手捩),
            (HumanBodyBones.RightHand,                  MmdBodyBones.右手首),
            (HumanBodyBones.LeftThumbProximal,          MmdBodyBones.左親指０),
            (HumanBodyBones.LeftThumbIntermediate,      MmdBodyBones.左親指１),
            (HumanBodyBones.LeftThumbDistal,            MmdBodyBones.左親指２),
            (HumanBodyBones.LeftIndexProximal,          MmdBodyBones.左人指１),
            (HumanBodyBones.LeftIndexIntermediate,      MmdBodyBones.左人指２),
            (HumanBodyBones.LeftIndexDistal,            MmdBodyBones.左人指３),
            (HumanBodyBones.LeftMiddleProximal,         MmdBodyBones.左中指１),
            (HumanBodyBones.LeftMiddleIntermediate,     MmdBodyBones.左中指２),
            (HumanBodyBones.LeftMiddleDistal,           MmdBodyBones.左中指３),
            (HumanBodyBones.LeftRingProximal,           MmdBodyBones.左薬指１),
            (HumanBodyBones.LeftRingIntermediate,       MmdBodyBones.左薬指２),
            (HumanBodyBones.LeftRingDistal,             MmdBodyBones.左薬指３),
            (HumanBodyBones.LeftLittleProximal,         MmdBodyBones.左小指１),
            (HumanBodyBones.LeftLittleIntermediate,     MmdBodyBones.左小指２),
            (HumanBodyBones.LeftLittleDistal,           MmdBodyBones.左小指３),
            (HumanBodyBones.RightThumbProximal,         MmdBodyBones.右親指０),
            (HumanBodyBones.RightThumbIntermediate,     MmdBodyBones.右親指１),
            (HumanBodyBones.RightThumbDistal,           MmdBodyBones.右親指２),
            (HumanBodyBones.RightIndexProximal,         MmdBodyBones.右人指１),
            (HumanBodyBones.RightIndexIntermediate,     MmdBodyBones.右人指２),
            (HumanBodyBones.RightIndexDistal,           MmdBodyBones.右人指３),
            (HumanBodyBones.RightMiddleProximal,        MmdBodyBones.右中指１),
            (HumanBodyBones.RightMiddleIntermediate,    MmdBodyBones.右中指２),
            (HumanBodyBones.RightMiddleDistal,          MmdBodyBones.右中指３),
            (HumanBodyBones.RightRingProximal,          MmdBodyBones.右薬指１),
            (HumanBodyBones.RightRingIntermediate,      MmdBodyBones.右薬指２),
            (HumanBodyBones.RightRingDistal,            MmdBodyBones.右薬指３),
            (HumanBodyBones.RightLittleProximal,        MmdBodyBones.右小指１),
            (HumanBodyBones.RightLittleIntermediate,    MmdBodyBones.右小指２),
            (HumanBodyBones.RightLittleDistal,          MmdBodyBones.右小指３),
        };



        public static Dictionary<VmdBoneName, MmdBodyBones> MmdBoneNameToId = new()
        {
            {"全ての親",        MmdBodyBones.全ての親 },
            {"センター",        MmdBodyBones.センター},
            {"グルーブ",        MmdBodyBones.グルーブ},
            {"下半身",          MmdBodyBones.下半身},
        //  {"下半身2",         MmdBodyBones.下半身2},
            {"上半身",          MmdBodyBones.上半身},
            {"上半身2",         MmdBodyBones.上半身2},
            {"上半身２",        MmdBodyBones.上半身2},
            {"首",              MmdBodyBones.首},
            {"頭",              MmdBodyBones.頭},
            {"左肩P",           MmdBodyBones.左肩P},
            {"左肩Ｐ",          MmdBodyBones.左肩P},
            {"左肩",            MmdBodyBones.左肩},
            //{"左肩2",            MmdBodyBones.左肩2},   // 
            {"左肩C",           MmdBodyBones.左肩C},
            {"左肩Ｃ",          MmdBodyBones.左肩C},
            {"左腕",            MmdBodyBones.左腕},
            {"左腕捩",          MmdBodyBones.左腕捩},
            {"左腕捩1",         MmdBodyBones.左腕捩1},
            {"左腕捩2",         MmdBodyBones.左腕捩2},
            {"左腕捩3",         MmdBodyBones.左腕捩3},
            {"左腕捩１",        MmdBodyBones.左腕捩1},
            {"左腕捩２",        MmdBodyBones.左腕捩2},
            {"左腕捩３",        MmdBodyBones.左腕捩3},
            {"左腕捩り",          MmdBodyBones.左腕捩},
            {"左腕捩り1",         MmdBodyBones.左腕捩1},
            {"左腕捩り2",         MmdBodyBones.左腕捩2},
            {"左腕捩り3",         MmdBodyBones.左腕捩3},
            {"左腕捩り１",        MmdBodyBones.左腕捩1},
            {"左腕捩り２",        MmdBodyBones.左腕捩2},
            {"左腕捩り３",        MmdBodyBones.左腕捩3},
            {"左ひじ",          MmdBodyBones.左ひじ},
            {"左肘",          MmdBodyBones.左ひじ},
            {"左手捩",          MmdBodyBones.左手捩},
            {"左手捩1",         MmdBodyBones.左手捩1},
            {"左手捩2",         MmdBodyBones.左手捩2},
            {"左手捩3",         MmdBodyBones.左手捩3},
            {"左手捩１",        MmdBodyBones.左手捩1},
            {"左手捩２",        MmdBodyBones.左手捩2},
            {"左手捩３",        MmdBodyBones.左手捩3},
            {"左手捩り",          MmdBodyBones.左手捩},
            {"左手捩り1",         MmdBodyBones.左手捩1},
            {"左手捩り2",         MmdBodyBones.左手捩2},
            {"左手捩り3",         MmdBodyBones.左手捩3},
            {"左手捩り１",        MmdBodyBones.左手捩1},
            {"左手捩り２",        MmdBodyBones.左手捩2},
            {"左手捩り３",        MmdBodyBones.左手捩3},
            {"左手首",          MmdBodyBones.左手首},
            {"左親指0",         MmdBodyBones.左親指０},
            {"左親指1",         MmdBodyBones.左親指１},
            {"左親指2",         MmdBodyBones.左親指２},
            {"左人指1",         MmdBodyBones.左人指１},
            {"左人指2",         MmdBodyBones.左人指２},
            {"左人指3",         MmdBodyBones.左人指３},
            {"左中指1",         MmdBodyBones.左中指１},
            {"左中指2",         MmdBodyBones.左中指２},
            {"左中指3",         MmdBodyBones.左中指３},
            {"左薬指1",         MmdBodyBones.左薬指１},
            {"左薬指2",         MmdBodyBones.左薬指２},
            {"左薬指3",         MmdBodyBones.左薬指３},
            {"左小指1",         MmdBodyBones.左小指１},
            {"左小指2",         MmdBodyBones.左小指２},
            {"左小指3",         MmdBodyBones.左小指３},
            {"左親指０",        MmdBodyBones.左親指０},
            {"左親指１",        MmdBodyBones.左親指１},
            {"左親指２",        MmdBodyBones.左親指２},
            {"左人指１",        MmdBodyBones.左人指１},
            {"左人指２",        MmdBodyBones.左人指２},
            {"左人指３",        MmdBodyBones.左人指３},
            {"左中指１",        MmdBodyBones.左中指１},
            {"左中指２",        MmdBodyBones.左中指２},
            {"左中指３",        MmdBodyBones.左中指３},
            {"左薬指１",        MmdBodyBones.左薬指１},
            {"左薬指２",        MmdBodyBones.左薬指２},
            {"左薬指３",        MmdBodyBones.左薬指３},
            {"左小指１",        MmdBodyBones.左小指１},
            {"左小指２",        MmdBodyBones.左小指２},
            {"左小指３",        MmdBodyBones.左小指３},
            {"右肩P",           MmdBodyBones.右肩P},
            {"右肩Ｐ",          MmdBodyBones.右肩P},
            {"右肩",            MmdBodyBones.右肩},
            //{"右肩2",            MmdBodyBones.右肩2},   // 
            {"右肩C",           MmdBodyBones.右肩C},
            {"右肩Ｃ",          MmdBodyBones.右肩C},
            {"右腕",            MmdBodyBones.右腕},
            {"右腕捩",          MmdBodyBones.右腕捩},
            {"右腕捩1",         MmdBodyBones.右腕捩1},
            {"右腕捩2",         MmdBodyBones.右腕捩2},
            {"右腕捩3",         MmdBodyBones.右腕捩3},
            {"右腕捩１",        MmdBodyBones.右腕捩1},
            {"右腕捩２",        MmdBodyBones.右腕捩2},
            {"右腕捩３",        MmdBodyBones.右腕捩3},
            {"右腕捩り",          MmdBodyBones.右腕捩},
            {"右腕捩り1",         MmdBodyBones.右腕捩1},
            {"右腕捩り2",         MmdBodyBones.右腕捩2},
            {"右腕捩り3",         MmdBodyBones.右腕捩3},
            {"右腕捩り１",        MmdBodyBones.右腕捩1},
            {"右腕捩り２",        MmdBodyBones.右腕捩2},
            {"右腕捩り３",        MmdBodyBones.右腕捩3},
            {"右ひじ",          MmdBodyBones.右ひじ},
            {"右肘",          MmdBodyBones.右ひじ},
            {"右手捩",          MmdBodyBones.右手捩},
            {"右手捩1",         MmdBodyBones.右手捩1},
            {"右手捩2",         MmdBodyBones.右手捩2},
            {"右手捩3",         MmdBodyBones.右手捩3},
            {"右手捩１",        MmdBodyBones.右手捩1},
            {"右手捩２",        MmdBodyBones.右手捩2},
            {"右手捩３",        MmdBodyBones.右手捩3},
            {"右手捩り",          MmdBodyBones.右手捩},
            {"右手捩り1",         MmdBodyBones.右手捩1},
            {"右手捩り2",         MmdBodyBones.右手捩2},
            {"右手捩り3",         MmdBodyBones.右手捩3},
            {"右手捩り１",        MmdBodyBones.右手捩1},
            {"右手捩り２",        MmdBodyBones.右手捩2},
            {"右手捩り３",        MmdBodyBones.右手捩3},
            {"右手首",          MmdBodyBones.右手首},
            {"右親指０",        MmdBodyBones.右親指０},
            {"右親指１",        MmdBodyBones.右親指１},
            {"右親指２",        MmdBodyBones.右親指２},
            {"右人指１",        MmdBodyBones.右人指１},
            {"右人指２",        MmdBodyBones.右人指２},
            {"右人指３",        MmdBodyBones.右人指３},
            {"右中指１",        MmdBodyBones.右中指１},
            {"右中指２",        MmdBodyBones.右中指２},
            {"右中指３",        MmdBodyBones.右中指３},
            {"右薬指１",        MmdBodyBones.右薬指１},
            {"右薬指２",        MmdBodyBones.右薬指２},
            {"右薬指３",        MmdBodyBones.右薬指３},
            {"右小指１",        MmdBodyBones.右小指１},
            {"右小指２",        MmdBodyBones.右小指２},
            {"右小指３",        MmdBodyBones.右小指３},
            {"右親指0",         MmdBodyBones.右親指０},
            {"右親指1",         MmdBodyBones.右親指１},
            {"右親指2",         MmdBodyBones.右親指２},
            {"右人指1",         MmdBodyBones.右人指１},
            {"右人指2",         MmdBodyBones.右人指２},
            {"右人指3",         MmdBodyBones.右人指３},
            {"右中指1",         MmdBodyBones.右中指１},
            {"右中指2",         MmdBodyBones.右中指２},
            {"右中指3",         MmdBodyBones.右中指３},
            {"右薬指1",         MmdBodyBones.右薬指１},
            {"右薬指2",         MmdBodyBones.右薬指２},
            {"右薬指3",         MmdBodyBones.右薬指３},
            {"右小指1",         MmdBodyBones.右小指１},
            {"右小指2",         MmdBodyBones.右小指２},
            {"右小指3",         MmdBodyBones.右小指３},
            {"左足",            MmdBodyBones.左足},
            {"左ひざ",          MmdBodyBones.左ひざ},
            {"左足首",          MmdBodyBones.左足首},
            {"左つま先",        MmdBodyBones.左つま先},
            {"左爪先",         MmdBodyBones.左つま先},
            {"右足",            MmdBodyBones.右足},
            {"右ひざ",          MmdBodyBones.右ひざ},
            {"右足首",          MmdBodyBones.右足首},
            {"右つま先",        MmdBodyBones.右つま先},
            {"右爪先",        MmdBodyBones.右つま先},
            {"左足IK",          MmdBodyBones.左足ＩＫ},
            {"右足IK",          MmdBodyBones.右足ＩＫ},
            {"左つま先IK",      MmdBodyBones.左つま先ＩＫ},
            {"右つま先IK",      MmdBodyBones.右つま先ＩＫ},
            {"左爪先IK",      MmdBodyBones.左つま先ＩＫ},
            {"右爪先IK",      MmdBodyBones.右つま先ＩＫ},
            {"左足ＩＫ",        MmdBodyBones.左足ＩＫ},
            {"右足ＩＫ",        MmdBodyBones.右足ＩＫ},
            {"左つま先ＩＫ",    MmdBodyBones.左つま先ＩＫ},
            {"右つま先ＩＫ",    MmdBodyBones.右つま先ＩＫ},
            {"左爪先ＩＫ",    MmdBodyBones.左つま先ＩＫ},
            {"右爪先ＩＫ",    MmdBodyBones.右つま先ＩＫ},
        };



        //public static HumanBodyBones GetParentBone(this Animator anim, HumanBodyBones thisbone)
        //{
        //    var parentbone = HumanBoneToParentDict[thisbone];
        //    if (parentbone == HumanBodyBones.LastBone)
        //    {
        //        return parentbone;
        //    }
        //    //Debug.Log(parentbone);
        //    var tfParent = anim.GetBoneTransform(parentbone);
        //    if (tfParent is not null)
        //    {
        //        return parentbone;
        //    }

        //    return anim.GetParentBone(parentbone);
        //}
        //public static Transform GetParent(this Animator anim, HumanBodyBones thisbone)
        //{
        //    var parentbone = HumanBoneToParentDict[thisbone];
        //    if (parentbone == HumanBodyBones.LastBone)
        //    {
        //        return anim.GetBoneTransform(HumanBodyBones.Hips).parent;
        //    }

        //    var tfParent = anim.GetBoneTransform(parentbone);
        //    if (tfParent is not null) return tfParent;

        //    return anim.GetParent(parentbone);
        //}

        public static Dictionary<HumanBodyBones, HumanBodyBones> HumanBoneToParentDict = new()
        {
            {HumanBodyBones.Hips,                        HumanBodyBones.LastBone},
            {HumanBodyBones.Spine,                       HumanBodyBones.Hips},
            {HumanBodyBones.Chest,                       HumanBodyBones.Spine},
            {HumanBodyBones.UpperChest,                  HumanBodyBones.Chest},
            {HumanBodyBones.Neck,                        HumanBodyBones.UpperChest},
            {HumanBodyBones.Head,                        HumanBodyBones.Neck},
            {HumanBodyBones.Jaw,                         HumanBodyBones.Head},

            {HumanBodyBones.LeftUpperLeg,                HumanBodyBones.Hips},
            {HumanBodyBones.LeftLowerLeg,                HumanBodyBones.LeftUpperLeg},
            {HumanBodyBones.LeftFoot,                    HumanBodyBones.LeftLowerLeg},
            {HumanBodyBones.LeftToes,                    HumanBodyBones.LeftFoot},

            {HumanBodyBones.RightUpperLeg,               HumanBodyBones.Hips},
            {HumanBodyBones.RightLowerLeg,               HumanBodyBones.RightUpperLeg},
            {HumanBodyBones.RightFoot,                   HumanBodyBones.RightLowerLeg},
            {HumanBodyBones.RightToes,                   HumanBodyBones.RightFoot},

            {HumanBodyBones.LeftShoulder,                HumanBodyBones.UpperChest},
            {HumanBodyBones.LeftUpperArm,                HumanBodyBones.LeftShoulder},
            {HumanBodyBones.LeftLowerArm,                HumanBodyBones.LeftUpperArm},
            {HumanBodyBones.LeftHand,                    HumanBodyBones.LeftLowerArm},

            {HumanBodyBones.LeftThumbProximal,           HumanBodyBones.LeftHand},
            {HumanBodyBones.LeftThumbIntermediate,       HumanBodyBones.LeftThumbProximal},
            {HumanBodyBones.LeftThumbDistal,             HumanBodyBones.LeftThumbIntermediate},

            {HumanBodyBones.LeftIndexProximal,           HumanBodyBones.LeftHand},
            {HumanBodyBones.LeftIndexIntermediate,       HumanBodyBones.LeftIndexProximal},
            {HumanBodyBones.LeftIndexDistal,             HumanBodyBones.LeftIndexIntermediate},

            {HumanBodyBones.LeftMiddleProximal,          HumanBodyBones.LeftHand},
            {HumanBodyBones.LeftMiddleIntermediate,      HumanBodyBones.LeftMiddleProximal},
            {HumanBodyBones.LeftMiddleDistal,            HumanBodyBones.LeftMiddleIntermediate},

            {HumanBodyBones.LeftRingProximal,            HumanBodyBones.LeftHand},
            {HumanBodyBones.LeftRingIntermediate,        HumanBodyBones.LeftRingProximal},
            {HumanBodyBones.LeftRingDistal,              HumanBodyBones.LeftRingIntermediate},

            {HumanBodyBones.LeftLittleProximal,          HumanBodyBones.LeftHand},
            {HumanBodyBones.LeftLittleIntermediate,      HumanBodyBones.LeftLittleProximal},
            {HumanBodyBones.LeftLittleDistal,            HumanBodyBones.LeftLittleIntermediate},

            {HumanBodyBones.RightShoulder,               HumanBodyBones.UpperChest},
            {HumanBodyBones.RightUpperArm,               HumanBodyBones.RightShoulder},
            {HumanBodyBones.RightLowerArm,               HumanBodyBones.RightUpperArm},
            {HumanBodyBones.RightHand,                   HumanBodyBones.RightLowerArm},

            {HumanBodyBones.RightThumbProximal,          HumanBodyBones.RightHand},
            {HumanBodyBones.RightThumbIntermediate,      HumanBodyBones.RightThumbProximal},
            {HumanBodyBones.RightThumbDistal,            HumanBodyBones.RightThumbIntermediate},

            {HumanBodyBones.RightIndexProximal,          HumanBodyBones.RightHand},
            {HumanBodyBones.RightIndexIntermediate,      HumanBodyBones.RightIndexProximal},
            {HumanBodyBones.RightIndexDistal,            HumanBodyBones.RightIndexIntermediate},

            {HumanBodyBones.RightMiddleProximal,         HumanBodyBones.RightHand},
            {HumanBodyBones.RightMiddleIntermediate,     HumanBodyBones.RightMiddleProximal},
            {HumanBodyBones.RightMiddleDistal,           HumanBodyBones.RightMiddleIntermediate},

            {HumanBodyBones.RightRingProximal,           HumanBodyBones.RightHand},
            {HumanBodyBones.RightRingIntermediate,       HumanBodyBones.RightRingProximal},
            {HumanBodyBones.RightRingDistal,             HumanBodyBones.RightRingIntermediate},

            {HumanBodyBones.RightLittleProximal,         HumanBodyBones.RightHand},
            {HumanBodyBones.RightLittleIntermediate,     HumanBodyBones.RightLittleProximal},
            {HumanBodyBones.RightLittleDistal,           HumanBodyBones.RightLittleIntermediate},
        };

        public static Dictionary<HumanBodyBones, quaternion> HumanBodyToAdjustRotation2 = new()
        {
            {HumanBodyBones.LeftUpperArm,               rotz_(+30)},

            {HumanBodyBones.RightUpperArm,              rotz_(-30)},
        };

        static quaternion rotx_(float deg) => quaternion.RotateX(math.radians(deg));
        static quaternion roty_(float deg) => quaternion.RotateY(math.radians(deg));
        static quaternion rotz_(float deg) => quaternion.RotateZ(math.radians(deg));




        ///// <summary>
        ///// 存在しないものは次のＴＦが明確ではないもの
        ///// </summary>
        //public static Dictionary<HumanBodyBones, (HumanBodyBones primary, HumanBodyBones secondary, Vector3 forward)> ParentToChildDictionary = new()
        //{
        //    {HumanBodyBones.Hips, (HumanBodyBones.Spine, HumanBodyBones.Spine, Vector3.up)},
        //    {HumanBodyBones.Spine, (HumanBodyBones.Chest, HumanBodyBones.Chest, Vector3.up)},
        //    {HumanBodyBones.Chest, (HumanBodyBones.UpperChest, HumanBodyBones.Neck, Vector3.up)},
        //    {HumanBodyBones.UpperChest, (HumanBodyBones.Neck, HumanBodyBones.Neck, Vector3.up)},
        //    {HumanBodyBones.Neck, (HumanBodyBones.Head, HumanBodyBones.Head, Vector3.up)},
        //    //{HumanBodyBones.Head, (HumanBodyBones, HumanBodyBones, Vector3)},


        //    {HumanBodyBones.LeftShoulder, (HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftUpperArm, Vector3.left)},
        //    {HumanBodyBones.LeftUpperArm, (HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftLowerArm, Vector3.left)},
        //    {HumanBodyBones.LeftLowerArm, (HumanBodyBones.LeftHand, HumanBodyBones.LeftHand, Vector3.left)},
        //    //{HumanBodyBones.LeftHand, (HumanBodyBones, HumanBodyBones, Vector3)},

        //    {HumanBodyBones.LeftThumbProximal, (HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbIntermediate, Vector3.left)},
        //    {HumanBodyBones.LeftThumbIntermediate, (HumanBodyBones.LeftThumbDistal, HumanBodyBones.LeftThumbDistal, Vector3.left)},
        //    //{HumanBodyBones.LeftThumbDistal, (HumanBodyBones, HumanBodyBones, Vector3)},
        //    {HumanBodyBones.LeftIndexProximal, (HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexIntermediate, Vector3.left)},
        //    {HumanBodyBones.LeftIndexIntermediate, (HumanBodyBones.LeftIndexDistal, HumanBodyBones.LeftIndexDistal, Vector3.left)},
        //    //{HumanBodyBones.LeftIndexDistal, (HumanBodyBones, HumanBodyBones, Vector3)},
        //    {HumanBodyBones.LeftMiddleProximal, (HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleIntermediate, Vector3.left)},
        //    {HumanBodyBones.LeftMiddleIntermediate, (HumanBodyBones.LeftMiddleDistal, HumanBodyBones.LeftMiddleDistal, Vector3.left)},
        //    //{HumanBodyBones.LeftMiddleDistal, (HumanBodyBones, HumanBodyBones, Vector3)},
        //    {HumanBodyBones.LeftRingProximal, (HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingIntermediate, Vector3.left)},
        //    {HumanBodyBones.LeftRingIntermediate, (HumanBodyBones.LeftRingDistal, HumanBodyBones.LeftRingDistal, Vector3.left)},
        //    //{HumanBodyBones.LeftRingDistal, (HumanBodyBones, HumanBodyBones, Vector3)},
        //    {HumanBodyBones.LeftLittleProximal, (HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftIndexIntermediate, Vector3.left)},
        //    {HumanBodyBones.LeftLittleIntermediate, (HumanBodyBones.LeftLittleDistal, HumanBodyBones.LeftLittleDistal, Vector3.left)},
        //    //{HumanBodyBones.LeftLittleDistal, (HumanBodyBones, HumanBodyBones, Vector3)},

        //    {HumanBodyBones.LeftUpperLeg, (HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftLowerLeg, Vector3.down)},
        //    {HumanBodyBones.LeftLowerLeg, (HumanBodyBones.LeftFoot, HumanBodyBones.LeftFoot, Vector3.down)},
        //    {HumanBodyBones.LeftFoot, (HumanBodyBones.LeftToes, HumanBodyBones.LeftToes, Vector3.forward)},
        //    //{HumanBodyBones.LeftToes, (HumanBodyBones, HumanBodyBones, Vector3)},
            

        //    {HumanBodyBones.RightShoulder, (HumanBodyBones.RightUpperArm, HumanBodyBones.RightUpperArm, Vector3.right)},
        //    {HumanBodyBones.RightUpperArm, (HumanBodyBones.RightLowerArm, HumanBodyBones.RightLowerArm, Vector3.right)},
        //    {HumanBodyBones.RightLowerArm, (HumanBodyBones.RightHand, HumanBodyBones.RightHand, Vector3.right)},
        //    //{HumanBodyBones.RightHand, (HumanBodyBones, HumanBodyBones, Vector3)},

        //    {HumanBodyBones.RightThumbProximal, (HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbIntermediate, Vector3.right)},
        //    {HumanBodyBones.RightThumbIntermediate, (HumanBodyBones.RightThumbDistal, HumanBodyBones.RightThumbDistal, Vector3.right)},
        //    //{HumanBodyBones.RightThumbDistal, (HumanBodyBones, HumanBodyBones, Vector3)},
        //    {HumanBodyBones.RightIndexProximal, (HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexIntermediate, Vector3.right)},
        //    {HumanBodyBones.RightIndexIntermediate, (HumanBodyBones.RightIndexDistal, HumanBodyBones.RightIndexDistal, Vector3.right)},
        //    //{HumanBodyBones.RightIndexDistal, (HumanBodyBones, HumanBodyBones, Vector3)},
        //    {HumanBodyBones.RightMiddleProximal, (HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleIntermediate, Vector3.right)},
        //    {HumanBodyBones.RightMiddleIntermediate, (HumanBodyBones.RightMiddleDistal, HumanBodyBones.RightMiddleDistal, Vector3.right)},
        //    //{HumanBodyBones.RightMiddleDistal, (HumanBodyBones, HumanBodyBones, Vector3)},
        //    {HumanBodyBones.RightRingProximal, (HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingIntermediate, Vector3.right)},
        //    {HumanBodyBones.RightRingIntermediate, (HumanBodyBones.RightRingDistal, HumanBodyBones.RightRingDistal, Vector3.right)},
        //    //{HumanBodyBones.RightRingDistal, (HumanBodyBones, HumanBodyBones, Vector3)},
        //    {HumanBodyBones.RightLittleProximal, (HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightIndexIntermediate, Vector3.right)},
        //    {HumanBodyBones.RightLittleIntermediate, (HumanBodyBones.RightLittleDistal, HumanBodyBones.RightLittleDistal, Vector3.right)},
        //    //{HumanBodyBones.RightLittleDistal, (HumanBodyBones, HumanBodyBones, Vector3)},

        //    {HumanBodyBones.RightUpperLeg, (HumanBodyBones.RightLowerLeg, HumanBodyBones.RightLowerLeg, Vector3.down)},
        //    {HumanBodyBones.RightLowerLeg, (HumanBodyBones.RightFoot, HumanBodyBones.RightFoot, Vector3.down)},
        //    {HumanBodyBones.RightFoot, (HumanBodyBones.RightToes, HumanBodyBones.RightToes, Vector3.forward)},
        //    //{HumanBodyBones.RightToes, (HumanBodyBones, HumanBodyBones, Vector3)},
        //};
    }
}
