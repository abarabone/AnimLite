using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;

namespace AnimLite.Vmd
{

    using AnimLite.IK;


    public interface IVmdAnimationJob
    {
        void UpdateTimer(float currentTime);
    }


    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct VmdAnimationJob<TPFinder, TRFinder> : IAnimationJob, IVmdAnimationJob
        where TPFinder : struct, IKeyFinderWithoutProcedure<float4>
        where TRFinder : struct, IKeyFinderWithoutProcedure<quaternion>
    {

        public TPFinder pkf;
        public TRFinder rkf;
        public StreamingTimer timer;

        float previousTime;

        [ReadOnly]
        public float indexBlockTime;


        public VmdBodyMotionOperator<TransformHandleMappings, TfHandle> body;
        public FootIkOperator<TfHandle> foot;

        [ReadOnly]
        public bool useLegPositionIk;
        [ReadOnly]
        public bool useFootRotationIk;




        public void UpdateTimer(float currentTime)
        {
            this.previousTime = this.timer.CurrentTime;

            this.timer.UpdateTime(currentTime);
        }


        //[BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
        public void ProcessRootMotion(AnimationStream stream)
        {
            //rootMotion_(stream);
            //$"{this.timer.CurrentTime}".ShowDebugLog();
        }

        //void rootMotion_(AnimationStream stream)
        //{
        //    var map = this.bone.BoneToStreamIndexMappings[0].ToVmd();
        //    var ini = this.bone.InitialPoseRotations[0];

        //    var _lrot = this.rkf.AccumulateStreamRotation(this.bone.OptionalBones, HumanBodyBones.LastBone);
        //    var _lpos = this.pkf.AccumulateStreamPosition(this.rkf, HumanBodyBones.LastBone);

        //    var lrot = ini.RotateBone(_lrot);
        //    var lpos = _lpos * this.bodyScale;
        //    //map.TransformHandle.SetLocalRotation(stream, lrot);
        //    //map.TransformHandle.SetLocalPosition(stream, lpos);

        //    //this.prevLocalPosition = stream.rootMotionPosition;
        //    //this.prevLocalRotation = stream.rootMotionRotation;
        //    var v = calcVelocity_(this.prevLocalPosition, this.prevLocalRotation);
        //    stream.velocity = v.p;
        //    stream.angularVelocity = v.r;

        //    this.prevLocalPosition = lpos;
        //    this.prevLocalRotation = lrot;

        //    (float3 p, float3 r) calcVelocity_(float3 prevlpos, quaternion prevlrot)
        //    {
        //        var rcdt = math.rcp(stream.deltaTime);

        //        var vp = (lpos - prevlpos) * rcdt;

        //        var invprev = math.inverse(prevlrot);
        //        var drot = math.mul(invprev, lrot);
        //        //var angle = math.acos(drot.value.w);// acos(dot(rot, ident))
        //        //var sin = math.sin(angle);
        //        //var axis = math.normalize(drot.value.As_float3());// * math.rcp(sin);
        //        drot.AsQuaternion().ToAngleAxis(out var angle, out var axis);// math égópÇ…Ç»Ç®ÇµÇΩÇ¢
        //        angle = math.degrees(angle);


        //        //var invprev = math.inverse(prevlrot);
        //        //var drot = math.mul(invprev, lrot);
        //        //var axis = drot.value.As_float3();
        //        //var angle = math.lengthsq(drot);

        //        var vr = axis * (angle * rcdt);

        //        return (vp, vr);
        //    }
        //}


        //[BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
        public void ProcessAnimation(AnimationStream stream)
        {
            if (this.timer.CurrentTime >= this.previousTime - this.indexBlockTime)
            {
                var pkf = this.pkf.With<float4, TPFinder, Forward>(this.timer);
                var rkf = this.rkf.With<quaternion, TRFinder, Forward>(this.timer);
                processAnimation_(stream, pkf, rkf);
            }
            else
            {
                //"absolute anim".ShowDebugLog();
                var pkf = this.pkf.With<float4, TPFinder, Absolute>(this.timer);
                var rkf = this.rkf.With<quaternion, TRFinder, Absolute>(this.timer);
                processAnimation_(stream, pkf, rkf);
            }
        }

        void processAnimation_<TPFinder_, TRFinder_>(AnimationStream rawstream, TPFinder_ pkf_, TRFinder_ rkf_)
            where TPFinder_ : struct, IKeyFinder<float4>
            where TRFinder_ : struct, IKeyFinder<quaternion>
        {
            var stream = new TfHandle.StreamSource { stream = rawstream };

            this.body.SetLocalMotions(pkf_, rkf_, stream);

            if (!this.useLegPositionIk) return;
            this.foot.SolveLegPositionIk(stream, pkf_, rawstream.rootMotionPosition, rawstream.rootMotionRotation);

            if (!this.useFootRotationIk) return;
            this.foot.SolveFootRotationIk(stream, rkf_, rawstream.rootMotionPosition, rawstream.rootMotionRotation);
        }
    }



    public static class VmdJobExtension
    {

        public static VmdAnimationJob<TPFinder, TRFinder> create<TPFinder, TRFinder>(
            this Animator anim, TransformHandleMappings bone, TPFinder pkf, TRFinder rkf, StreamingTimer timer)
                where TPFinder : struct, IKeyFinderWithoutProcedure<float4>
                where TRFinder : struct, IKeyFinderWithoutProcedure<quaternion>
        {

            var useIk = true;
                //rkf.Streams.Sections[(int)MmdBodyBones.âEë´éÒ].length < rkf.Streams.Sections[(int)MmdBodyBones.âEë´ÇhÇj].length
                //&&
                //rkf.Streams.Sections[(int)MmdBodyBones.ç∂ë´éÒ].length < rkf.Streams.Sections[(int)MmdBodyBones.ç∂ë´ÇhÇj].length;


            anim.BindStreamTransform(anim.transform);// ÉoÉCÉìÉhÇµÇ»Ç¢Ç∆ rootMotionPosition Ç™éÊìæÇ≈Ç´Ç»Ç¢óléq

            return new VmdAnimationJob<TPFinder, TRFinder>
            {
                pkf = pkf,
                rkf = rkf,

                timer = timer,
                indexBlockTime = rkf.IndexBlockTimeRange,

                // ë´ÇhÇjégópÇ∑ÇÈÅEÇµÇ»Ç¢ÇÃîªï ÇÕÅAÇ¢ÇÎÇ¢ÇÎééçsçˆåÎÇµÇΩÅBäÆëSÇÕÇ»Ç¢ÇæÇÎÇ§Ç»ÅcÅB
                //useLegPositionIk = rkf.Streams.Sections[(int)MmdBodyBones.ç∂Ç–Ç¥].length < 3,
                //useFootRotationIk = rkf.Streams.Sections[(int)MmdBodyBones.ç∂ë´éÒ].length < 3,
                //useLegPositionIk = pkf.Streams.Sections[(int)MmdBodyBones.ç∂ë´ÇhÇj].length > 2,
                //useFootRotationIk = pkf.Streams.Sections[(int)MmdBodyBones.ç∂ë´ÇhÇj].length > 2,
                //useLegPositionIk = rkf.Streams.Sections[(int)MmdBodyBones.ç∂Ç–Ç¥].length < pkf.Streams.Sections[(int)MmdBodyBones.ç∂ë´ÇhÇj].length,
                //useFootRotationIk = rkf.Streams.Sections[(int)MmdBodyBones.ç∂ë´éÒ].length < rkf.Streams.Sections[(int)MmdBodyBones.ç∂ë´ÇhÇj].length,
                useLegPositionIk = useIk,
                useFootRotationIk = useIk,

                body = anim.ToVmdBodyMotionOperator<TransformHandleMappings, TfHandle>(bone),

                foot = anim.ToFootIkOperator<TransformHandleMappings, TfHandle>(bone),
            };
        }
    }

}
