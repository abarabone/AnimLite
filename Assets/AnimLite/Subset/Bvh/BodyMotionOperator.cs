using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace AnimLite
{

    public struct BodyMotionOperator<TBone, TTf>
        where TBone : ITransformMappings<TTf>
        where TTf : ITransformProxy
    {

        [ReadOnly]
        public TBone bone;


        [ReadOnly]
        public float bodyScale;


    }


    public static class VmdBodyMotionOperatorBuilderExtension
    {



        public static BodyMotionOperator<TransformMappings, Tf> ToBodyTransformMotionOperator(
            this Animator anim, TransformMappings bone, float bodyScale = 0)
        =>
            anim.ToBodyMotionOperator<TransformMappings, Tf>(bone, bodyScale);



        public static BodyMotionOperator<TBone, TTf> ToBodyMotionOperator<TBone, TTf>(this Animator anim, TBone bone, float bodyScale = 0)
            where TBone : ITransformMappings<TTf>
            where TTf : ITransformProxy
        {
            var bodyScale_ = bodyScale == 0
                ? anim.humanScale
                : bodyScale;

            return new BodyMotionOperator<TBone, TTf>
            {
                bodyScale = bodyScale_,

                bone = bone,
            };
        }

    }

}
