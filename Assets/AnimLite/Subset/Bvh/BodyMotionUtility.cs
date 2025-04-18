﻿using AnimLite.Utility;
using AnimLite.Vmd;
using Unity.Mathematics;

namespace AnimLite
{


    static public class BodyMotionExtension
    {

        public static void SetLocalMotions<TPFinder, TRFinder>(
            this BodyMotionOperator<TransformMappings, Tf> op, TPFinder pkf, TRFinder rkf)
                where TPFinder : IKeyFinder<float4>
                where TRFinder : IKeyFinder<quaternion>
        =>
            op.SetLocalMotions(pkf, rkf, new Tf.StreamSource());


        public static void SetLocalMotions<TPFinder, TRFinder, TBone, TTf, TStream>(
            this BodyMotionOperator<TBone, TTf> op, TPFinder pkf, TRFinder rkf, TStream stream)
                where TPFinder : IKeyFinder<float4>
                where TRFinder : IKeyFinder<quaternion>
                where TBone : ITransformMappings<TTf>
                where TTf : ITransformProxy<TStream>
                where TStream : ITransformStreamSource
        {
            op.bone.SetLocal<TPFinder, TRFinder, TBone, TTf, TStream>(
                0, pkf, rkf, stream, op.bodyScale);

            for (var i = 1; i < op.bone.BoneLength; i++)
            {
                op.bone.SetLocalRotation<TRFinder, TBone, TTf, TStream>(i, rkf, stream);
            }
        }
    }

    static class BodyTransExtension
    {

        public static void SetLocalRotation<TRFinder, TBone, TTf, TStream>(
            this TBone bone, int i, TRFinder rkf, TStream stream)
                where TRFinder : IKeyFinder<quaternion>
                where TBone : ITransformMappings<TTf>
                where TTf : ITransformeLocalRotationProxy<TStream>
                where TStream : ITransformStreamSource
        {
            var unit = bone[i];
            var map = unit.human;

            var _lrot = rkf.AccumulateStreamRotation(unit.option, map.HumanBoneId);//, map.StreamId);
            var lrot = unit.initpose.RotateBone(_lrot);

            map.TransformHandle.SetLocalRotation(stream, lrot);
        }

        public static void SetLocal<TPFinder, TRFinder, TBone, TTf, TStream>(
            this TBone bone, int i, TPFinder pkf, TRFinder rkf, TStream stream, float bodyScale)
                where TPFinder : IKeyFinder<float4>
                where TRFinder : IKeyFinder<quaternion>
                where TBone : ITransformMappings<TTf>
                where TTf : ITransformeLocalRotationProxy<TStream>, ITransformLocalPositionProxy<TStream>
                where TStream : ITransformStreamSource
        {
            var unit = bone[i];
            var map = unit.human;

            var _lrot = rkf.AccumulateStreamRotation(unit.option, map.HumanBoneId);//, map.StreamId);
            var _lpos = pkf.AccumulateStreamPosition(rkf, map.HumanBoneId);

            var lrot = unit.initpose.RotateBone(_lrot);
            var lpos = _lpos * bodyScale;
            map.TransformHandle.SetLocalRotation(stream, lrot);
            map.TransformHandle.SetLocalPosition(stream, lpos);
        }




    }
}
