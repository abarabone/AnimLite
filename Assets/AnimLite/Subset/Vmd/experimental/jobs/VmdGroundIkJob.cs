using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.UIElements;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using UnityEngine.Jobs;

using AnimLite.Vmd.experimental;
using AnimLite;

namespace AnimLite.Vmd.experimental.Job
{
    using AnimLite.Vmd;
    using AnimLite.Vmd.experimental.Data;
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;
    using AnimLite.IK;




    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct CopyLegFkToIkAnchorJob : IJobParallelForTransform
    {
        [ReadOnly]
        public NativeArray<LegIkAnchorIndex> noikleg_anchorIndices;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<LegIkAnchor> legalways_ikAnchors;


        public void Execute(int index_noikleg, TransformAccess tf)
        {
            var i = this.noikleg_anchorIndices[index_noikleg];

            this.legalways_ikAnchors[i.legalways_ikAnchorIndex] = new LegIkAnchor
            {
                legWorldPosition = tf.position.As_float4(1.0f)
            };
        }
    }

    //[BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    //public struct CopyFootFkToIkAnchorJob : IJobParallelForTransform
    //{
    //    [ReadOnly]
    //    public NativeArray<FootIkAnchorIndex> noikfoot_anchorIndices;

    //    [WriteOnly]
    //    [NativeDisableParallelForRestriction]
    //    public NativeArray<FootIkAnchor> footalways_ikAnchors;


    //    public void Execute(int index_noikfoot, TransformAccess tf)
    //    {
    //        var i = this.noikfoot_anchorIndices[index_noikfoot];

    //        this.footalways_ikAnchors[i.footalways_ikAnchorIndex] = new FootIkAnchor
    //        {
    //            footWorldRotation = tf.rotation
    //        };
    //    }
    //}




    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct IkAnchorToCastCommandJob : IJobParallelFor
    {

        [ReadOnly]
        public NativeArray<LegHitData> ground_hitData;
        [ReadOnly]
        public NativeArray<GroundLegInterpolationStorageLR> ground_rootHeigts;


        [ReadOnly]
        public NativeArray<LegIkAnchorLR> legalways_legIkAnchors;
        //[WriteOnly]
        //public NativeArray<LegIkHitIndex> legIkHitIndexs;

        [ReadOnly]
        public NativeArray<IkBaseTransformValue> ikalways_baseTransformValues;


        //[WriteOnly]
        //[NativeDisableParallelForRestriction]
        //public NativeArray<SpherecastCommand> castCommands;
        [WriteOnly]
        public NativeArray<LegGroundcastCommandLR> ground_hitCastCommands;

        //public GenId.Concurrent genid;


        public void Execute(int index_ground)
        {
            var data = this.ground_hitData[index_ground];
            var localGroundHeight = this.ground_rootHeigts[index_ground].localGroundHeight;

            var tfBase = this.ikalways_baseTransformValues[data.ikalways_index];
            var foots = this.legalways_legIkAnchors[data.legalways_index];

            var up = tfBase.worldUp.xyz;
            var scale = tfBase.scale.y;

            var offset = (data.rayOriginOffset + localGroundHeight * scale) * up;

            var hitmask = data.hitMask;
            this.ground_hitCastCommands[index_ground] = new LegGroundcastCommandLR
            {
                commandL = makeCommand_(foots.legWorldPositionL.xyz, data.ankleHightL * scale * 0.5f),
                commandR = makeCommand_(foots.legWorldPositionR.xyz, data.ankleHightR * scale * 0.5f),
            };

            return;


            SpherecastCommand makeCommand_(float3 thispos, float radius) =>
                new SpherecastCommand
                {
                    origin = thispos + offset,
                    radius = radius,
                    direction = -up,
                    distance = data.rayDistance - radius,

                    queryParameters = new QueryParameters
                    {
                        hitBackfaces = false,
                        hitMultipleFaces = false,
                        hitTriggers = QueryTriggerInteraction.Collide,
                        layerMask = hitmask,
                    }
                };
            //RaycastCommand makeCommand_(float3 thispos) =>
            //    new RaycastCommand
            //    {
            //        from = thispos + offset,
            //        direction = -up,
            //        distance = data.rayDistance,

            //        queryParameters = new QueryParameters
            //        {
            //            hitBackfaces = false,
            //            hitMultipleFaces = false,
            //            hitTriggers = QueryTriggerInteraction.Collide,
            //            layerMask = hitmask,
            //        }
            //    };
        }
    }




    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct HitApplyToIkAnchorJob : IJobParallelFor
    {

        [ReadOnly]
        public NativeArray<LegRaycastHitLR> ground_hits;
        [ReadOnly]
        public NativeArray<LegHitData> ground_hitData;

        // r/w
        public NativeArray<GroundLegInterpolationStorageLR> ground_hitHeightStorages;


        // r/w
        [NativeDisableParallelForRestriction]
        public NativeArray<LegIkAnchorLR> legalways_ikAnchors;

        //// r/w
        //[NativeDisableParallelForRestriction]
        //public NativeArray<FootIkAnchorLR> footalways_ikAnchors;


        // r/w
        [NativeDisableParallelForRestriction]
        public NativeArray<BodyBoneLocalPositionResult> boneroothip_posResults;
        //public NativeSlice<BodyBoneLocalPositionResult> hipPositions;

        [ReadOnly]
        public NativeArray<IkBaseTransformValue> ikalways_baseTransformValues;


        [ReadOnly]
        public NativeArray<ModelTimer> model_timer;


        public void Execute(int index_ground)
        {
            var lposStorage = this.ground_hitHeightStorages[index_ground];
            var data = this.ground_hitData[index_ground];

            var tfBase = this.ikalways_baseTransformValues[data.ikalways_index];
            var wup = tfBase.worldUp.xyz;
            var base_wpos = tfBase.position.xyz;

            var base_scale = tfBase.scale.y;
            var base_scale_rec = 1.0f / base_scale;

            var t = this.model_timer[data.model_index];
            var dt = t.timer.CurrentTime - t.previousTime;


            // ‚¢‚Á‚½‚ñ base local ‚É‚µ‚Ä‚‚³‚ÅŒvŽZ‚µ‚Ä‚¢‚­

            //var rootlpos = this.boneroothip_posResults[data.model_index].localPosition.xyz;
            //var local_height_root_ofs = rootStorage.rootLocalHeight - rootlpos.y;

            var ikanchor = this.legalways_ikAnchors[data.legalways_index];
            var ikleg_local_heightLR = wpos_to_local_height_(ikanchor.legWorldPositionL.xyz, ikanchor.legWorldPositionR.xyz);
            var ikleg_local_height_onGroundLR = ikleg_local_heightLR + lposStorage.localGroundHeight;

            var hit = this.ground_hits[index_ground];
            var hit_local_height_onGroundLR = wpos_to_local_height_(hit.hitL.point, hit.hitR.point);

            var hitIdLR = new int2(hit.hitL.colliderInstanceID, hit.hitR.colliderInstanceID);
            var ankle_heightLR = new float2(data.ankleHightL, data.ankleHightR);
            var root_local_height_onGround = lposStorage.localGroundHeight;

            var chkLR = check_();
            var new_root_local_height_onGround = calc_root_();
            var new_leg_local_height_onGroundLR = calc_leg_();


            // easing
            {
                var isRootEasing = new_root_local_height_onGround >= root_local_height_onGround;
                var isLegEasingLR = chkLR.isGrounding;// & !chkLR.isFloating;
                var ease_time_span_reciprocal = math.select(
                    new float3(1f / 0.3f, 1f / 0.01f, 1f / 0.01f) * t.speedHint,
                    new float3(1f / 0.5f, 1f / 0.3f, 1f / 0.3f) * t.speedHint,
                    new bool3(isRootEasing, isLegEasingLR));
                var ease_from = new float3(lposStorage.rootLocalHeight, lposStorage.legLocalHeightLR);
                var ease_to = new float3(new_root_local_height_onGround, new_leg_local_height_onGroundLR);
                var saturation = new float3(1.1f, 1.05f, 1.05f);

                var new_local_height_onGround_eased = ease_out_(ease_from, ease_to, ease_time_span_reciprocal, saturation, dt);

                static float3 ease_out_(float3 from, float3 to, float3 timeSpanR, float3 saturationTop, float timeDelta)
                {
                    //var t = timeDelta * timeSpanR;
                    //return from + (to - from) * math.clamp(t, 0.0f, saturationTop);
                    var t = timeDelta * timeSpanR - 1.0f;
                    return from + (to - from) * math.clamp(1 + t * t * t, 0.0f, saturationTop);
                }

                lposStorage.localGroundHeight = new_root_local_height_onGround;
                lposStorage.rootLocalHeight = new_local_height_onGround_eased.x;
                lposStorage.legLocalHeightLR = new_local_height_onGround_eased.yz;
                this.ground_hitHeightStorages[index_ground] = lposStorage;
            }


            // root
            {
                var rootlpos = this.boneroothip_posResults[data.model_index].localPosition.xyz;
                this.boneroothip_posResults[data.model_index] = new BodyBoneLocalPositionResult
                {
                    localPosition = new float4(rootlpos.x, lposStorage.rootLocalHeight, rootlpos.z, 1.0f),
                };
            }


            // leg
            {
                var local_height_leg_moveLR = lposStorage.legLocalHeightLR - ikleg_local_heightLR;
                var new_leg_wpos = height_to_world_pos_(
                    ikanchor.legWorldPositionL.xyz,
                    ikanchor.legWorldPositionR.xyz,
                    local_height_leg_moveLR);

                this.legalways_ikAnchors[data.legalways_index] = new LegIkAnchorLR
                {
                    legWorldPositionL = new_leg_wpos.l.As4(1.0f),
                    legWorldPositionR = new_leg_wpos.r.As4(1.0f),
                };
            }


            return;


            float2 wpos_to_local_height_(float3 wposL, float3 wposR)
            {
                var l = math.dot(wposL - base_wpos, wup) * base_scale_rec;
                var r = math.dot(wposR - base_wpos, wup) * base_scale_rec;

                return new float2(l, r);
            }
            (float3 l, float3 r) height_to_world_pos_(float3 wbaseposL, float3 wbaseposR, float2 height)
            {
                var l = wbaseposL + wup * height.x * base_scale;
                var r = wbaseposR + wup * height.y * base_scale;

                return (l, r);
            }

            (bool2 isGrounding, bool2 isFloating) check_()
            {
                var isFloating = ikleg_local_height_onGroundLR > hit_local_height_onGroundLR + ankle_heightLR * 0.95f;
                var isGrounding = hitIdLR != 0;

                return (isGrounding, isFloating);
            }

            float calc_root_()
            {
                var new_root_local_height_onGroundLR = math.select(
                    falseValue:
                        root_local_height_onGround,
                    trueValue:
                        hit_local_height_onGroundLR,
                    test:
                        chkLR.isGrounding);
                var new_root_local_height_onGround =
                    math.min(new_root_local_height_onGroundLR.x, new_root_local_height_onGroundLR.y);

                return new_root_local_height_onGround;
            }
            float2 calc_leg_()
            {
                var hitleg_local_height_onGroundLR = hit_local_height_onGroundLR + ankle_heightLR;
                var height_delta = new_root_local_height_onGround - root_local_height_onGround;
                var new_leg_local_height_onGroundLR = math.select(
                    falseValue:
                        ikleg_local_height_onGroundLR + height_delta,
                    trueValue:
                        hitleg_local_height_onGroundLR,
                    test:
                        !chkLR.isFloating & chkLR.isGrounding);
                        //chkLR.isGrounding);

                return new_leg_local_height_onGroundLR;
            }

            //// base local
            //(float2 legLR, float root) calc_()
            //{
            //    var new_root_local_height_onGroundLR = math.select(
            //        falseValue:
            //            root_local_height_onGround,
            //        trueValue:
            //            hit_local_height_onGroundLR,
            //        test:
            //            chkLR.isGrounding);
            //    var new_root_local_height_onGround =
            //        math.min(new_root_local_height_onGroundLR.x, new_root_local_height_onGroundLR.y); 

            //    var hitleg_local_height_onGroundLR = hit_local_height_onGroundLR + ankle_heightLR;
            //    var height_delta = new_root_local_height_onGround - root_local_height_onGround;
            //    var new_leg_local_height_onGroundLR = math.select(
            //        falseValue:
            //            ikleg_local_height_onGroundLR + height_delta,
            //        trueValue:
            //            hitleg_local_height_onGroundLR,
            //        test:
            //            !chkLR.isFloating & chkLR.isGrounding);

            //    //Debug.Log($"hit:{hit_height:f3} ank:{ankle_height:f3} ik:{ik_height:f3} rt:{root_height:f3} nh:{new_height:f3} nr:{newroot_height:f3} f:{isFloating} g:{isGrounding} {hit.colliderInstanceID}");
            //    return (new_leg_local_height_onGroundLR, new_root_local_height_onGround);
            //}
        }

    }


    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct LegIkApplyRootHeightJob : IJobParallelForTransform
    {
        [ReadOnly]
        public NativeArray<GroundLegInterpolationStorageLR> ground_hitHeightStorages;

        public void Execute(int index, TransformAccess tf)
        {
            var lpos = tf.localPosition;
            
            lpos.y = this.ground_hitHeightStorages[index].rootLocalHeight;

            tf.localPosition = lpos;
        }
    }





    // -----------------------------------------------------------------------------------


    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct CopyTransformToFootFkValueJob : IJobParallelForTransform
    {

        [WriteOnly]
        public NativeArray<GroundFootFkTransformValue> ground_footFkTransformValues;


        public void Execute(int index_ground, TransformAccess tf)
        {
            this.ground_footFkTransformValues[index_ground] = new GroundFootFkTransformValue
            {
                footWorldPosition = tf.position.As_float4(1.0f),
                footWorldRotation = tf.rotation,
            };
        }
    }


    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct GroundFootInterpolateJob : IJobParallelFor
    {

        [ReadOnly]
        public NativeArray<ModelTimer> model_timer;

        [ReadOnly]
        public NativeArray<IkBaseTransformValue> ikalways_baseTransformValues;


        [ReadOnly]
        public NativeArray<LegRaycastHitLR> ground_hits;
        [ReadOnly]
        public NativeArray<LegHitData> ground_hitData;
        [ReadOnly]
        public NativeArray<GroundFootFkTransformValueLR> ground_footFkTransformValues;

        // r/w
        public NativeArray<GroundFootInterpolationStorageLR> ground_footStorages;

        [WriteOnly]
        public NativeArray<GroundFootResultValueLR> ground_transformValueResults;


        public void Execute(int index_ground)
        {
            var storage = this.ground_footStorages[index_ground];
            var data = this.ground_hitData[index_ground];
            var tfFoot = this.ground_footFkTransformValues[index_ground];

            var tfBase = this.ikalways_baseTransformValues[data.ikalways_index];
            var wup = tfBase.worldUp.xyz;
            var base_wpos = tfBase.position.xyz;

            var t = this.model_timer[data.model_index];
            var dt = t.timer.CurrentTime - t.previousTime;

            var hit = this.ground_hits[index_ground];
            var hitIdLR = new int2(hit.hitL.colliderInstanceID, hit.hitR.colliderInstanceID);
            var hit_local_heightLR = wpos_to_local_heightLR_(hit.hitL.point.As_float3().xyz, hit.hitR.point.As_float3().xyz);
            

            var isGrounding = check_GroundingLR_();

            var easeSpeed = math.select(100.0f, math.select(6.0f, 12.0f, isGrounding), hitIdLR != 0);
            var et = easeSpeed * dt - 1.0f;
            var easerate = math.saturate(1.0f + et * et * et);


            var result = new GroundFootResultValueLR
            {
                footWorldRotationL = calc_new_foot_(
                    tfFoot.footWorldRotationL, hit.hitL.normal, isGrounding.x, storage.footRotationL, easerate.x),

                footWorldRotationR = calc_new_foot_(
                    tfFoot.footWorldRotationR, hit.hitR.normal, isGrounding.y, storage.footRotationR, easerate.y),
            };
            this.ground_transformValueResults[index_ground] = result;

            this.ground_footStorages[index_ground] = new GroundFootInterpolationStorageLR
            {
                footRotationL = result.footWorldRotationL,
                footRotationR = result.footWorldRotationR,
            };

            return;


            float2 wpos_to_local_heightLR_(float3 wposL, float3 wposR)
            {
                var l = math.dot(wposL - base_wpos, wup);
                var r = math.dot(wposR - base_wpos, wup);

                return new float2(l, r);
            }

            bool2 check_GroundingLR_()
            {
                var ankle_heightLR = new float2(data.ankleHightL, data.ankleHightR);
                var foot_local_heightLR = wpos_to_local_heightLR_(tfFoot.footWorldPositionL.xyz, tfFoot.footWorldPositionR.xyz);

                var legDirL = math.normalize(tfFoot.legWorldPositionL.xyz - tfFoot.footWorldPositionL.xyz);
                var legDirR = math.normalize(tfFoot.legWorldPositionR.xyz - tfFoot.footWorldPositionR.xyz);

                var isVertical = new bool2(
                    math.dot(legDirL, hit.hitL.normal) > 0.5f,// 60“x
                    math.dot(legDirR, hit.hitR.normal) > 0.5f);
                var isFloating = foot_local_heightLR > hit_local_heightLR + ankle_heightLR * 0.95f;
                var isGrounding = hitIdLR != 0;

                return isVertical & isGrounding & !isFloating;
            }

            static quaternion calc_new_foot_(quaternion wrot, float3 hit_normal, bool isGround, quaternion wrotprev, float t)
            {
                //return math.select(
                //    falseValue:
                //        math.slerp(wrotprev, wrot, t).value,
                //    trueValue:
                //        calc_().value,
                //    test:
                //        isGround);
                var wrotnew = math.select(
                    falseValue:
                        wrot.value,
                    trueValue:
                        calc_().value,
                    test:
                        isGround);
                return math.slerp(wrotprev, wrotnew, t);

                quaternion calc_()
                {
                    var up = math.rotate(wrot, Vector3.up);
                    var rotTo = IkExtension.fromToRotation(up, hit_normal);
                    return math.mul(rotTo, wrot);
                }
            }
        }

    }

    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct CopyGroundFootToTransformJob : IJobParallelForTransform
    {

        [ReadOnly]
        public NativeArray<GroundFootResultValue> ground_transformValues;


        public void Execute(int index_ground, TransformAccess tf)
        {
            var value = this.ground_transformValues[index_ground];

            tf.rotation = value.footWorldRotation;
        }
    }

}
