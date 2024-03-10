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
        //        drot.AsQuaternion().ToAngleAxis(out var angle, out var axis);// math 使用になおしたい
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


    public enum VmdFootIkMode
    {
        auto,
        on,
        off,
    }

    public static class VmdJobExtension
    {


        public static VmdAnimationJob<TPFinder, TRFinder> create<TPFinder, TRFinder>(
            this Animator anim, TransformHandleMappings bone, TPFinder pkf, TRFinder rkf, StreamingTimer timer, VmdFootIkMode footIkMode = VmdFootIkMode.auto, float bodyScale = 0)
                where TPFinder : struct, IKeyFinderWithoutProcedure<float4>
                where TRFinder : struct, IKeyFinderWithoutProcedure<quaternion>
        {

            var useIk = footIkMode switch
            {
                VmdFootIkMode.on => (pos: true, rot: true),
                VmdFootIkMode.off => (pos: false, rot: false),
                _ => getUseIk_(),
            };

            anim.BindStreamTransform(anim.transform);// バインドしないと rootMotionPosition が取得できない様子

            return new VmdAnimationJob<TPFinder, TRFinder>
            {
                pkf = pkf,
                rkf = rkf,

                timer = timer,
                indexBlockTime = rkf.IndexBlockTimeRange,
                
                useLegPositionIk = useIk.pos,
                useFootRotationIk = useIk.rot,

                body = anim.ToVmdBodyMotionOperator<TransformHandleMappings, TfHandle>(bone, bodyScale),

                foot = anim.ToFootIkOperator<TransformHandleMappings, TfHandle>(bone, bodyScale),
            };


            (bool pos, bool rot) getUseIk_()
            {
                var kneeRotLengthL = rkf.Streams.Sections[(int)MmdBodyBones.左ひざ].length;
                var ankleRotLengthL = rkf.Streams.Sections[(int)MmdBodyBones.左足首].length;
                var footIkLengthL = pkf.Streams.Sections[(int)MmdBodyBones.左足ＩＫ].length;

                var kneeRotLengthR = rkf.Streams.Sections[(int)MmdBodyBones.右ひざ].length;
                var ankleRotLengthR = rkf.Streams.Sections[(int)MmdBodyBones.右足首].length;
                var footIkLengthR = pkf.Streams.Sections[(int)MmdBodyBones.右足ＩＫ].length;

                var usePosIk1 =
                    kneeRotLengthL < 3 & footIkLengthL > 2
                    &
                    kneeRotLengthR < 3 & footIkLengthR > 2;

                var useRotIk1 =
                    ankleRotLengthL < 3 & footIkLengthL > 2
                    &
                    ankleRotLengthR < 3 & footIkLengthR > 2;

                var useIk2 =
                    ankleRotLengthL < footIkLengthL
                    &
                    ankleRotLengthR < footIkLengthR;

                return (usePosIk1 | useIk2, useRotIk1 | useIk2);
            }
        }
    }

}
