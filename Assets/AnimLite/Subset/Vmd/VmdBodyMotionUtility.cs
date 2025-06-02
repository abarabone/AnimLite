using AnimLite.IK;
using AnimLite.Utility;
using Unity.Mathematics;
using UnityEngine;

namespace AnimLite.Vmd
{

    static public class BodyMotionExtension
    {

        public static void SetLocalMotions<TPFinder, TRFinder>(
            this VmdBodyMotionOperator<TransformMappings, Tf> op, TPFinder pkf, TRFinder rkf)
                where TPFinder : IKeyFinder<float4>
                where TRFinder : IKeyFinder<quaternion>
        =>
            op.SetLocalMotions(new Tf.StreamSource(), pkf, rkf);


        public static void SetLocalMotions<TPFinder, TRFinder, TBone, TTf, TStream>(
            this VmdBodyMotionOperator<TBone, TTf> op, TStream stream, TPFinder pkf, TRFinder rkf)
                where TPFinder : IKeyFinder<float4>
                where TRFinder : IKeyFinder<quaternion>
                where TBone : ITransformMappings<TTf>
                where TTf : ITransformProxy<TStream>
                where TStream : ITransformStreamSource
        {
            op.bones.SetLocal<TPFinder, TRFinder, TBone, TTf, TStream>(
                0, pkf, rkf, stream, op.moveScale);

            op.bones.SetHipLocal<TPFinder, TRFinder, TBone, TTf, TStream>(
                1, pkf, rkf, stream, op.bodyScale, op.rootToHipLocal, op.spineToHipLocal);

            for (var i = 2; i < op.bones.BoneLength; i++)
            {
                op.bones.SetLocalRotation<TRFinder, TBone, TTf, TStream>(i, rkf, stream);
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
            var map = unit.human.ToVmd();

            var _lrot = rkf.AccumulateStreamRotation(unit.option, map.HumanBoneId, map.StreamId);

            var lrot = unit.initpose.RotateBone(_lrot);
            map.TransformHandle.SetLocalRotation(stream, lrot);
        }

        public static void SetLocal<TPFinder, TRFinder, TBone, TTf, TStream>(
            this TBone bone, int i, TPFinder pkf, TRFinder rkf, TStream stream, float3 scale)
                where TPFinder : IKeyFinder<float4>
                where TRFinder : IKeyFinder<quaternion>
                where TBone : ITransformMappings<TTf>
                where TTf : ITransformLocalPositionProxy<TStream>, ITransformeLocalRotationProxy<TStream>
                where TStream : ITransformStreamSource
        {
            var unit = bone[i];
            var map = unit.human.ToVmd();

            var _lrot = rkf.AccumulateStreamRotation(unit.option, map.HumanBoneId, map.StreamId);
            var _lpos = pkf.AccumulateStreamPosition(rkf, map.HumanBoneId);

            var lrot = unit.initpose.RotateBone(_lrot);
            var lpos = _lpos * scale;
            //map.TransformHandle.SetLocalRotation(stream, lrot);
            //map.TransformHandle.SetLocalPosition(stream, lpos);
            map.TransformHandle.SetLocalRotation(stream, lrot);
            map.TransformHandle.SetLocalPosition(stream, lpos);
        }

        public static void SetHipLocal<TPFinder, TRFinder, TBone, TTf, TStream>(
            this TBone bone, int i, TPFinder pkf, TRFinder rkf, TStream stream,
            float3 bodyScale, float3 rootToHipLocal, float3 spineToHipLocal)
                where TPFinder : IKeyFinder<float4>
                where TRFinder : IKeyFinder<quaternion>
                where TBone : ITransformMappings<TTf>
                where TTf : ITransformLocalPositionProxy<TStream>, ITransformeLocalRotationProxy<TStream>
                where TStream : ITransformStreamSource
        {
            var unit = bone[i];
            var map = unit.human.ToVmd();

            var _lrot = rkf.AccumulateStreamRotation(unit.option, map.HumanBoneId, map.StreamId);
            var _lpos = pkf.AccumulateStreamPosition(rkf, map.HumanBoneId);
            //var hipHeight = rootToHipLocal;
            //var hipAdjust = math.rotate(_lrot, spineToHipLocal).AsXZ();

            var lrot = unit.initpose.RotateBone(_lrot);
            var hipHeight = rootToHipLocal;
            var hipAdjust = math.rotate(lrot, spineToHipLocal).AsXoZ();
            var lpos = _lpos * bodyScale + hipHeight + hipAdjust;
            //var lpos = _lpos + hipHeight + hipAdjust;
            map.TransformHandle.SetLocalRotation(stream, lrot);
            map.TransformHandle.SetLocalPosition(stream, lpos);
        }




    }
}

namespace AnimLite.Vmd.world
{


    static public class BodyMotionExtension
    {

        //public static void SetLocalMotions<TPFinder, TRFinder>(
        //    this VmdBodyMotionOperator<TransformMappings, Tf> op, TPFinder pkf, TRFinder rkf)
        //        where TPFinder : IKeyFinder<float4>
        //        where TRFinder : IKeyFinder<quaternion>
        //=>
        //    op.SetLocalMotions(pkf, rkf, new Tf.StreamSource());


        //public static void SetLocalMotions<TPFinder, TRFinder, TBone, TTf, TStream>(
        //    this VmdBodyMotionOperator<TBone, TTf> op, TPFinder pkf, TRFinder rkf, TStream stream)
        //        where TPFinder : IKeyFinder<float4>
        //        where TRFinder : IKeyFinder<quaternion>
        //        where TBone : ITransformMappings<TTf>
        //        where TTf : ITransformProxy
        //        where TStream : ITransformStreamSource<TTf>
        //{
        //    op.bone.SetLocal<TPFinder, TRFinder, TBone, TTf, TStream>(
        //        0, pkf, rkf, stream, op.bodyScale);

        //    op.bone.SetHipLocal<TPFinder, TRFinder, TBone, TTf, TStream>(
        //        1, pkf, rkf, stream, op.bodyScale, op.rootToHipLocal, op.spineToHipLocal);

        //    for (var i = 2; i < op.bone.BoneLength; i++)
        //    {
        //        op.bone.SetLocalRotation<TRFinder, TBone, TTf, TStream>(i, rkf, stream);
        //    }
        //}
    }

    static class BodyTransExtension
    {

        //public static void SetWorldRotation<TRFinder, TBone, TTf, TStream>(
        //    this TBone bone, int i, TRFinder rkf, TStream stream, quaternion rotParent)
        //        where TRFinder : IKeyFinder<quaternion>
        //        where TBone : ITransformMappings<TTf>
        //        where TTf : ITransformWorldPositionProxy<TStream>, ITransformWorldRotationProxy<TStream>
        //        where TStream : ITransformStreamSource
        //{
        //    var unit = bone[i];
        //    var map = unit.human.ToVmd();

        //    var _lrot = rkf.AccumulateStreamRotation(unit.option, map.HumanBoneId, map.StreamId);
        //    var lrot = unit.initpose.RotateBone(_lrot);

        //    var wrot = math.mul(lrot, rotParent);

        //    map.TransformHandle.SetRotation(stream, wrot);
        //}

        //public static void SetWorld<TPFinder, TRFinder, TBone, TTf, TStream>(
        //    this TBone bone, int i, TPFinder pkf, TRFinder rkf, TStream stream, float bodyScale)
        //        where TPFinder : IKeyFinder<float4>
        //        where TRFinder : IKeyFinder<quaternion>
        //        where TBone : ITransformMappings<TTf>
        //        where TTf : ITransformProxy<TStream>
        //        where TStream : ITransformStreamSource
        //{
        //    var unit = bone[i];
        //    var map = unit.human.ToVmd();

        //    var _lrot = rkf.AccumulateStreamRotation(unit.option, map.HumanBoneId, map.StreamId);
        //    var _lpos = pkf.AccumulateStreamPosition(rkf, map.HumanBoneId);

        //    var lrot = unit.initpose.RotateBone(_lrot);
        //    var lpos = _lpos * bodyScale;
        //    map.TransformHandle.SetLocalRotation(stream, lrot);
        //    map.TransformHandle.SetLocalPosition(stream, lpos);
        //}

        //public static void SetHipWorld<TPFinder, TRFinder, TBone, TTf, TStream>(
        //    this TBone bone, int i, TPFinder pkf, TRFinder rkf, TStream stream,
        //    float bodyScale, float3 rootToHipLocal, float3 spineToHipLocal)
        //        where TPFinder : IKeyFinder<float4>
        //        where TRFinder : IKeyFinder<quaternion>
        //        where TBone : ITransformMappings<TTf>
        //        where TTf : ITransformLocalPositionProxy<TStream>, ITransformeLocalRotationProxy<TStream>
        //        where TStream : ITransformStreamSource
        //{
        //    var unit = bone[i];
        //    var map = unit.human.ToVmd();

        //    var _lrot = rkf.AccumulateStreamRotation(unit.option, map.HumanBoneId, map.StreamId);
        //    var _lpos = pkf.AccumulateStreamPosition(rkf, map.HumanBoneId);
        //    var hipHeight = rootToHipLocal;
        //    var hipAdjust = math.rotate(_lrot, spineToHipLocal).AsXZ();

        //    var lrot = unit.initpose.RotateBone(_lrot);
        //    var lpos = _lpos * bodyScale + hipHeight + hipAdjust;
        //    map.TransformHandle.SetLocalRotation(stream, lrot);
        //    map.TransformHandle.SetLocalPosition(stream, lpos);
        //}




    }
}