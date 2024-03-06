using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace AnimLite.Vmd
{

    public struct VmdBodyMotionOperator<TBone, TTf>
        where TBone : IStreamBone<TTf>
        where TTf : ITransformProxy
    {

        [ReadOnly]
        public TBone bone;


        [ReadOnly]
        public float bodyScale;

        [ReadOnly]
        public float3 spineToHipLocal;
        [ReadOnly]
        public float3 rootToHipLocal;


        //public float3 prevLocalPosition;
        //public quaternion prevLocalRotation;

    }


    public static class VmdBodyMotionOperatorBuilderExtension
    {



        public static VmdBodyMotionOperator<TransformStreamingBone, Tf> ToVmdBodyTransformMotionOperator(
            this Animator anim, TransformStreamingBone bone)
        =>
            anim.ToVmdBodyMotionOperator<TransformStreamingBone, Tf>(bone);


        // mmd と humanoid のスケール比 80cm : 100cm くらい
        // 158cm のミクの股位置がそのくらいと思われる
        // humanoid の humanscale 1m は、hip の位置らしいとのこと

        public static VmdBodyMotionOperator<TBone, TTf> ToVmdBodyMotionOperator<TBone, TTf>(
            this Animator anim, TBone bone)
                where TBone : IStreamBone<TTf>
                where TTf : ITransformProxy
        {
            //anim.ResetPose();

            var bodySizeRate = anim.humanScale * 0.8f;// 0.8 は、ミク → humaoid 補正

            // humanoid の初期ポーズの中心が root に来るようで、そのｙ位置はたぶん 1m * humanScale 
            // なので仕方なく hip の位置を humanScale で元に戻すために、ここで設定する。
            //var bodyInitialOffset = Vector3.up.As_float3() * anim.humanScale;
            // ↑上記のように考えていたが、どうも hip の高さと考えてよいらしい。

            //anim.BindStreamTransform(anim.transform);// バインドしないと rootMotionPosition が取得できない様子

            return new VmdBodyMotionOperator<TBone, TTf>
            {
                bodyScale = bodySizeRate,

                bone = bone,

                spineToHipLocal = -anim.GetBoneTransform(HumanBodyBones.Spine).localPosition,
                rootToHipLocal = anim.GetBoneTransform(HumanBodyBones.Hips).localPosition,
            };
        }

    }

}
