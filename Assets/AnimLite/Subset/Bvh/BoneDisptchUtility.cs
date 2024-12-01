using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace AnimLite
{
    using AnimLite;
    using AnimLite.Utility;


    ///// <summary>
    ///// 
    ///// </summary>
    //public struct VmdHumanBoneReference<TTf>
    //    where TTf : ITransformProxy
    //{
    //    public TTf TransformHandle;

    //    public HumanBodyBones HumanBoneId;
    //    public MmdBodyBones StreamId;

    //    public static implicit operator VmdHumanBoneReference<TTf>(HumanBoneReference<TTf> src) =>
    //        new VmdHumanBoneReference<TTf>
    //        {
    //            TransformHandle = src.TransformHandle,
    //            HumanBoneId = src.HumanBoneId,
    //            StreamId = (MmdBodyBones)src.StreamId,
    //        };
    //}




    public static class BoneDispatchExtension
    {



        public static quaternion getrot<TFinder>(this TFinder kf, HumanBodyBones ibone)
            where TFinder : IKeyFinder<quaternion>
        =>
            kf.get((int)ibone);

        public static float4 getpos<TFinder>(this TFinder kf, HumanBodyBones ibone)
            where TFinder : IKeyFinder<float4>
        =>
            kf.get((int)ibone);

        public static quaternion getrotIfOptout<TFinder>(
            this TFinder rkf, OptionalBoneChecker opt, HumanBodyBones humanbone, bool optionBoneIsNotExists)
            where TFinder : IKeyFinder<quaternion>
        {
            if (!optionBoneIsNotExists) return quaternion.identity;

            return rkf.AccumulateStreamRotation(opt, humanbone);
        }




        /// <summary>
        /// 
        /// </summary>
        public static float3 AccumulateStreamPosition<TPFinder, TRFinder>(
            this TPFinder pkf, TRFinder rkf, HumanBodyBones humanbone)
            where TPFinder : IKeyFinder<float4>
            where TRFinder : IKeyFinder<quaternion>
        {

            return humanbone switch
            {

                HumanBodyBones.Hips =>
                    pkf.getpos(HumanBodyBones.Hips).As3(),

                _ => default,
            };
        }




        static quaternion mul(quaternion r1) => r1;
        static quaternion mul(quaternion r1, quaternion r2) => math.mul(r1, r2);
        static quaternion mul(quaternion r1, quaternion r2, quaternion r3) => math.mul(math.mul(r1, r2), r3);
        static quaternion mul(quaternion r1, quaternion r2, quaternion r3, quaternion r4) => math.mul(mul(r1, r2, r3), r4);
        static quaternion mul(quaternion r1, quaternion r2, quaternion r3, quaternion r4, quaternion r5) => math.mul(mul(r1, r2, r3, r4), r5);
        static quaternion mul(quaternion r1, quaternion r2, quaternion r3, quaternion r4, quaternion r5, quaternion r6) => math.mul(mul(r1, r2, r3, r4, r5), r6);


        //static quaternion accumulate(quaternion r1) => r1;
        //static quaternion accumulate(quaternion r1, quaternion r2) => math.mul(r2, r1);
        //static quaternion accumulate(quaternion r1, quaternion r2, quaternion r3) => accumulate(accumulate(r1, r2), r3);// mul(3, mul(2, 1))
        //static quaternion accumulate(quaternion r1, quaternion r2, quaternion r3, quaternion r4) => accumulate(accumulate(r1, r2, r3), r4);
        //static quaternion accumulate(quaternion r1, quaternion r2, quaternion r3, quaternion r4, quaternion r5) => accumulate(accumulate(r1, r2, r3, r4), r5);
        //static quaternion accumulate(quaternion r1, quaternion r2, quaternion r3, quaternion r4, quaternion r5, quaternion r6) => accumulate(accumulate(r1, r2, r3, r4, r5), r6);
        //static quaternion accumulate(quaternion r1, quaternion r2, quaternion r3, quaternion r4, quaternion r5, quaternion r6, quaternion r7) => accumulate(accumulate(r1, r2, r3, r4, r5, r6), r7);


        static quaternion downArmL() => quaternion.RotateZ(math.radians(+30));
        static quaternion downArmR() => quaternion.RotateZ(math.radians(-30));
        //static quaternion downArmL() => quaternion.RotateZ(math.radians(+45));
        //static quaternion downArmR() => quaternion.RotateZ(math.radians(-45));

        static quaternion reverse(quaternion r) => new quaternion(r.value.x, -r.value.y, r.value.z, -r.value.w);


        /// <summary>
        /// äÓñ{ìIÇ… human bone id Ç∆ stream id ÇÕìØÇ∂Å@strem id ÇÕ last bone ÇÃå„ÇÎÇ…ì∆é©î‘çÜÇí«â¡Ç≈Ç´ÇÈ
        /// </summary>
        public static quaternion AccumulateStreamRotation<TFinder>(//, TTF>(
            this TFinder rkf, OptionalBoneChecker opt, HumanBodyBones humanbone)//, int streamId)
                where TFinder : IKeyFinder<quaternion>
        {

            return humanbone switch
            {

                HumanBodyBones.Neck => mul(
                    rkf.getrotIfOptout(opt, HumanBodyBones.UpperChest, !opt.HasChest),
                    rkf.getrot(humanbone)
                    //rkf.get(streamId)
                ),


                HumanBodyBones.LeftShoulder => mul(
                    rkf.getrotIfOptout(opt, HumanBodyBones.UpperChest, !opt.HasChest),
                    rkf.getrot(humanbone)
                    //rkf.get(streamId)
                ),

                HumanBodyBones.RightShoulder => mul(
                    rkf.getrotIfOptout(opt, HumanBodyBones.UpperChest, !opt.HasChest),
                    rkf.getrot(humanbone)
                    //rkf.get(streamId)
                ),


                HumanBodyBones.LeftUpperArm => mul(
                    rkf.getrotIfOptout(opt, HumanBodyBones.LeftShoulder, !opt.HasLeftSholder),
                    rkf.getrot(humanbone)
                    //rkf.get(streamId)
                ),

                HumanBodyBones.RightUpperArm => mul(
                    rkf.getrotIfOptout(opt, HumanBodyBones.RightShoulder, !opt.HasRightSholder),
                    rkf.getrot(humanbone)
                    //rkf.get(streamId)
                ),


                _ =>
                    rkf.getrot(humanbone)
                    //rkf.get(streamId)
            };
        }

    }

}
