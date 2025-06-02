using AnimLite.IK;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace AnimLite.Vmd
{

    public struct VmdBodyMotionOperator<TBone, TTf>
        where TBone : ITransformMappings<TTf>
        where TTf : ITransformProxy
    {

        [ReadOnly]
        public TBone bones;


        [ReadOnly]
        public float3 moveScale;
        [ReadOnly]
        public float3 bodyScale;

        [ReadOnly]
        public float3 spineToHipLocal;
        [ReadOnly]
        public float3 rootToHipLocal;


        //public float3 prevLocalPosition;
        //public quaternion prevLocalRotation;
    }


    public static class VmdBodyMotionOperator
    {

        // humanoid の初期ポーズの中心が root に来るようで、そのｙ位置はたぶん 1m * humanScale 
        // なので仕方なく hip の位置を humanScale で元に戻すために、ここで設定する。
        //var bodyInitialOffset = Vector3.up.As_float3() * anim.humanScale;
        // ↑上記のように考えていたが、どうも hip の高さと考えてよいらしい。なので、calcAnimatonScale() のようにする。

        public const float VmdBodyScale = 0.8f;// 0.8 は、ミク → humaoid 補正

        /// <summary>
        /// mmd と humanoid のスケール比 80cm : 100cm くらい
        /// 158cm のミクの股位置がそのくらいと思われる
        /// humanoid の humanscale 1m は、hip の位置らしいとのこと
        /// </summary>
        public static float3 calcVmdBoneScale(this Animator anim, float3 scale) =>
             /*anim.transform.lossyScale * */VmdBodyScale * math.select(
                falseValue:
                    scale,
                trueValue:
                    anim.humanScale,
                test:
                    scale == 0.0f);

        public static float3 calcVmdBoneScale(this Animator anim) =>
            /*anim.transform.lossyScale * */VmdBodyScale * anim.humanScale;

        // ＭＭＤの移動データは、ボーンのオフセットは除いた値が格納されているように思う。
        // なので、センター → 下半身 までの高さは、移動データには反映されていないと考える。
        // つまり、相対的移動量のみがデータとなっており、root 位置は足元のまま移動させればよい、と考える。

    }

    public static class VmdBodyMotionOperatorBuilderExtension
    {

        public static VmdBodyMotionOperator<TransformMappings, Tf> ToVmdBodyTransformMotionOperator(
            this Animator anim, TransformMappings bone)
        =>
            anim.ToVmdBodyMotionOperator<TransformMappings, Tf>(bone);



        public static VmdBodyMotionOperator<TBone, TTf> ToVmdBodyMotionOperator<TBone, TTf>(this Animator anim, TBone bone)
            where TBone : ITransformMappings<TTf>
            where TTf : ITransformProxy
        {
            var scale = (float3)anim.transform.lossyScale;
            var bonescale = anim.calcVmdBoneScale();

            return new VmdBodyMotionOperator<TBone, TTf>
            {
                moveScale = bonescale,
                bodyScale = bonescale,

                bones = bone,

                spineToHipLocal = -anim.GetBoneTransform(HumanBodyBones.Spine).localPosition,// * scale,
                rootToHipLocal = anim.GetBoneTransform(HumanBodyBones.Hips).localPosition,// * scale,
            };
        }


        public static VmdBodyMotionOperator<TBone, TTf> WithScales<TBone, TTf>(
            this VmdBodyMotionOperator<TBone, TTf> bodyop, Animator anim,
            float3 moveScale, float3 bodyScale)
                where TBone : ITransformMappings<TTf>
                where TTf : ITransformProxy
        {
            bodyop.moveScale = anim.calcVmdBoneScale(moveScale);
            bodyop.bodyScale = anim.calcVmdBoneScale(bodyScale);

            return bodyop;
        }
        //public static VmdBodyMotionOperator<TBone, TTf> WithScales<TBone, TTf>(
        //    this VmdBodyMotionOperator<TBone, TTf> bodyop,
        //    float3 moveScale, float3 bodyScale)
        //        where TBone : ITransformMappings<TTf>
        //        where TTf : ITransformProxy
        //{
        //    if (moveScale != 0.0f) bodyop.moveScale = moveScale;
        //    if (bodyScale != 0.0f) bodyop.bodyScale = bodyScale;

        //    return bodyop;
        //}
    }

}
