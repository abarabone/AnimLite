using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.UIElements;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using UnityEngine.Jobs;

namespace AnimLite.Vmd.experimental
{
    using AnimLite.Vmd;
    using AnimLite.Vmd.experimental.Job;
    using AnimLite.Vmd.experimental.Data;
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;




    public class JobBuffers<TPFinder, TRFinder> : IDisposable//INativeDisposable
        where TPFinder : unmanaged, IKeyFinderWithoutProcedure<float4>
        where TRFinder : unmanaged, IKeyFinderWithoutProcedure<quaternion>
    {

        public NativeArray<ModelFinder<TPFinder, TRFinder>> model_finders_origin;

        public NativeArray<ModelFinderReference<TPFinder, TRFinder>> model_finders;
        public NativeArray<ModelBoneOption> model_boneOptions;
        public NativeArray<ModelHipBoneAdjust> model_hipAdjusts;
        public NativeArray<ModelTimer> model_timer;
        public NativeArray<ModelProcedureSelector> model_procedureSelectors;

        public NativeArray<BoneIndexData> bonefull_rotIndices;
        public NativeArray<BoneRotationOffsetPose> bonefull_rotOffsets;
        public NativeArray<BodyBoneLocalRotationResult> bonefull_rotResults;

        public TransformAccessArray bonefull_transforms;
        public NativeArray<BoneTransformApplyIndex> bonefull_transformApplyIndices;

        public NativeArray<BoneIndexData> boneroothip_posIndices;
        public NativeArray<BodyBoneScale> boneroothip_posScales;
        public NativeArray<BodyBoneLocalPositionResult> boneroothip_posResults;

        public NativeSlice<BodyBoneLocalPositionResult> bodyhip_posResults;


        public TransformAccessArray ikalways_baseTransforms;
        public NativeArray<IkBaseTransformValue> ikalways_baseTransformValues;

        public NativeArray<SolveIkAnchorIndex> ikalways_ikAnchorIndices;
        public NativeArray<SolveIkTransformValueSet> ikalways_legTransformValueSets;
        public TransformAccessArray ikalways_legTransformValues;

        public NativeArray<LegIkAnchorLR> legalways_ikAnchors;
        public NativeArray<FootIkAnchorLR> footalways_ikAnchors;

        public NativeArray<LegIkData> ikleg_ikData;
        public NativeArray<LegIkAnchorIndex> noikleg_ikIndices;
        public TransformAccessArray noikleg_footTransforms;
        
        public NativeArray<FootIkData> ikfoot_ikData;
        public NativeArray<FootIkAnchorIndex> noikfoot_ikIndices;
        public TransformAccessArray noikfoot_footTransforms;

        public NativeArray<LegHitCastCommandLR> ground_hitCastCommands;
        public NativeArray<LegHitRaycastHitLR> ground_hits;
        public NativeArray<LegHitData> ground_hitData;
        public NativeArray<LegHitRootHeightStorage> ground_hitHeightStorages;
        public TransformAccessArray ground_rootTransforms;



        public void Dispose()
        {
            this.model_finders_origin.Dispose();

            this.model_finders.Dispose();
            this.model_boneOptions.Dispose();
            this.model_timer.Dispose();
            this.model_hipAdjusts.Dispose();
            this.model_procedureSelectors.Dispose();

            this.bonefull_rotIndices.Dispose();
            this.bonefull_rotOffsets.Dispose();
            this.bonefull_rotResults.Dispose();
            this.bonefull_transforms.Dispose();
            this.bonefull_transformApplyIndices.Dispose();

            this.boneroothip_posIndices.Dispose();
            this.boneroothip_posScales.Dispose();
            this.boneroothip_posResults.Dispose();

            if (this.ikalways_baseTransforms.isCreated)
            {
                this.ikalways_baseTransforms.Dispose();
                this.ikalways_baseTransformValues.Dispose();
                this.ikalways_ikAnchorIndices.Dispose();
                this.ikalways_legTransformValueSets.Dispose();
                this.ikalways_legTransformValues.Dispose();
                this.legalways_ikAnchors.Dispose();
                this.footalways_ikAnchors.Dispose();
            }

            if (this.ikleg_ikData.IsCreated)
            {
                this.ikleg_ikData.Dispose();
            }
            if (this.ikfoot_ikData.IsCreated)
            {
                this.ikfoot_ikData.Dispose();
            }
            if (this.ground_hitData.IsCreated)
            {
                this.ground_hitCastCommands.Dispose();
                this.ground_hits.Dispose();
                this.ground_hitData.Dispose();
                this.ground_hitHeightStorages.Dispose();
                this.ground_rootTransforms.Dispose();
            }
            if (this.noikleg_footTransforms.isCreated)
            {
                this.noikleg_footTransforms.Dispose();
                this.noikleg_ikIndices.Dispose();
            }
            if (this.noikfoot_footTransforms.isCreated)
            {
                this.noikfoot_footTransforms.Dispose();
                this.noikfoot_ikIndices.Dispose();
            }
        }
    }



    public static class JobNativeBufferExtension
    {

        public static IEnumerable<U> ConvertParam<T, U>(this IEnumerable<T> src, Func<T, IEnumerable<U>> f) =>
            from x in src
            from y in f(x)
            select y;

        public static IEnumerable<V> ConvertParam<T, U, V>(this (IEnumerable<T> param, IEnumerable<U> opt) src, Func<T, U, IEnumerable<V>> f) =>
            from x in src.param.Zip(src.opt, (src, opt) => (src, opt))
            from y in f(x.src, x.opt)
            select y;



        public unsafe static JobBuffers<TPFinder, TRFinder> BuildJobBuffers<TPFinder, TRFinder>(
            this IEnumerable<ModelParams<TPFinder, TRFinder>> paramlist, IEnumerable<ParamCount> countlist)
                where TPFinder : unmanaged, IKeyFinderWithoutProcedure<float4>
                where TRFinder : unmanaged, IKeyFinderWithoutProcedure<quaternion>
        {
            var buffers = new JobBuffers<TPFinder, TRFinder>();
            var alloc = Allocator.Persistent;


            buffers.model_finders_origin = to_(p => p.model_finders_origin);
            buffers.model_finders = Enumerable.Range(0, buffers.model_finders_origin.Length)
                .Select(i =>
                {
                    var p = (ModelFinder<TPFinder, TRFinder>*)buffers.model_finders_origin.GetUnsafePtr() + i;

                    return new ModelFinderReference<TPFinder, TRFinder>
                    {
                        p_pos = (TPFinder*)UnsafeUtility.AddressOf(ref p->pos),
                        p_rot = (TRFinder*)UnsafeUtility.AddressOf(ref p->rot),
                    };
                })
                .ToNativeArray(alloc);
            buffers.model_boneOptions = to_(p => p.model_boneOptions);
            buffers.model_hipAdjusts = count_to_((p, c) => p.model_hipAdjusts.invoke(c));
            buffers.model_timer = to_(p => p.model_timeOptions);
            buffers.model_procedureSelectors = create_<ModelProcedureSelector>(buffers.model_finders.Length);


            //if (paramlist.SelectMany(p => p.ikalways_baseTransforms).Any())
            {
                buffers.ikalways_baseTransforms = tf_to_(p => p.ikalways_baseTransforms);
                buffers.ikalways_baseTransformValues = create_<IkBaseTransformValue>(buffers.ikalways_baseTransforms.length);
                buffers.ikalways_ikAnchorIndices = count_to_((p, c) => p.ikalways_ikAnchorIndices.invoke(c));
                buffers.ikalways_legTransformValueSets = create_<SolveIkTransformValueSet>(buffers.ikalways_ikAnchorIndices.Length);
                buffers.ikalways_legTransformValues = tf_to_(p => p.ikalways_legTransforms);
            }

            //if (paramlist.SelectMany(p => p.ikleg_ikData.invoke(default)).Any())
            {
                buffers.ikleg_ikData = count_to_((p, c) => p.ikleg_ikData.invoke(c));
            }
            //if (paramlist.Any(p => p.noikleg_footTransforms is not null))
            {
                buffers.noikleg_ikIndices = count_to_((p, c) => p.noikleg_ikIndices.invoke(c));
                buffers.noikleg_footTransforms = tf_to_(p => p.noikleg_footTransforms);
            }
            buffers.legalways_ikAnchors = create_<LegIkAnchorLR>(
                buffers.ikleg_ikData.Length + (buffers.noikleg_ikIndices.Length >> 1));

            //if (paramlist.Any(p => p.ikfoot_ikData.invoke is not null))
            {
                buffers.ikfoot_ikData = count_to_((p, c) => p.ikfoot_ikData.invoke(c));
            }
            //if (paramlist.Any(p => p.noikfoot_footTransforms is not null))
            {
                buffers.noikfoot_ikIndices = count_to_((p, c) => p.noikfoot_ikIndices.invoke(c));
                buffers.noikfoot_footTransforms = tf_to_(p => p.noikfoot_footTransforms);
            }
            buffers.footalways_ikAnchors = create_<FootIkAnchorLR>(
                buffers.ikfoot_ikData.Length + (buffers.noikfoot_ikIndices.Length >> 1));

            //if (paramlist.Any(p => p.ground_hitData.invoke is not null))
            {
                buffers.ground_hitData = count_to_((p, c) => p.ground_hitData.invoke(c));
                buffers.ground_hitCastCommands = create_<LegHitCastCommandLR>(buffers.ground_hitData.Length);
                buffers.ground_hits = create_<LegHitRaycastHitLR>(buffers.ground_hitData.Length);
                buffers.ground_hitHeightStorages = to_(p => p.ground_rootHeights);
                buffers.ground_rootTransforms = tf_to_(p => p.ground_rootTransforms);
            }


            buffers.bonefull_rotIndices = count_to_((p, c) => p.bonefull_rotIndices.invoke(c));
            buffers.bonefull_rotOffsets = to_(p => p.bonefull_rotOffsets);
            buffers.bonefull_rotResults = create_<BodyBoneLocalRotationResult>(buffers.bonefull_rotIndices.Length);
            buffers.bonefull_transforms = tf_to_(p => p.bonefull_transgorms);
            buffers.bonefull_transformApplyIndices = count_to_((p, c) => p.bonefull_transformApplyIndices.invoke(c));

            buffers.boneroothip_posIndices = concat_count_to_(
                (p, c) => p.boneroot_posIndices.invoke(c),
                (p, c) => p.bonehip_posIndices.invoke(c));
            buffers.boneroothip_posScales = concat_to_(
                p => p.boneroot_posScales,
                p => p.bonehip_posScales);
            buffers.boneroothip_posResults =
                create_<BodyBoneLocalPositionResult>(buffers.boneroothip_posIndices.Length);


            var counter = countlist.First();
            buffers.bodyhip_posResults = buffers.boneroothip_posResults
                .Slice(counter.model_total_length, counter.model_total_length);


            return buffers;



            NativeArray<U> to_<U>(Func<ModelParams<TPFinder, TRFinder>, IEnumerable<U>> f) where U : unmanaged =>
                paramlist
                    .ConvertParam(f)
                    .ToNativeArray(alloc);

            NativeArray<U> concat_to_<U>(
                Func<ModelParams<TPFinder, TRFinder>, IEnumerable<U>> f1,
                Func<ModelParams<TPFinder, TRFinder>, IEnumerable<U>> f2) where U : unmanaged
            =>
                Enumerable.Concat(paramlist.ConvertParam(f1), paramlist.ConvertParam(f2))
                    .ToNativeArray(alloc);

            NativeArray<U> count_to_<U>(Func<ModelParams<TPFinder, TRFinder>, ParamCount, IEnumerable<U>> f) where U : unmanaged =>
                (paramlist, countlist)
                    .ConvertParam(f)
                    .ToNativeArray(alloc);

            NativeArray<U> concat_count_to_<U>(
                Func<ModelParams<TPFinder, TRFinder>, ParamCount, IEnumerable<U>> f1,
                Func<ModelParams<TPFinder, TRFinder>, ParamCount, IEnumerable<U>> f2) where U : unmanaged
            =>
                Enumerable.Concat((paramlist, countlist).ConvertParam(f1), (paramlist, countlist).ConvertParam(f2))
                    .ToNativeArray(alloc);

            NativeArray<T> create_<T>(int length) where T : unmanaged =>
                new NativeArray<T>(length, alloc);

            TransformAccessArray tf_to_(Func<ModelParams<TPFinder, TRFinder>, IEnumerable<Transform>> f) =>
                new TransformAccessArray(paramlist.ConvertParam(f).ToArray());
        }




    }





}
