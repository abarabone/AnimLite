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
        public TBone bone;


        [ReadOnly]
        public float moveScale;
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


        // humanoid の初期ポーズの中心が root に来るようで、そのｙ位置はたぶん 1m * humanScale 
        // なので仕方なく hip の位置を humanScale で元に戻すために、ここで設定する。
        //var bodyInitialOffset = Vector3.up.As_float3() * anim.humanScale;
        // ↑上記のように考えていたが、どうも hip の高さと考えてよいらしい。なので、calcAnimatonScale() のようにする。

        /// <summary>
        /// mmd と humanoid のスケール比 80cm : 100cm くらい
        /// 158cm のミクの股位置がそのくらいと思われる
        /// humanoid の humanscale 1m は、hip の位置らしいとのこと
        /// </summary>
        public static float calcVmdBoneScale(this Animator anim, float scale) =>
            scale == 0.0f
                ? anim.humanScale * 0.8f// 0.8 は、ミク → humaoid 補正
                : scale * 0.8f;


        // ＭＭＤの移動データは、ボーンのオフセットは除いた値が格納されているように思う。
        // なので、センター → 下半身 までの高さは、移動データには反映されていないと考える。
        // つまり、相対的移動量のみがデータとなっており、root 位置は足元のまま移動させればよい、と考える。





        public static VmdBodyMotionOperator<TransformMappings, Tf> ToVmdBodyTransformMotionOperator(
            this Animator anim, TransformMappings bone, float moveScale = 0, float bodyScale = 0)
        =>
            anim.ToVmdBodyMotionOperator<TransformMappings, Tf>(bone, moveScale, bodyScale);



        public static VmdBodyMotionOperator<TBone, TTf> ToVmdBodyMotionOperator<TBone, TTf>(
            this Animator anim, TBone bone, float moveScale = 0, float bodyScale = 0)
                where TBone : ITransformMappings<TTf>
                where TTf : ITransformProxy
        =>
            new VmdBodyMotionOperator<TBone, TTf>
            {
                moveScale = anim.calcVmdBoneScale(moveScale),
                bodyScale = anim.calcVmdBoneScale(bodyScale),

                bone = bone,

                spineToHipLocal = -anim.GetBoneTransform(HumanBodyBones.Spine).localPosition,
                rootToHipLocal = anim.GetBoneTransform(HumanBodyBones.Hips).localPosition,
            };

    }

}
