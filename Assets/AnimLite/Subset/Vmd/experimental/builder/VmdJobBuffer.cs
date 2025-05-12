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

        // pre
        public NativeArray<ModelFinder<TPFinder, TRFinder>> model_finders_origin;

        public NativeArray<ModelFinderReference<TPFinder, TRFinder>> model_finders;
        public NativeArray<ModelBoneOption> model_boneOptions;
        public NativeArray<ModelHipBoneAdjust> model_hipAdjusts;
        public NativeArray<ModelTimer> model_timer;
        public NativeArray<ModelProcedureSelector> model_procedureSelectors;


        // body motion
        public NativeArray<BoneIndexData> bonefull_rotIndices;
        public NativeArray<BoneRotationOffsetPose> bonefull_rotOffsets;
        public NativeArray<BodyBoneLocalRotationResult> bonefull_rotResults;

        public TransformAccessArray bonefull_transforms;
        public NativeArray<BoneTransformApplyIndex> bonefull_transformApplyIndices;

        public NativeArray<BoneIndexData> boneroothip_posIndices;
        public NativeArray<BodyBoneScale> boneroothip_posScales;
        public NativeArray<BodyBoneLocalPositionResult> boneroothip_posResults;

        public NativeSlice<BodyBoneLocalPositionResult> bodyhip_posResults;

        // ik
        public TransformAccessArray ikalways_baseTransforms;
        public NativeArray<IkBaseTransformValue> ikalways_baseTransformValues;

        public NativeArray<LegIkData> ikleg_ikData;
        public NativeArray<FootIkData> ikfoot_ikData;

        public NativeArray<LegIkAnchorLR> legalways_ikAnchors;
        public NativeArray<FootIkAnchorLR> ikfoot_ikAnchors;

        // ik : solving
        public NativeArray<SolveIkAnchorIndex> ikalways_ikAnchorIndices;
        public NativeArray<SolveIkTransformValueSet> ikalways_legTransformValueSets;
        public TransformAccessArray ikalways_legTransformValues;


        // grounding
        public NativeArray<LegIkAnchorIndex> noikleg_ikIndices;
        public TransformAccessArray noikleg_footTransforms;
        //public NativeArray<FootIkAnchorIndex> noikfoot_ikIndices;
        //public TransformAccessArray noikfoot_footTransforms;

        // grounding : cast and leg interpolation
        public NativeArray<LegGroundcastCommandLR> ground_castCommands;
        public NativeArray<LegRaycastHitLR> ground_hits;
        public NativeArray<LegHitData> ground_hitData;
        public NativeArray<GroundLegInterpolationStorageLR> ground_hitHeightStorages;
        public TransformAccessArray ground_rootTransforms;

        // grounding : foot interpolation
        public NativeArray<GroundFootInterpolationStorageLR> ground_footStorages;
        public TransformAccessArray ground_footFkTransforms;
        public NativeArray<GroundFootFkTransformValueLR> ground_footFkTransformValues;
        public TransformAccessArray ground_footResultTransforms;
        public NativeArray<GroundFootResultValueLR> ground_footTransformValueResults;



        public void Dispose()
        {
            this.model_finders.Dispose();
            this.model_finders_origin.Dispose();

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

            //if (this.ikalways_baseTransforms.isCreated)
            {
                this.ikalways_baseTransforms.Dispose();
                this.ikalways_baseTransformValues.Dispose();
                this.ikalways_ikAnchorIndices.Dispose();
                this.ikalways_legTransformValueSets.Dispose();
                this.ikalways_legTransformValues.Dispose();
                this.legalways_ikAnchors.Dispose();
                this.ikfoot_ikAnchors.Dispose();
            }

            //if (this.ikleg_ikData.IsCreated)
            {
                this.ikleg_ikData.Dispose();
            }
            //if (this.ikfoot_ikData.IsCreated)
            {
                this.ikfoot_ikData.Dispose();
            }
            //if (this.ground_hitData.IsCreated)
            {
                this.ground_castCommands.Dispose();
                this.ground_hits.Dispose();
                this.ground_hitData.Dispose();
                this.ground_hitHeightStorages.Dispose();
                this.ground_rootTransforms.Dispose();

                this.ground_footStorages.Dispose();
                this.ground_footFkTransforms.Dispose();
                this.ground_footFkTransformValues.Dispose();
                this.ground_footResultTransforms.Dispose();
                this.ground_footTransformValueResults.Dispose();
            }
            //if (this.noikleg_footTransforms.isCreated)
            {
                this.noikleg_footTransforms.Dispose();
                this.noikleg_ikIndices.Dispose();
            }
            //if (this.noikfoot_footTransforms.isCreated)
            //{
            //    this.noikfoot_footTransforms.Dispose();
            //    this.noikfoot_ikIndices.Dispose();
            //}
        }
    }



    public static class JobNativeBufferExtension
    {


        public unsafe static JobBuffers<TPFinder, TRFinder> BuildJobBuffers<TPFinder, TRFinder>(
            this IEnumerable<ModelParams<TPFinder, TRFinder>> paramlist, IEnumerable<ParamCount> countlist)
                where TPFinder : unmanaged, IKeyFinderWithoutProcedure<float4>
                where TRFinder : unmanaged, IKeyFinderWithoutProcedure<quaternion>
        {
            var buffers = new JobBuffers<TPFinder, TRFinder>();
            var alloc = Allocator.Persistent;


            // pre

            buffers.model_finders_origin = to_(p => p.model_finders_origin);
            buffers.model_finders = Enumerable.Range(0, buffers.model_finders_origin.Length())
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
            buffers.model_hipAdjusts = to_with_index_(p => p.model_hipAdjusts);
            buffers.model_timer = to_(p => p.model_timeOptions);
            buffers.model_procedureSelectors = create_<ModelProcedureSelector>(buffers.model_finders.Length());


            // body motion

            buffers.bonefull_rotIndices = to_with_index_(p => p.bonefull_rotIndices);
            buffers.bonefull_rotOffsets = to_(p => p.bonefull_rotOffsets);
            buffers.bonefull_rotResults = create_<BodyBoneLocalRotationResult>(buffers.bonefull_rotIndices.Length());
            buffers.bonefull_transforms = tf_to_(p => p.bonefull_transgorms);
            buffers.bonefull_transformApplyIndices = to_with_index_(p => p.bonefull_transformApplyIndices);

            buffers.boneroothip_posIndices = concat_to_with_index_(
                p => p.boneroot_posIndices,
                p => p.bonehip_posIndices);
            buffers.boneroothip_posScales = concat_to_(
                p => p.boneroot_posScales,
                p => p.bonehip_posScales);
            buffers.boneroothip_posResults = create_<BodyBoneLocalPositionResult>(
                buffers.boneroothip_posIndices.Length());


            // ik

            //if (paramlist.SelectMany(p => p.ikalways_baseTransforms).Any())
            {
                buffers.ikalways_baseTransforms = tf_to_(p => p.ikalways_baseTransforms);
                buffers.ikalways_baseTransformValues = create_<IkBaseTransformValue>(buffers.ikalways_baseTransforms.Length());
                buffers.ikalways_ikAnchorIndices = to_with_index_(p => p.ikalways_ikAnchorIndices);
                buffers.ikalways_legTransformValueSets = create_<SolveIkTransformValueSet>(buffers.ikalways_ikAnchorIndices.Length());
                buffers.ikalways_legTransformValues = tf_to_(p => p.ikalways_legTransforms);
            }

            //if (paramlist.Any(p => p.noikleg_footTransforms is not null))
            {
                buffers.noikleg_ikIndices = to_with_index_(p => p.noikleg_ikIndices);
                buffers.noikleg_footTransforms = tf_to_(p => p.noikleg_footTransforms);
            }
            //if (paramlist.SelectMany(p => p.ikleg_ikData.invoke(default)).Any())
            {
                buffers.ikleg_ikData = to_with_index_(p => p.ikleg_ikData);
            }
            ////if (paramlist.Any(p => p.noikfoot_footTransforms is not null))
            //{
            //    buffers.noikfoot_ikIndices = count_to_(p => p.noikfoot_ikIndices);
            //    buffers.noikfoot_footTransforms = tf_to_(p => p.noikfoot_footTransforms);
            //}
            //if (paramlist.Any(p => p.ikfoot_ikData.invoke is not null))
            {
                buffers.ikfoot_ikData = to_with_index_(p => p.ikfoot_ikData);
            }

            buffers.legalways_ikAnchors = create_<LegIkAnchorLR>(
                buffers.ikleg_ikData.Length() + (buffers.noikleg_ikIndices.Length() >> 1));
            buffers.ikfoot_ikAnchors = create_<FootIkAnchorLR>(
                buffers.ikfoot_ikData.Length());// + (buffers.noikfoot_ikIndices.Length() >> 1));


            // grounding

            //if (paramlist.Any(p => p.ground_hitData.invoke is not null))
            {
                buffers.ground_hitData = to_with_index_(p => p.ground_hitData);
                buffers.ground_castCommands = create_<LegGroundcastCommandLR>(buffers.ground_hitData.Length());
                buffers.ground_hits = create_<LegRaycastHitLR>(buffers.ground_hitData.Length());
                buffers.ground_hitHeightStorages = to_(p => p.ground_rootHeights);
                buffers.ground_rootTransforms = tf_to_(p => p.ground_rootTransforms);
            }

            buffers.ground_footStorages = to_(p => p.ground_footStorages);
            buffers.ground_footFkTransforms = tf_to_(p => p.ground_footFkTransforms);
            buffers.ground_footFkTransformValues = create_<GroundFootFkTransformValueLR>(buffers.ground_footFkTransforms.Length());
            buffers.ground_footResultTransforms = tf_to_(p => p.ground_footResultTransforms);
            buffers.ground_footTransformValueResults = create_<GroundFootResultValueLR>(buffers.ground_footResultTransforms.Length());


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

            NativeArray<U> to_with_index_<U>(Func<ModelParams<TPFinder, TRFinder>, EnumerableWithParam<U>> f) where U : unmanaged =>
                (paramlist, countlist)
                    .ConvertParam(f)
                    .ToNativeArray(alloc);

            NativeArray<U> concat_to_with_index_<U>(
                Func<ModelParams<TPFinder, TRFinder>, EnumerableWithParam<U>> f1,
                Func<ModelParams<TPFinder, TRFinder>, EnumerableWithParam<U>> f2) where U : unmanaged
            =>
                Enumerable.Concat((paramlist, countlist).ConvertParam(f1), (paramlist, countlist).ConvertParam(f2))
                    .ToNativeArray(alloc);

            NativeArray<T> create_<T>(int length) where T : unmanaged =>
                new NativeArray<T>(length, alloc);

            TransformAccessArray tf_to_(Func<ModelParams<TPFinder, TRFinder>, IEnumerable<Transform>> f) =>
                new TransformAccessArray(paramlist.ConvertParam(f).ToArray());


            // 要素数が 0 のときでもバッファを確保しないと、ジョブに渡したときにエラーがでてしまう
            // ものによっては確保しなくてもよいが、面倒なので一律確保する

            //NativeArray<U> to_<U>(Func<ModelParams<TPFinder, TRFinder>, IEnumerable<U>> f) where U : unmanaged =>
            //    paramlist
            //        .ConvertParam(f)
            //        .ToNativeArrayOrNot(alloc);

            //NativeArray<U> concat_to_<U>(
            //    Func<ModelParams<TPFinder, TRFinder>, IEnumerable<U>> f1,
            //    Func<ModelParams<TPFinder, TRFinder>, IEnumerable<U>> f2) where U : unmanaged
            //=>
            //    Enumerable.Concat(paramlist.ConvertParam(f1), paramlist.ConvertParam(f2))
            //        .ToNativeArrayOrNot(alloc);

            //NativeArray<U> to_with_index_<U>(Func<ModelParams<TPFinder, TRFinder>, EnumerableWithParam<U>> f) where U : unmanaged =>
            //    (paramlist, countlist)
            //        .ConvertParam(f)
            //        .ToNativeArrayOrNot(alloc);

            //NativeArray<U> concat_to_with_index_<U>(
            //    Func<ModelParams<TPFinder, TRFinder>, EnumerableWithParam<U>> f1,
            //    Func<ModelParams<TPFinder, TRFinder>, EnumerableWithParam<U>> f2) where U : unmanaged
            //=>
            //    Enumerable.Concat(
            //        (paramlist, countlist).ConvertParam(f1), (paramlist, countlist).ConvertParam(f2))
            //        .ToNativeArrayOrNot(alloc);

            //NativeArray<T> create_<T>(int length) where T : unmanaged =>
            //    length > 0
            //        ? new NativeArray<T>(length, alloc)
            //        : default;

            //TransformAccessArray tf_to_(Func<ModelParams<TPFinder, TRFinder>, IEnumerable<Transform>> f)
            //{
            //    var arr = paramlist.ConvertParam(f).ToArray();

            //    return arr.Length > 0
            //        ? new TransformAccessArray(arr)
            //        : default;
            //}
        }


        public static IEnumerable<U> ConvertParam<T, U>(
            this IEnumerable<T> src,
            Func<T, IEnumerable<U>> invokerSelector)
        =>
            from x in src
            from y in invokerSelector(x) ?? Enumerable.Empty<U>()
            select y;

        public static IEnumerable<U> ConvertParam<T, U>(
            this (IEnumerable<T> param, IEnumerable<ParamCount> opt) src,
            Func<T, EnumerableWithParam<U>> invokerSelector)
        =>
            from x in (src.param, src.opt).Zip()
            from y in invokerSelector(x.Item1).invoke?.Invoke(x.Item2) ?? Enumerable.Empty<U>()
            select y;

        public static int Length(this TransformAccessArray arr) =>
            arr.isCreated
                ? arr.length
                : 0
            ;
        public static int Length<T>(this NativeArray<T> arr) where T : unmanaged =>
            arr.IsCreated
                ? arr.Length
                : 0
            ;


    }

}
