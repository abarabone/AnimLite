using AnimLite.Utility;
using Unity.Mathematics;

namespace AnimLite.Vmd
{


    static public class BodyMotionExtension
    {


        public static void SetLocalRotation<TRFinder, TBone, TTf, TStream>(
            this TBone bone, int i, TRFinder rkf, TStream stream)
                where TRFinder : IKeyFinder<quaternion>
                where TBone : ITransformMappings<TTf>
                where TTf : ITransformProxy
                where TStream : ITransformStreamSource<TTf>
        {
            var unit = bone[i];
            var map = unit.human.ToVmd();

            var _lrot = rkf.AccumulateStreamRotation(unit.option, map.HumanBoneId, map.StreamId);
            var lrot = unit.initpose.RotateBone(_lrot);

            stream.SetLocalRotation(map.TransformHandle, lrot);
        }

        public static void SetLocal<TPFinder, TRFinder, TBone, TTf, TStream>(
            this TBone bone, int i, TPFinder pkf, TRFinder rkf, TStream stream, float bodyScale)
                where TPFinder : IKeyFinder<float4>
                where TRFinder : IKeyFinder<quaternion>
                where TBone : ITransformMappings<TTf>
                where TTf : ITransformProxy
                where TStream : ITransformStreamSource<TTf>
        {
            var unit = bone[i];
            var map = unit.human.ToVmd();

            var _lrot = rkf.AccumulateStreamRotation(unit.option, map.HumanBoneId, map.StreamId);
            var _lpos = pkf.AccumulateStreamPosition(rkf, map.HumanBoneId);

            var lrot = unit.initpose.RotateBone(_lrot);
            var lpos = _lpos * bodyScale;
            stream.SetLocalRotation(map.TransformHandle, lrot);
            stream.SetLocalPosition(map.TransformHandle, lpos);
        }

        public static void SetHipLocal<TPFinder, TRFinder, TBone, TTf, TStream>(
            this TBone bone, int i, TPFinder pkf, TRFinder rkf, TStream stream,
            float bodyScale, float3 rootToHipLocal, float3 spineToHipLocal)
                where TPFinder : IKeyFinder<float4>
                where TRFinder : IKeyFinder<quaternion>
                where TBone : ITransformMappings<TTf>
                where TTf : ITransformProxy
                where TStream : ITransformStreamSource<TTf>
        {
            var unit = bone[i];
            var map = unit.human.ToVmd();

            var _lrot = rkf.AccumulateStreamRotation(unit.option, map.HumanBoneId, map.StreamId);
            var _lpos = pkf.AccumulateStreamPosition(rkf, map.HumanBoneId);
            var hipHeight = rootToHipLocal;
            var hipAdjust = math.rotate(_lrot, spineToHipLocal).AsXZ();

            var lrot = unit.initpose.RotateBone(_lrot);
            var lpos = _lpos * bodyScale + hipHeight + hipAdjust;
            stream.SetLocalRotation(map.TransformHandle, lrot);
            stream.SetLocalPosition(map.TransformHandle, lpos);
        }




        public static void SetLocalMotions<TPFinder, TRFinder>(
            this VmdBodyMotionOperator<TransformMappings, Tf> op, TPFinder pkf, TRFinder rkf)
                where TPFinder : IKeyFinder<float4>
                where TRFinder : IKeyFinder<quaternion>
        =>
            op.SetLocalMotions(pkf, rkf, new Tf.StreamSource());


        public static void SetLocalMotions<TPFinder, TRFinder, TBone, TTf, TStream>(
            this VmdBodyMotionOperator<TBone, TTf> op, TPFinder pkf, TRFinder rkf, TStream stream)
                where TPFinder : IKeyFinder<float4>
                where TRFinder : IKeyFinder<quaternion>
                where TBone : ITransformMappings<TTf>
                where TTf : ITransformProxy
                where TStream : ITransformStreamSource<TTf>
        {
            op.bone.SetLocal<TPFinder, TRFinder, TBone, TTf, TStream>(
                0, pkf, rkf, stream, op.bodyScale);

            op.bone.SetHipLocal<TPFinder, TRFinder, TBone, TTf, TStream>(
                1, pkf, rkf, stream, op.bodyScale, op.rootToHipLocal, op.spineToHipLocal);

            for (var i = 2; i < op.bone.BoneLength; i++)
            {
                op.bone.SetLocalRotation<TRFinder, TBone, TTf, TStream>(i, rkf, stream);
            }
        }


    }
}
