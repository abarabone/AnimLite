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

    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct CopyFootFkToIkAnchorJob : IJobParallelForTransform
    {
        [ReadOnly]
        public NativeArray<FootIkAnchorIndex> noikfoot_anchorIndices;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<FootIkAnchor> footalways_ikAnchors;


        public void Execute(int index_noikfoot, TransformAccess tf)
        {
            var i = this.noikfoot_anchorIndices[index_noikfoot];

            this.footalways_ikAnchors[i.footalways_ikAnchorIndex] = new FootIkAnchor
            {
                footWorldRotation = tf.rotation
            };
        }
    }




    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct IkAnchorToCastCommandJob : IJobParallelFor
    {

        [ReadOnly]
        public NativeArray<LegHitData> ground_hitData;
        [ReadOnly]
        public NativeArray<LegHitInterpolationStorage> ground_rootHeigts;


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
        public NativeArray<LegHitCastCommandLR> ground_hitCastCommands;

        //public GenId.Concurrent genid;


        public void Execute(int index_ground)
        {
            var data = this.ground_hitData[index_ground];
            var rootheight = this.ground_rootHeigts[index_ground].rootLocalHeight;
            
            var up = this.ikalways_baseTransformValues[data.ikalways_index].worldUp.xyz;
            var foots = this.legalways_legIkAnchors[data.legalways_index];

            var offset = (data.rayOriginOffset + rootheight) * up;

            var hitmask = data.hitMask;
            this.ground_hitCastCommands[index_ground] = new LegHitCastCommandLR
            {
                commandL = makeCommand_(foots.legWorldPositionL.xyz),
                commandR = makeCommand_(foots.legWorldPositionR.xyz),
            };

            return;


            RaycastCommand makeCommand_(float3 thispos) =>
                new RaycastCommand
                {
                    from = thispos + offset,
                    direction = -up,
                    distance = data.rayDistance,
                    
                    queryParameters = new QueryParameters
                    {
                        hitBackfaces = false,
                        hitMultipleFaces = false,
                        hitTriggers = QueryTriggerInteraction.Collide,
                        layerMask = hitmask,
                    }
                };
        }
    }




    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct HitApplyToIkAnchorJob : IJobParallelFor
    {

        [ReadOnly]
        public NativeArray<LegHitRaycastHitLR> ground_hits;
        [ReadOnly]
        public NativeArray<LegHitData> ground_hitData;

        // r/w
        public NativeArray<LegHitInterpolationStorage> ground_hitHeightStorages;


        // r/w
        [NativeDisableParallelForRestriction]
        public NativeArray<LegIkAnchorLR> legalways_ikAnchors;

        // r/w
        [NativeDisableParallelForRestriction]
        public NativeArray<FootIkAnchorLR> footalways_ikAnchors;


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
            var rootStorage = this.ground_hitHeightStorages[index_ground];
            var data = this.ground_hitData[index_ground];

            var tfBase = this.ikalways_baseTransformValues[data.ikalways_index];
            var wup = tfBase.worldUp.xyz;
            var base_wpos = tfBase.position.xyz;

            var t = this.model_timer[data.model_index];
            var dt = t.timer.CurrentTime - t.previousTime;


            // ‚¢‚Á‚½‚ñ base local ‚É‚µ‚Ä‚‚³‚ÅŒvŽZ‚µ‚Ä‚¢‚­

            var rootlpos = this.boneroothip_posResults[data.model_index].localPosition.xyz;
            var local_height_root_ofs = rootStorage.rootLocalHeight - rootlpos.y;

            var ikpos = this.legalways_ikAnchors[data.legalways_index];
            var ik_local_height = wpos_to_local_height_(ikpos.legWorldPositionL.xyz, ikpos.legWorldPositionR.xyz);

            var hit = this.ground_hits[index_ground];
            var hit_local_height = wpos_to_local_height_(hit.hitL.point.As_float3(), hit.hitR.point.As_float3());


            var hitid = new int2(hit.hitL.colliderInstanceID, hit.hitR.colliderInstanceID);
            var ankle_height = new float2(data.ankleHightL, data.ankleHightR);
            var ik_height_ankle = ik_local_height + local_height_root_ofs;
            var root_height = new float2(rootStorage.rootLocalHeight, rootStorage.rootLocalHeight);
            var hit_height = hit_local_height;

            var chk = check_();
            var new_local_height = calc_();


            var easeSpeedTarget = math.select(new float4(12.0f, 12.0f, 10.0f, 10.0f), 100.0f, new bool4(!chk.isGrounding/* | chk.isFloating*/, false, false));
            var easeSpeed = easeSpeedTarget;// rootStorage.easeSpeed + (easeSpeedTarget - rootStorage.easeSpeed) * math.saturate(10.0f * dt);
            var easerate = math.saturate(easeSpeed * dt);
            //rootStorage.easeSpeed = easeSpeed;


            // leg
            {
                var new_local_height_leg_eased = rootStorage.footLocalHeightLR +
                    (new_local_height.leg - rootStorage.footLocalHeightLR) * easerate.xy;

                var local_height_leg_move = new_local_height_leg_eased - ik_local_height;
                var new_leg_wpos = height_to_world_pos_(
                    ikpos.legWorldPositionL.xyz,
                    ikpos.legWorldPositionR.xyz,
                    local_height_leg_move);

                this.legalways_ikAnchors[data.legalways_index] = new LegIkAnchorLR
                {
                    legWorldPositionL = new_leg_wpos.l.As4(1.0f),
                    legWorldPositionR = new_leg_wpos.r.As4(1.0f),
                };

                rootStorage.footLocalHeightLR = new_local_height_leg_eased;
            }


            // foot
            {
                var ikfoot = this.footalways_ikAnchors[data.footalways_index];
                var result = new FootIkAnchorLR
                {
                    footWorldRotationL = calc_new_foot_(
                        ikfoot.footWorldRotationL, hit.hitL.normal, chk.isGrounding.x & !chk.isFloating.x, rootStorage.footRotationL, easerate.w),

                    footWorldRotationR = calc_new_foot_(
                        ikfoot.footWorldRotationR, hit.hitR.normal, chk.isGrounding.y & !chk.isFloating.y, rootStorage.footRotationR, easerate.w),
                };
                this.footalways_ikAnchors[data.footalways_index] = result;

                //static quaternion calc_new_foot_(quaternion foot_wrot, float3 hit_normal) =>
                //    quaternion.LookRotation(math.rotate(foot_wrot, Vector3.forward), hit_normal);
                static quaternion calc_new_foot_(quaternion wrot, float3 hit_normal, bool isHit, quaternion wrotprev, float t)
                {
                    return math.select(
                        falseValue:
                            new float4(float.NaN, float.NaN, float.NaN, float.NaN),
                        trueValue:
                            calc_().value,
                        //trueValue:
                        //    math.select(
                        //        math.slerp(wrotprev, calc_(), t).value,
                        //        wrot.value,//calc_().value,
                        //        float.IsNaN(wrotprev.value.x)),
                        test:
                            isHit);

                    quaternion calc_()
                    {
                        var up = math.rotate(wrot, Vector3.up);
                        var rotTo = IkExtension.fromToRotation(up, hit_normal);
                        return math.mul(rotTo, wrot);
                    }
                }

                ////rootStorage.footRotationL = result.footWorldRotationL;
                ////rootStorage.footRotationR = result.footWorldRotationR;
                //rootStorage.footRotationL =
                //    math.select(
                //        result.footWorldRotationL.value,
                //        ikfoot.footWorldRotationL.value,
                //        float.IsNaN(result.footWorldRotationL.value.x));
                //rootStorage.footRotationR =
                //    math.select(
                //        result.footWorldRotationR.value,
                //        ikfoot.footWorldRotationR.value,
                //        float.IsNaN(result.footWorldRotationR.value.x));
            }


            // root
            {
                var new_local_height_root_lower_LorR = math.min(new_local_height.root.x, new_local_height.root.y);
                var new_local_height_root = rootStorage.rootLocalHeight +
                    (new_local_height_root_lower_LorR - rootStorage.rootLocalHeight) * easerate.z;

                this.boneroothip_posResults[data.model_index] = new BodyBoneLocalPositionResult
                {
                    localPosition = new float4(rootlpos.x, new_local_height_root, rootlpos.z, 1.0f),
                };

                rootStorage.rootLocalHeight = new_local_height_root;
            }


            this.ground_hitHeightStorages[index_ground] = rootStorage;

            return;


            float2 wpos_to_local_height_(float3 wposL, float3 wposR)
            {
                var l = math.dot(wposL - base_wpos, wup);
                var r = math.dot(wposR - base_wpos, wup);

                return new float2(l, r);
            }
            (float3 l, float3 r) height_to_world_pos_(float3 wbaseposL, float3 wbaseposR, float2 height)
            {
                var l = wbaseposL + wup * height.x;
                var r = wbaseposR + wup * height.y;

                return (l, r);
            }

            (bool2 isGrounding, bool2 isFloating) check_()
            {
                //var isFloating = ik_height_ankle > root_height + ankle_height;
                //var isFloating = ik_height_ankle > root_height + ankle_height * 0.95f;
                var isFloating = ik_height_ankle > hit_height + ankle_height * 0.95f;
                var isGrounding = hitid != 0;

                return (isGrounding, isFloating);
            }

            // base local
            (float2 leg, float2 root) calc_()
            {
                var newroot_height = math.select(
                    falseValue: root_height,
                    trueValue: hit_height,
                    test: chk.isGrounding);

                var hit_height_ankle = hit_height + ankle_height;
                var ofs = newroot_height - root_height;
                var new_height_ankle = math.select(
                    falseValue: ik_height_ankle + ofs,
                    trueValue: hit_height_ankle,
                    test: !chk.isFloating & chk.isGrounding);

                //Debug.Log($"hit:{hit_height:f3} ank:{ankle_height:f3} ik:{ik_height:f3} rt:{root_height:f3} nh:{new_height:f3} nr:{newroot_height:f3} f:{isFloating} g:{isGrounding} {hit.colliderInstanceID}");
                return (new_height_ankle, newroot_height);
            }
        }

    }


    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct LegIkApplyRootHeightJob : IJobParallelForTransform
    {
        [ReadOnly]
        public NativeArray<LegHitInterpolationStorage> ground_hitHeightStorages;

        public void Execute(int index, TransformAccess tf)
        {
            var lpos = tf.localPosition;
            
            lpos.y = this.ground_hitHeightStorages[index].rootLocalHeight;

            tf.localPosition = lpos;
        }
    }



}
