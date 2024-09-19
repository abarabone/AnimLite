using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace AnimLite.Vmd
{
    using AnimLite;
    using AnimLite.Utility;


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

        public static float4 getpos<TFinder>(this TFinder kf, MmdBodyBones ibone)
            where TFinder : IKeyFinder<float4>
        =>
            kf.get((int)ibone);

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

            return humanbone switch
            {

                HumanBodyBones.LastBone =>
                    //kf.p.get((int)MmdBodyBones.�Z���^�[).As3() * 0.1f,

                    //math.rotate(math.inverse(math.mul(rkf.get((int)MmdBodyBones.�O���[�u), rkf.get((int)MmdBodyBones.�Z���^�[))), pkf.get((int)MmdBodyBones.�S�Ă̐e).To3()) *0.1f +
                    //math.rotate(math.inverse(rkf.get((int)MmdBodyBones.�O���[�u)), pkf.get((int)MmdBodyBones.�Z���^�[).To3()) * 0.1f +
                    //pkf.get((int)MmdBodyBones.�O���[�u).To3() * 0.1f,

                    //kf.p.get((int)MmdBodyBones.�S�Ă̐e).As3() * 0.1f +
                    //math.rotate(kf.r.get((int)MmdBodyBones.�Z���^�[), kf.p.get((int)MmdBodyBones.�Z���^�[).As3()) * 0.1f +
                    //math.rotate(mul(kf.r.get((int)MmdBodyBones.�O���[�u), kf.r.get((int)MmdBodyBones.�Z���^�[)), kf.p.get((int)MmdBodyBones.�O���[�u).As3()) * 0.1f,

                    //pkf.getpos(MmdBodyBones.�S�Ă̐e).As3() * 0.1f +
                    a() * 0.1f,

                HumanBodyBones.Hips =>
                    pkf.getpos(MmdBodyBones.�����g).As3() * 0.1f,

                HumanBodyBones.Spine =>
                    pkf.getpos(MmdBodyBones.�㔼�g).As3() * 0.1f,

                _ => default,
            };

            float3 a()
            {
                var arr = new NativeArray<MmdBodyBones>(3, Allocator.Temp);
                arr[0] = MmdBodyBones.�S�Ă̐e;
                arr[1] = MmdBodyBones.�Z���^�[;
                arr[2] = MmdBodyBones.�O���[�u;
                var pos = (pkf, rkf).transform<TPFinder, TRFinder>(arr);
                arr.Dispose();
                return pos;
            }
        }

        static float3 transform<TPFinder, TRFinder>(
            this (TPFinder p, TRFinder r) kf, NativeSlice<MmdBodyBones> istreams)
                where TPFinder : IKeyFinder<float4>
                where TRFinder : IKeyFinder<quaternion>
        {
            var pos = kf.p.getpos(istreams[0]).As3();

            for (var i = 1; i < istreams.Length; i++)
            {
                var lrot = kf.r.getrot(istreams[i - 1]);
                var lpos = kf.p.getpos(istreams[i - 0]).As3();

                pos += math.rotate(lrot, lpos);
            }

            return pos;
        }



        static quaternion mul(quaternion r1, quaternion r2) => math.mul(r1, r2);
        static quaternion mul(quaternion r1, quaternion r2, quaternion r3) => math.mul(math.mul(r1, r2), r3);
        static quaternion mul(quaternion r1, quaternion r2, quaternion r3, quaternion r4) => math.mul(mul(r1, r2, r3), r4);
        static quaternion mul(quaternion r1, quaternion r2, quaternion r3, quaternion r4, quaternion r5) => math.mul(mul(r1, r2, r3, r4), r5);
        static quaternion mul(quaternion r1, quaternion r2, quaternion r3, quaternion r4, quaternion r5, quaternion r6) => math.mul(mul(r1, r2, r3, r4, r5), r6);


        static quaternion accumulate(quaternion r1) => r1;
        static quaternion accumulate(quaternion r1, quaternion r2) => math.mul(r2, r1);
        static quaternion accumulate(quaternion r1, quaternion r2, quaternion r3) => accumulate(accumulate(r1, r2), r3);
        static quaternion accumulate(quaternion r1, quaternion r2, quaternion r3, quaternion r4) => accumulate(accumulate(r1, r2, r3), r4);
        static quaternion accumulate(quaternion r1, quaternion r2, quaternion r3, quaternion r4, quaternion r5) => accumulate(accumulate(r1, r2, r3, r4), r5);
        static quaternion accumulate(quaternion r1, quaternion r2, quaternion r3, quaternion r4, quaternion r5, quaternion r6) => accumulate(accumulate(r1, r2, r3, r4, r5), r6);
        static quaternion accumulate(quaternion r1, quaternion r2, quaternion r3, quaternion r4, quaternion r5, quaternion r6, quaternion r7) => accumulate(accumulate(r1, r2, r3, r4, r5, r6), r7);


        static quaternion downArmL() => quaternion.RotateZ(math.radians(+30));
        static quaternion downArmR() => quaternion.RotateZ(math.radians(-30));
        //static quaternion downArmL() => quaternion.RotateZ(math.radians(+45));
        //static quaternion downArmR() => quaternion.RotateZ(math.radians(-45));

        static quaternion reverse(quaternion r) => new quaternion(r.value.x, -r.value.y, r.value.z, -r.value.w);


        /// <summary>
        /// 
        /// </summary>
        public static quaternion AccumulateStreamRotation<TFinder>(
            this TFinder rkf, OptionalBoneChecker opt, HumanBodyBones humanbone, MmdBodyBones mmdbone = MmdBodyBones.nobone)
            where TFinder : IKeyFinder<quaternion>
        {

            return humanbone switch
            {

                HumanBodyBones.LastBone => accumulate(
                    rkf.getrot(MmdBodyBones.�S�Ă̐e),
                    rkf.getrot(MmdBodyBones.�Z���^�[),
                    rkf.getrot(MmdBodyBones.�O���[�u)
                ),

                HumanBodyBones.Hips => accumulate(
                    //rkf.getrot(MmdBodyBones.�Z���^�[),
                    //rkf.getrot(MmdBodyBones.�O���[�u),
                    rkf.getrot(MmdBodyBones.�����g)
                //kh.(MmdBodyBones.�����g2)
                ),
                HumanBodyBones.Spine => accumulate(
                    //mul(math.inverse(rkf.getrot(MmdBodyBones.�����g)), rkf.getrot(MmdBodyBones.�㔼�g))
                //rkf.getrot(MmdBodyBones.�����g2),
                    rkf.getrot(MmdBodyBones.�㔼�g),
                    math.inverse(rkf.getrot(MmdBodyBones.�����g))
                ),


                // �P��{�[���� switch �� _ => �ŏ�������̂ŃR�����g�A�E�g

                //HumanBodyBones.Chest =>
                //    MmdBodyBones.�㔼�g2,
                //HumanBodyBones.Head =>
                //    MmdBodyBones.��,
                //HumanBodyBones.Neck =>
                //    MmdBodyBones.��,


                HumanBodyBones.LeftShoulder => accumulate(
                    //rkf.getrotIfOptout(opt, HumanBodyBones.UpperChest, !opt.HasChest),    // upper chest �ɂ����� mmd �{�[���͂Ȃ������Ȃ̂Ŗ��ݒ�A�o�O��
                    rkf.getrot(MmdBodyBones.����)
                ),

                HumanBodyBones.RightShoulder => accumulate(
                    //rkf.getrotIfOptout(opt, HumanBodyBones.UpperChest, !opt.HasChest),    // upper chest �ɂ����� mmd �{�[���͂Ȃ������Ȃ̂Ŗ��ݒ�A�o�O��
                    rkf.getrot(MmdBodyBones.�E��)
                ),

                // ���{�[���͂��낢�날��݂��������ǁA�֌W�����킩��Ȃ�����Ƃ肠��������

                //HumanBodyBones.LeftShoulder => accumulate(
                //    //rkf.getrotIfOptout(opt, HumanBodyBones.UpperChest, !opt.HasChest),
                //    rkf.getrot(MmdBodyBones.����P),
                //    rkf.getrot(MmdBodyBones.����),
                //    math.inverse(rkf.getrot(MmdBodyBones.����C))
                //),

                //HumanBodyBones.RightShoulder => accumulate(
                //    //rkf.getrotIfOptout(opt, HumanBodyBones.UpperChest, !opt.HasChest),
                //    rkf.getrot(MmdBodyBones.�E��P),
                //    rkf.getrot(MmdBodyBones.�E��),
                //    math.inverse(rkf.getrot(MmdBodyBones.�E��C))
                //),


                HumanBodyBones.LeftUpperArm => accumulate(
                    rkf.getrotIfOptout(opt, HumanBodyBones.LeftShoulder, !opt.HasLeftSholder),
                    //downArmL(),
                    rkf.getrot(MmdBodyBones.���r)//,
                    //rkf.getrot(MmdBodyBones.���r��)
                ),

                HumanBodyBones.RightUpperArm => accumulate(
                    rkf.getrotIfOptout(opt, HumanBodyBones.LeftShoulder, !opt.HasRightSholder),
                    //downArmR(),
                    rkf.getrot(MmdBodyBones.�E�r)//,
                    //rkf.getrot(MmdBodyBones.�E�r��)
                ),

                // �r�̂˂���́A�P�`�R�̓��b�V���p�ɂ˂���𕪎U��������Ԓl���ۂ�
                // ���{�[�������l����΂����Ǝv���񂾂��ǂǂ��Ȃ񂾂�H
                // �i�r�t�����ɝ�������Ă��܂��ƁA���t�߂����]���Ă��܂����A�I�Ɏd���ނƑO�r�̝���ɂȂ��Ă��܂��̂ŁA�r�Ɲ���͕���������𓾂Ȃ��A�񂾂Ǝv���j

                HumanBodyBones.LeftLowerArm => accumulate(
                    ////rkf.getrot(MmdBodyBones.���r��1),
                    ////rkf.getrot(MmdBodyBones.���r��2),
                    ////rkf.getrot(MmdBodyBones.���r��3),
                    rkf.getrot(MmdBodyBones.���r��),
                    rkf.getrot(MmdBodyBones.���Ђ�)
                    //rkf.getrot(MmdBodyBones.���Ђ�),
                    //rkf.getrot(MmdBodyBones.���r��)
                ),

                HumanBodyBones.RightLowerArm => accumulate(
                    ////rkf.getrot(MmdBodyBones.�E�r��1),
                    ////rkf.getrot(MmdBodyBones.�E�r��2),
                    ////rkf.getrot(MmdBodyBones.�E�r��3),
                    rkf.getrot(MmdBodyBones.�E�r��),
                    rkf.getrot(MmdBodyBones.�E�Ђ�)
                    //rkf.getrot(MmdBodyBones.�E�Ђ�),
                    //rkf.getrot(MmdBodyBones.�E�r��)
                ),

                HumanBodyBones.LeftHand => accumulate(
                    rkf.getrot(MmdBodyBones.���蝀),
                    rkf.getrot(MmdBodyBones.�����)
                //rkf.getrot(MmdBodyBones.�����),
                //rkf.getrot(MmdBodyBones.���蝀)
                ),

                HumanBodyBones.RightHand => accumulate(
                    rkf.getrot(MmdBodyBones.�E�蝀),
                    rkf.getrot(MmdBodyBones.�E���)
                //rkf.getrot(MmdBodyBones.�E���),
                //rkf.getrot(MmdBodyBones.�E�蝀)
                ),


                //HumanBodyBones.LeftThumbProximal => accumulate(
                //    downArmL(),
                //    rkf.getrot(MmdBodyBones.���e�w�O)
                //    ),
                //HumanBodyBones.LeftThumbIntermediate => accumulate(
                //    downArmL(),
                //    rkf.getrot(MmdBodyBones.���e�w�P)
                //    ),
                //HumanBodyBones.LeftThumbDistal =>accumulate(
                //    downArmL(),
                //    rkf.getrot(MmdBodyBones.���e�w�Q)
                //    ),


                ////HumanBodyBones.LeftUpperLeg =>
                ////    MmdBodyBones.����,
                ////HumanBodyBones.LeftLowerLeg =>
                ////    MmdBodyBones.���Ђ�,
                //HumanBodyBones.LeftFoot => accumulate(
                //    rkf.getrot(MmdBodyBones.������)
                ////rkf.getrot(MmdBodyBones.�����h�j)
                //),
                //HumanBodyBones.LeftToes =>
                //    rkf.getrot(MmdBodyBones.���ܐ�),

                ////HumanBodyBones.RightUpperLeg =>
                ////    MmdBodyBones.�E��,
                ////HumanBodyBones.RightLowerLeg =>
                ////    MmdBodyBones.�E�Ђ�,
                //HumanBodyBones.RightFoot => accumulate(
                //    rkf.getrot(MmdBodyBones.�E����)
                ////rkf.getrot(MmdBodyBones.�E���h�j)
                //),
                //HumanBodyBones.RightToes =>
                //    rkf.getrot(MmdBodyBones.�E�ܐ�),

                _ =>
                    rkf.getrot(mmdbone),
            };
        }

    }

}
