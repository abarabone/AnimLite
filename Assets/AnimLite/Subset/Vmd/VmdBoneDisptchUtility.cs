﻿using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace AnimLite.Vmd
{
    using AnimLite;
    using AnimLite.Utility;
    using System.Numerics;


    /// <summary>
    /// 
    /// </summary>
    public struct VmdHumanBoneReference<TTf>
        where TTf : ITransformProxy
    {
        public TTf TransformHandle;

        public HumanBodyBones HumanBoneId;
        public MmdBodyBones StreamId;

        public static implicit operator VmdHumanBoneReference<TTf>(HumanBoneReference<TTf> src) =>
            new VmdHumanBoneReference<TTf>
            {
                TransformHandle = src.TransformHandle,
                HumanBoneId = src.HumanBoneId,
                StreamId = (MmdBodyBones)src.StreamId,
            };
    }




    public static class VmdBoneDispatchExtension
    {


        //public static (quaternion rot, float3 pos) get<TRFinder, TPFinder>(this (TRFinder r, TPFinder p) kf, MmdBodyBones ibone)
        //    where TRFinder : IKeyFinder<quaternion>
        //    where TPFinder : IKeyFinder<float4>
        //=>
        //    (kf.r.getrot(ibone), kf.p.getpos(ibone));

        //public static (quaternion rot, float3 pos) accumulate<TRFinder, TPFinder>(this (TRFinder r, TPFinder p) kf, MmdBodyBones ibone0, MmdBodyBones ibone1)
        //    where TRFinder : IKeyFinder<quaternion>
        //    where TPFinder : IKeyFinder<float4>
        //{
        //    var bone0 = kf.get(ibone0);
        //    var bone1 = kf.get(ibone1);

        //    var pos0 = bone0.pos;
        //    var rot0 = bone0.rot;
        //    var pos1 = pos0 + math.rotate(rot0, bone1.pos);
        //    var rot1 = accumulate(rot0, bone1.rot);

        //    return (rot1, pos1);
        //}
        //public static (quaternion rot, float3 pos) accumulate<TRFinder, TPFinder>(this (TRFinder r, TPFinder p) kf, MmdBodyBones ibone0, MmdBodyBones ibone1, MmdBodyBones ibone2)
        //    where TRFinder : IKeyFinder<quaternion>
        //    where TPFinder : IKeyFinder<float4>
        //{
        //    var bone0 = kf.get(ibone0);
        //    var bone1 = kf.get(ibone1);
        //    var bone2 = kf.get(ibone2);

        //    var pos0 = bone0.pos;
        //    var rot0 = bone0.rot;
        //    var pos1 = pos0 + math.rotate(rot0, bone1.pos);
        //    var rot1 = accumulate(rot0, bone1.rot);
        //    var pos2 = pos1 + math.rotate(rot1, bone2.pos);
        //    var rot2 = accumulate(rot1, bone2.rot);

        //    return (rot2, pos2);
        //}

        //public static (quaternion rot, float3 pos) transform<TRFinder, TPFinder>(this (TRFinder r, TPFinder p) kf, (quaternion rot, float3 pos) bone0, MmdBodyBones ibone1)
        //    where TRFinder : IKeyFinder<quaternion>
        //    where TPFinder : IKeyFinder<float4>
        //{
        //    var bone1 = kf.get(ibone1);

        //    var pos0 = bone0.pos;
        //    var rot0 = bone0.rot;
        //    var pos1 = pos0 + math.rotate(rot0, bone1.pos);
        //    var rot1 = accumulate(rot0, bone1.rot);

        //    return (rot1, pos1);
        //}

        //public static (quaternion rot, float3 pos) aaaaa<TRFinder, TPFinder>(this (TRFinder r, TPFinder p) kf)
        //    where TRFinder : IKeyFinder<quaternion>
        //    where TPFinder : IKeyFinder<float4>
        //{
        //    var body = kf.transform(kf.transform(kf.transform(kf.get(
        //        MmdBodyBones.全ての親),
        //        MmdBodyBones.センター),
        //        MmdBodyBones.グルーブ),
        //        MmdBodyBones.下半身);
        //    var left = kf.transform(kf.transform(kf.transform(
        //        body,
        //        MmdBodyBones.左足),
        //        MmdBodyBones.左ひざ),
        //        MmdBodyBones.左足首);
        //    var right = kf.transform(kf.transform(kf.transform(
        //        body,
        //        MmdBodyBones.右足),
        //        MmdBodyBones.右ひざ),
        //        MmdBodyBones.右足首);

        //    return 
        //}



        public static VmdHumanBoneReference<TTf> ToVmd<TTf>(
            this HumanBoneReference<TTf> src)
            where TTf : ITransformProxy
            => src;


        //public static T get<T, TFinder>(this TFinder kf, MmdBodyBones ibone)
        //    where T : unmanaged
        //    where TFinder : IKeyFinder<T>
        //=>
        //    kf.get((int)ibone);

        public static quaternion getrot<TFinder>(this TFinder kf, MmdBodyBones ibone)
            where TFinder : IKeyFinder<quaternion>
        =>
            kf.get((int)ibone);

        public static float3 getpos<TFinder>(this TFinder kf, MmdBodyBones ibone)
            where TFinder : IKeyFinder<float4>
        =>
            kf.get((int)ibone).xyz;

        public static quaternion getrotIfOptout<TFinder>(
            this TFinder rkf, OptionalBoneChecker opt, HumanBodyBones humanbone, bool optionBoneIsNotExists)
            where TFinder : IKeyFinder<quaternion>
        {
            if (!optionBoneIsNotExists) return quaternion.identity;

            return rkf.AccumulateStreamRotation(opt, humanbone, MmdBodyBones.nobone);
        }




        /// <summary>
        /// 
        /// </summary>
        public static float3 AccumulateStreamPosition<TPFinder, TRFinder>(
            this TPFinder pkf, TRFinder rkf, HumanBodyBones humanbone)//, MmdBodyBones mmdbone)
            where TPFinder : IKeyFinder<float4>
            where TRFinder : IKeyFinder<quaternion>
        {
            (quaternion, float3) get_(MmdBodyBones ibone) => (rkf.getrot(ibone), pkf.getpos(ibone));

            return humanbone switch
            {

                HumanBodyBones.LastBone =>
                    //transform(
                    //    get_(MmdBodyBones.全ての親),
                    //    get_(MmdBodyBones.センター),
                    //    get_(MmdBodyBones.グルーブ)) * 0.1f,
                    //pkf.getpos(MmdBodyBones.全ての親).xyz * 0.1f,
                    get_root_pos_() * 0.1f,

                HumanBodyBones.Hips =>
                    //pkf.getpos(MmdBodyBones.下半身).xyz * 0.1f,
                    //transform(
                    //    get_(MmdBodyBones.センター),
                    //    get_(MmdBodyBones.グルーブ),
                    //    get_(MmdBodyBones.下半身)) * 0.1f,
                    get_hip_pos_() * 0.1f,

                HumanBodyBones.Spine =>
                    pkf.getpos(MmdBodyBones.上半身) * 0.1f,

                _ => default,
            };

            float3 get_root_pos_()
            {
                var pos = transform(
                    get_(MmdBodyBones.全ての親),
                    get_(MmdBodyBones.センター),
                    pkf.getpos(MmdBodyBones.グルーブ));

                return new float3(pos.x, 0.0f, pos.z);
            }

            float3 get_hip_pos_()
            {
                var rootpos = transform(
                    get_(MmdBodyBones.全ての親),
                    get_(MmdBodyBones.センター),
                    pkf.getpos(MmdBodyBones.グルーブ));
                
                var hippos = pkf.getpos(MmdBodyBones.下半身);

                return hippos + new float3(0.0f, rootpos.y, 0.0f);
            }
        }

        static float3 transform((quaternion rot, float3 pos) bone0, float3 bone1pos)
        {
            var pos0 = bone0.pos;
            var rot0 = bone0.rot;
            var pos1 = pos0 + math.rotate(rot0, bone1pos);

            return pos1;
        }
        static float3 transform((quaternion rot, float3 pos) bone0, (quaternion rot, float3 pos) bone1, float3 bone2pos)
        {
            var pos0 = bone0.pos;
            var rot0 = bone0.rot;
            var pos1 = pos0 + math.rotate(rot0, bone1.pos);
            var rot1 = accumulate(rot0, bone1.rot);
            var pos2 = pos1 + math.rotate(rot1, bone2pos);

            return pos2;
        }
        //static float3 transform((quaternion, float4) bone0, (quaternion, float4) bone1)
        //{
        //    var arr = new NativeArray<(quaternion, float4)>(2, Allocator.Temp);
        //    arr[0] = bone0;
        //    arr[1] = bone1;
        //    var pos = transform(arr);
        //    arr.Dispose();
        //    return pos;
        //}
        //static float3 transform((quaternion, float4) bone0, (quaternion, float4) bone1, (quaternion, float4) bone2)
        //{
        //    var arr = new NativeArray<(quaternion, float4)>(3, Allocator.Temp);
        //    arr[0] = bone0;
        //    arr[1] = bone1;
        //    arr[2] = bone2;
        //    var pos = transform(arr);
        //    arr.Dispose();
        //    return pos;
        ////}
        //static float3 transform(NativeSlice<(quaternion rot, float4 pos)> streams)
        //{
        //    var pos = streams[0].pos.xyz;

        //    for (var i = 1; i < streams.Length; i++)
        //    {
        //        var lrot = streams[i - 1].rot;
        //        var lpos = streams[i - 0].pos.xyz;

        //        pos += math.rotate(lrot, lpos);
        //    }

        //    return pos;
        //}



        static quaternion mul(quaternion r1) => r1;
        static quaternion mul(quaternion r1, quaternion r2) => math.mul(r1, r2);
        static quaternion mul(quaternion r1, quaternion r2, quaternion r3) => math.mul(math.mul(r1, r2), r3);
        static quaternion mul(quaternion r1, quaternion r2, quaternion r3, quaternion r4) => math.mul(mul(r1, r2, r3), r4);
        static quaternion mul(quaternion r1, quaternion r2, quaternion r3, quaternion r4, quaternion r5) => math.mul(mul(r1, r2, r3, r4), r5);
        static quaternion mul(quaternion r1, quaternion r2, quaternion r3, quaternion r4, quaternion r5, quaternion r6) => math.mul(mul(r1, r2, r3, r4, r5), r6);


        static quaternion accumulate(quaternion r1) => r1;
        static quaternion accumulate(quaternion r1, quaternion r2) => math.mul(r2, r1);
        static quaternion accumulate(quaternion r1, quaternion r2, quaternion r3) => accumulate(accumulate(r1, r2), r3);// mul(3, mul(2, 1))
        static quaternion accumulate(quaternion r1, quaternion r2, quaternion r3, quaternion r4) => accumulate(accumulate(r1, r2, r3), r4);
        static quaternion accumulate(quaternion r1, quaternion r2, quaternion r3, quaternion r4, quaternion r5) => accumulate(accumulate(r1, r2, r3, r4), r5);
        static quaternion accumulate(quaternion r1, quaternion r2, quaternion r3, quaternion r4, quaternion r5, quaternion r6) => accumulate(accumulate(r1, r2, r3, r4, r5), r6);
        static quaternion accumulate(quaternion r1, quaternion r2, quaternion r3, quaternion r4, quaternion r5, quaternion r6, quaternion r7) => accumulate(accumulate(r1, r2, r3, r4, r5, r6), r7);

        // quaternion の回転適用は、右側がローカル側
        // 先に回転を計算すると、後に続く計算はその回転が適用された上で適用されることになり、
        // それはつまり後に計算される回転がよりローカルとして適用される＝先に適用されるということになる。

        //static quaternion downArmL() => quaternion.RotateZ(math.radians(+30));
        //static quaternion downArmR() => quaternion.RotateZ(math.radians(-30));
        ////static quaternion downArmL() => quaternion.RotateZ(math.radians(+45));
        ////static quaternion downArmR() => quaternion.RotateZ(math.radians(-45));

        static quaternion reverse(quaternion r) => new quaternion(r.value.x, -r.value.y, r.value.z, -r.value.w);


        static quaternion limitY(quaternion r, float rate)
        {
            var a = math.EulerZXY(r);
            a.z *= rate;

            var qx = quaternion.RotateX(a.x);
            var qy = quaternion.RotateY(a.y);
            var qz = quaternion.RotateZ(a.z);
            //return accumulate(qz, qx, qy);
            return quaternion.EulerZXY(new float3(a.x, a.y, a.z * rate));
        }


        /// <summary>
        /// 
        /// </summary>
        public static quaternion AccumulateStreamRotation<TFinder>(
            this TFinder rkf, OptionalBoneChecker opt, HumanBodyBones humanbone, MmdBodyBones mmdbone = MmdBodyBones.nobone)
            where TFinder : IKeyFinder<quaternion>
        {

            return humanbone switch
            {

                HumanBodyBones.LastBone => accumulate(// mul がいいのか？ちょっとわからん
                    rkf.getrot(MmdBodyBones.全ての親),
                    rkf.getrot(MmdBodyBones.センター),
                    rkf.getrot(MmdBodyBones.グルーブ)
                ),

                //HumanBodyBones.Hips => accumulate(
                //    //rkf.getrot(MmdBodyBones.センター),
                //    //rkf.getrot(MmdBodyBones.グルーブ),
                //    rkf.getrot(MmdBodyBones.下半身)
                ////kh.(MmdBodyBones.下半身2)
                //),
                HumanBodyBones.Spine => accumulate(
                    //accumulate(math.inverse(rkf.getrot(MmdBodyBones.下半身)), rkf.getrot(MmdBodyBones.上半身))
                    //rkf.getrot(MmdBodyBones.下半身2),
                    //math.inverse(rkf.getrot(MmdBodyBones.下半身)),
                    rkf.getrot(MmdBodyBones.上半身),
                    math.inverse(rkf.getrot(MmdBodyBones.下半身))
                ),


                // 単一ボーンは switch の _ => で処理するのでコメントアウト

                //HumanBodyBones.Chest =>
                //    MmdBodyBones.上半身2,
                //HumanBodyBones.Head =>
                //    MmdBodyBones.頭,
                //HumanBodyBones.Neck =>
                //    MmdBodyBones.首,


                //HumanBodyBones.LeftShoulder =>
                //    math.slerp(quaternion.identity, rkf.getrot(MmdBodyBones.左肩), -1.2f),
                //HumanBodyBones.RightShoulder =>
                //    math.slerp(quaternion.identity, rkf.getrot(MmdBodyBones.右肩), -1.2f),

                //HumanBodyBones.LeftShoulder => accumulate(
                //    //rkf.getrotIfOptout(opt, HumanBodyBones.UpperChest, !opt.HasChest),    // upper chest にあたる mmd ボーンはなさそうなので未設定、バグる
                //    //rkf.getrot(MmdBodyBones.左肩2),
                //    rkf.getrot(MmdBodyBones.左肩)
                //),

                //HumanBodyBones.RightShoulder => accumulate(
                //    //rkf.getrotIfOptout(opt, HumanBodyBones.UpperChest, !opt.HasChest),    // upper chest にあたる mmd ボーンはなさそうなので未設定、バグる
                //    //rkf.getrot(MmdBodyBones.右肩2),
                //    rkf.getrot(MmdBodyBones.右肩)
                //),

                // 肩ボーンはいろいろあるみたいだけど、関係性がわからないからとりあえず無視

                //HumanBodyBones.LeftShoulder => accumulate(
                //    //rkf.getrotIfOptout(opt, HumanBodyBones.UpperChest, !opt.HasChest),
                //    rkf.getrot(MmdBodyBones.左肩P),
                //    rkf.getrot(MmdBodyBones.左肩),
                //    math.inverse(rkf.getrot(MmdBodyBones.左肩C))
                //),

                //HumanBodyBones.RightShoulder => accumulate(
                //    //rkf.getrotIfOptout(opt, HumanBodyBones.UpperChest, !opt.HasChest),
                //    rkf.getrot(MmdBodyBones.右肩P),
                //    rkf.getrot(MmdBodyBones.右肩),
                //    math.inverse(rkf.getrot(MmdBodyBones.右肩C))
                //),

                ////HumanBodyBones.LeftShoulder =>
                ////    //math.slerp(quaternion.identity, rkf.getrot(MmdBodyBones.左肩), opt.omake)
                ////    limitY(rkf.getrot(MmdBodyBones.左肩), opt.omake)
                ////,
                ////HumanBodyBones.RightShoulder => accumulate(
                ////    //math.slerp(quaternion.identity, rkf.getrot(MmdBodyBones.右肩), opt.omake)
                ////    limitY(rkf.getrot(MmdBodyBones.右肩), opt.omake)
                ////    //rkf.getrot(MmdBodyBones.右肩),
                ////    //math.inverse(quaternion.RotateY(math.EulerZXY(rkf.getrot(MmdBodyBones.右肩)).y * (1-opt.omake)))
                ////    //math.inverse(limitY(rkf.getrot(MmdBodyBones.右肩), opt.omake))
                ////),

                HumanBodyBones.LeftUpperArm => accumulate(
                    //math.inverse(math.slerp(quaternion.identity, rkf.getrot(MmdBodyBones.左肩), opt.omake)),
                    rkf.getrotIfOptout(opt, HumanBodyBones.LeftShoulder, !opt.HasLeftSholder),
                    //downArmL(),
                    rkf.getrot(MmdBodyBones.左腕捩),
                    rkf.getrot(MmdBodyBones.左腕)
                    //math.inverse(math.slerp(quaternion.identity, rkf.getrot(MmdBodyBones.左肩), opt.omake))
                    //math.inverse(limitY(rkf.getrot(MmdBodyBones.左肩), opt.omake))
                ),

                HumanBodyBones.RightUpperArm => accumulate(
                    rkf.getrotIfOptout(opt, HumanBodyBones.RightShoulder, !opt.HasRightSholder),
                    //downArmR(),,
                    rkf.getrot(MmdBodyBones.右腕捩),
                    rkf.getrot(MmdBodyBones.右腕)
                    //math.inverse(math.slerp(quaternion.identity, rkf.getrot(MmdBodyBones.右肩), opt.omake))
                    //math.inverse(limitY(rkf.getrot(MmdBodyBones.右肩), opt.omake))
                ),

                // 腕のねじりは、１～３はメッシュ用にねじりを分散させた補間値っぽい
                // 捩ボーンだけ考えればいいと思うんだけどどうなんだろ？
                // （腕付け根に捩りを入れてしまうと、肩付近から回転してしまうし、肘に仕込むと前腕の捩りになってしまうので、腕と捩りは分割せざるを得ない、んだと思う）

                //HumanBodyBones.LeftLowerArm => accumulate(
                //    ////rkf.getrot(MmdBodyBones.左腕捩1),
                //    ////rkf.getrot(MmdBodyBones.左腕捩2),
                //    ////rkf.getrot(MmdBodyBones.左腕捩3),
                //    //rkf.getrot(MmdBodyBones.左腕捩),
                //    //rkf.getrot(MmdBodyBones.左手捩),
                //    rkf.getrot(MmdBodyBones.左ひじ)
                //    //rkf.getrot(MmdBodyBones.左ひじ),
                //    //rkf.getrot(MmdBodyBones.左腕捩)
                //),

                //HumanBodyBones.RightLowerArm => accumulate(
                //    ////rkf.getrot(MmdBodyBones.右腕捩1),
                //    ////rkf.getrot(MmdBodyBones.右腕捩2),
                //    ////rkf.getrot(MmdBodyBones.右腕捩3),
                //    //rkf.getrot(MmdBodyBones.右腕捩),
                //    //rkf.getrot(MmdBodyBones.右手捩),
                //    rkf.getrot(MmdBodyBones.右ひじ)
                //    //rkf.getrot(MmdBodyBones.右ひじ),
                //    //rkf.getrot(MmdBodyBones.右腕捩)
                //),

                HumanBodyBones.LeftHand => accumulate(
                    rkf.getrot(MmdBodyBones.左手捩),
                    rkf.getrot(MmdBodyBones.左手首)
                ),

                HumanBodyBones.RightHand => accumulate(
                    rkf.getrot(MmdBodyBones.右手捩),
                    rkf.getrot(MmdBodyBones.右手首)
                ),


                //HumanBodyBones.LeftThumbProximal => accumulate(
                //    downArmL(),
                //    rkf.getrot(MmdBodyBones.左親指０)
                //    ),
                //HumanBodyBones.LeftThumbIntermediate => accumulate(
                //    downArmL(),
                //    rkf.getrot(MmdBodyBones.左親指１)
                //    ),
                //HumanBodyBones.LeftThumbDistal =>accumulate(
                //    downArmL(),
                //    rkf.getrot(MmdBodyBones.左親指２)
                //    ),


                ////HumanBodyBones.LeftUpperLeg =>
                ////    MmdBodyBones.左足,
                ////HumanBodyBones.LeftLowerLeg =>
                ////    MmdBodyBones.左ひざ,
                //HumanBodyBones.LeftFoot => accumulate(
                //    rkf.getrot(MmdBodyBones.左足首)
                ////rkf.getrot(MmdBodyBones.左足ＩＫ)
                //),
                //HumanBodyBones.LeftToes =>
                //    rkf.getrot(MmdBodyBones.左つま先),

                ////HumanBodyBones.RightUpperLeg =>
                ////    MmdBodyBones.右足,
                ////HumanBodyBones.RightLowerLeg =>
                ////    MmdBodyBones.右ひざ,
                //HumanBodyBones.RightFoot => accumulate(
                //    rkf.getrot(MmdBodyBones.右足首)
                ////rkf.getrot(MmdBodyBones.右足ＩＫ)
                //),
                //HumanBodyBones.RightToes =>
                //    rkf.getrot(MmdBodyBones.右つま先),

                _ =>
                    rkf.getrot(mmdbone),
            };
        }

        ///// <summary>
        ///// 
        ///// </summary>
        //public static quaternion AccumulateStreamRotation<TFinder>(
        //    this TFinder rkf, OptionalBoneChecker opt, HumanBodyBones humanbone, MmdBodyBones mmdbone = MmdBodyBones.nobone)
        //    where TFinder : IKeyFinder<quaternion>
        //{

        //    return humanbone switch
        //    {

        //        HumanBodyBones.LastBone => mul(
        //            rkf.getrot(MmdBodyBones.全ての親),
        //            rkf.getrot(MmdBodyBones.センター),
        //            rkf.getrot(MmdBodyBones.グルーブ)
        //        ),

        //        HumanBodyBones.Hips => mul(
        //            //rkf.getrot(MmdBodyBones.センター),
        //            //rkf.getrot(MmdBodyBones.グルーブ),
        //            rkf.getrot(MmdBodyBones.下半身)
        //        //kh.(MmdBodyBones.下半身2)
        //        ),
        //        HumanBodyBones.Spine => mul(
        //            //mul(math.inverse(rkf.getrot(MmdBodyBones.下半身)), rkf.getrot(MmdBodyBones.上半身))
        //            //rkf.getrot(MmdBodyBones.下半身2),
        //            math.inverse(rkf.getrot(MmdBodyBones.下半身)),
        //            rkf.getrot(MmdBodyBones.上半身)
        //        ),


        //        // 単一ボーンは switch の _ => で処理するのでコメントアウト

        //        //HumanBodyBones.Chest =>
        //        //    MmdBodyBones.上半身2,
        //        //HumanBodyBones.Head =>
        //        //    MmdBodyBones.頭,
        //        //HumanBodyBones.Neck =>
        //        //    MmdBodyBones.首,


        //        HumanBodyBones.LeftShoulder => mul(
        //            //rkf.getrotIfOptout(opt, HumanBodyBones.UpperChest, !opt.HasChest),    // upper chest にあたる mmd ボーンはなさそうなので未設定、バグる
        //            //rkf.getrot(MmdBodyBones.左肩2),
        //            rkf.getrot(MmdBodyBones.左肩)
        //        ),

        //        HumanBodyBones.RightShoulder => mul(
        //            //rkf.getrotIfOptout(opt, HumanBodyBones.UpperChest, !opt.HasChest),    // upper chest にあたる mmd ボーンはなさそうなので未設定、バグる
        //            //rkf.getrot(MmdBodyBones.右肩2),
        //            rkf.getrot(MmdBodyBones.右肩)
        //        ),

        //        // 肩ボーンはいろいろあるみたいだけど、関係性がわからないからとりあえず無視

        //        //HumanBodyBones.LeftShoulder => mul(
        //        //    //rkf.getrotIfOptout(opt, HumanBodyBones.UpperChest, !opt.HasChest),
        //        //    rkf.getrot(MmdBodyBones.左肩P),
        //        //    rkf.getrot(MmdBodyBones.左肩),
        //        //    math.inverse(rkf.getrot(MmdBodyBones.左肩C))
        //        //),

        //        //HumanBodyBones.RightShoulder => mul(
        //        //    //rkf.getrotIfOptout(opt, HumanBodyBones.UpperChest, !opt.HasChest),
        //        //    rkf.getrot(MmdBodyBones.右肩P),
        //        //    rkf.getrot(MmdBodyBones.右肩),
        //        //    math.inverse(rkf.getrot(MmdBodyBones.右肩C))
        //        //),


        //        HumanBodyBones.LeftUpperArm => mul(
        //            rkf.getrotIfOptout(opt, HumanBodyBones.LeftShoulder, !opt.HasLeftSholder),
        //            //downArmL(),
        //            rkf.getrot(MmdBodyBones.左腕),
        //            rkf.getrot(MmdBodyBones.左腕捩)
        //        //rkf.getrot(MmdBodyBones.左腕捩),
        //        //rkf.getrot(MmdBodyBones.左腕)//
        //        ),

        //        HumanBodyBones.RightUpperArm => mul(
        //            rkf.getrotIfOptout(opt, HumanBodyBones.RightShoulder, !opt.HasRightSholder),
        //            //downArmR(),,
        //            rkf.getrot(MmdBodyBones.右腕),
        //            rkf.getrot(MmdBodyBones.右腕捩)
        //        //rkf.getrot(MmdBodyBones.右腕捩),
        //        //rkf.getrot(MmdBodyBones.右腕)//
        //        ),

        //        // 腕のねじりは、１～３はメッシュ用にねじりを分散させた補間値っぽい
        //        // 捩ボーンだけ考えればいいと思うんだけどどうなんだろ？
        //        // （腕付け根に捩りを入れてしまうと、肩付近から回転してしまうし、肘に仕込むと前腕の捩りになってしまうので、腕と捩りは分割せざるを得ない、んだと思う）

        //        HumanBodyBones.LeftLowerArm => mul(
        //            ////rkf.getrot(MmdBodyBones.左腕捩1),
        //            ////rkf.getrot(MmdBodyBones.左腕捩2),
        //            ////rkf.getrot(MmdBodyBones.左腕捩3),
        //            //rkf.getrot(MmdBodyBones.左腕捩),
        //            //rkf.getrot(MmdBodyBones.左手捩),
        //            rkf.getrot(MmdBodyBones.左ひじ)
        //        //rkf.getrot(MmdBodyBones.左ひじ),
        //        //rkf.getrot(MmdBodyBones.左腕捩)
        //        ),

        //        HumanBodyBones.RightLowerArm => mul(
        //            ////rkf.getrot(MmdBodyBones.右腕捩1),
        //            ////rkf.getrot(MmdBodyBones.右腕捩2),
        //            ////rkf.getrot(MmdBodyBones.右腕捩3),
        //            //rkf.getrot(MmdBodyBones.右腕捩),
        //            //rkf.getrot(MmdBodyBones.右手捩),
        //            rkf.getrot(MmdBodyBones.右ひじ)
        //        //rkf.getrot(MmdBodyBones.右ひじ),
        //        //rkf.getrot(MmdBodyBones.右腕捩)
        //        ),

        //        HumanBodyBones.LeftHand => mul(
        //        //    rkf.getrot(MmdBodyBones.左手捩),
        //        //    rkf.getrot(MmdBodyBones.左手首)
        //        rkf.getrot(MmdBodyBones.左手首),
        //        rkf.getrot(MmdBodyBones.左手捩)
        //        ),

        //        HumanBodyBones.RightHand => mul(
        //        //rkf.getrot(MmdBodyBones.右手捩),
        //        //rkf.getrot(MmdBodyBones.右手首)
        //        rkf.getrot(MmdBodyBones.右手首),
        //        rkf.getrot(MmdBodyBones.右手捩)
        //        ),


        //        //HumanBodyBones.LeftThumbProximal => mul(
        //        //    downArmL(),
        //        //    rkf.getrot(MmdBodyBones.左親指０)
        //        //    ),
        //        //HumanBodyBones.LeftThumbIntermediate => mul(
        //        //    downArmL(),
        //        //    rkf.getrot(MmdBodyBones.左親指１)
        //        //    ),
        //        //HumanBodyBones.LeftThumbDistal =>mul(
        //        //    downArmL(),
        //        //    rkf.getrot(MmdBodyBones.左親指２)
        //        //    ),


        //        ////HumanBodyBones.LeftUpperLeg =>
        //        ////    MmdBodyBones.左足,
        //        ////HumanBodyBones.LeftLowerLeg =>
        //        ////    MmdBodyBones.左ひざ,
        //        //HumanBodyBones.LeftFoot => mul(
        //        //    rkf.getrot(MmdBodyBones.左足首)
        //        ////rkf.getrot(MmdBodyBones.左足ＩＫ)
        //        //),
        //        //HumanBodyBones.LeftToes =>
        //        //    rkf.getrot(MmdBodyBones.左つま先),

        //        ////HumanBodyBones.RightUpperLeg =>
        //        ////    MmdBodyBones.右足,
        //        ////HumanBodyBones.RightLowerLeg =>
        //        ////    MmdBodyBones.右ひざ,
        //        //HumanBodyBones.RightFoot => mul(
        //        //    rkf.getrot(MmdBodyBones.右足首)
        //        ////rkf.getrot(MmdBodyBones.右足ＩＫ)
        //        //),
        //        //HumanBodyBones.RightToes =>
        //        //    rkf.getrot(MmdBodyBones.右つま先),

        //        _ =>
        //            rkf.getrot(mmdbone),
        //    };
        //}
    }

}
