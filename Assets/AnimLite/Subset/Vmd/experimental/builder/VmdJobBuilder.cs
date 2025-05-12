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



    public unsafe static class VmdMotionJobs
    {


        public static JobHandle BuildMotionJobsAndSchedule<TPFinder, TRFinder>(
            //this JobBuffers<TPFinder, TRFinder> buf, float deltaTime, JobHandle dep = default)
            this JobBuffers<TPFinder, TRFinder> buf, float currentTime, float speedHint = 1.0f, JobHandle dep = default)
                where TPFinder : unmanaged, IKeyFinderWithoutProcedure<float4>
                where TRFinder : unmanaged, IKeyFinderWithoutProcedure<quaternion>
        {

            var depsel = UpdateTimeAndProcedureSelectorJob_(dep);

            var deprot = BodyRotationJob_(depsel);
            var deppos = BodyPositionJob_(depsel);
            
            var depadjust = BodyHipPositionAdjustJob_((deprot, deppos).Combine());

            var depapplybody = ApplyBodyTransformJob_(depadjust);


            var depiktfbase = IkBaseTransformCopyJob_(dep);

            var depikleg = GetLegIkAnchorJob_((depiktfbase, depadjust).Combine());
            var depikfoot = GetFootIkAnchorJob_((depiktfbase, depsel).Combine());
            //var depfkikleg = CopyLegFkToIkAnchorJob_(depapplybody);
            //var depfkikfoot = CopyFootFkToIkAnchorJob_(depapplybody);
            var depfkikleg = CopyLegFkToIkAnchorJob_((depapplybody, depikleg).Combine());
            //var depfkikfoot = CopyFootFkToIkAnchorJob_((depapplybody, depikfoot).Combine());
            //var depfkikleg = (depapplybody, depikleg).Combine();
            //var depfkikfoot = (depapplybody, depikfoot).Combine();

            var depikhit = IkAnchorToCastCommandJob_((depfkikleg, depikleg, depiktfbase).Combine());

            var depikcast = GroundcastCommand_(depikhit);

            var dephitapply = HitApplyToIkAnchorJob_(depikcast);

            var dephitroot = LegIkApplyRootHeightJob_(dephitapply);


            var depsolve_fromtf = SolveCopyFromTransformJob_(depapplybody);
            
            var depsolve_apply = SolveIkJob_((depsolve_fromtf, dephitroot, depikfoot).Combine());
            
            var depsolve_totf = SolveCopyToTransformJob_(depsolve_apply);
            

            var dephitfoot_cpy = CopyTransformToFootFkValueJob_(depsolve_totf);

            var dephitfoot_inp = GroundFootInterpolateJob_(dephitfoot_cpy);

            var dephitfoot_res = CopyGroundFootToTransformJob_(dephitfoot_inp);


            return dephitfoot_res;
            



            JobHandle UpdateTimeAndProcedureSelectorJob_(JobHandle dep)
            {
                return new UpdateTimeAndProcedureSelectorJob
                {
                    model_timer = buf.model_timer,
                    model_procedureSelectors = buf.model_procedureSelectors,
                    //deltaTime = deltaTime,
                    currentTime = currentTime,
                    speedHint = speedHint,
                }
                .Schedule(buf.model_procedureSelectors.Length, 32, dep);
            }

            JobHandle BodyRotationJob_(JobHandle dep)
            {
                return new BodyRotationJob<TPFinder, TRFinder>
                {
                    model_finders = buf.model_finders,
                    model_boneOptions = buf.model_boneOptions,
                    model_timer = buf.model_timer,
                    model_procedureSelectors = buf.model_procedureSelectors,

                    bonefull_rotIndices = buf.bonefull_rotIndices,
                    bonefull_rotOffsets = buf.bonefull_rotOffsets,
                    bonefull_rotResults = buf.bonefull_rotResults,
                }
                .Schedule(buf.bonefull_rotIndices.Length, 16, dep);
            }
            
            JobHandle BodyPositionJob_(JobHandle dep)
            {
                return new BodyPositionJob<TPFinder, TRFinder>
                {
                    model_finders = buf.model_finders,
                    model_boneOptions = buf.model_boneOptions,
                    model_timer = buf.model_timer,
                    model_procedureSelectors = buf.model_procedureSelectors,

                    boneroothip_posIndices = buf.boneroothip_posIndices,
                    bodyroothip_posScales = buf.boneroothip_posScales,
                    boneroothip_posResults = buf.boneroothip_posResults,
                }
                .Schedule(buf.boneroothip_posIndices.Length, 16, dep);
            }

            JobHandle BodyHipPositionAdjustJob_(JobHandle dep)
            {
                return new BodyHipPositionAdjustJob
                {
                    bonefull_rotResults = buf.bonefull_rotResults,
                    model_hipAdjusts = buf.model_hipAdjusts,
                    bodyhip_posResults = buf.bodyhip_posResults,
                }
                .Schedule(buf.bodyhip_posResults.Length, 8, dep);
            }

            JobHandle ApplyBodyTransformJob_(JobHandle dep)
            {
                return new ApplyBodyTransformJob
                {
                    bonefull_transformApplyIndices = buf.bonefull_transformApplyIndices,
                    boneroothip_posResults = buf.boneroothip_posResults,
                    bonefull_rotResults = buf.bonefull_rotResults,
                }
                .Schedule(buf.bonefull_transforms, dep);
            }


            JobHandle CopyLegFkToIkAnchorJob_(JobHandle dep)
            {
                if (buf.noikleg_ikIndices.Length() == 0) return dep;

                return new CopyLegFkToIkAnchorJob
                {
                    legalways_ikAnchors = buf.legalways_ikAnchors.Reinterpret<LegIkAnchor>(sizeof(LegIkAnchorLR)),
                    noikleg_anchorIndices = buf.noikleg_ikIndices,
                }
                .ScheduleReadOnly(buf.noikleg_footTransforms, 32, dep);
            }

            //JobHandle CopyFootFkToIkAnchorJob_(JobHandle dep)
            //{
            //    if (buf.noikfoot_ikIndices.Length == 0) return dep;

            //    return new CopyFootFkToIkAnchorJob
            //    {
            //        footalways_ikAnchors = buf.footalways_ikAnchors.Reinterpret<FootIkAnchor>(sizeof(FootIkAnchorLR)),
            //        noikfoot_anchorIndices = buf.noikfoot_ikIndices,
            //    }
            //    .ScheduleReadOnly(buf.noikfoot_footTransforms, 32, dep);
            //}


            JobHandle IkBaseTransformCopyJob_(JobHandle dep)
            {
                if (buf.ikalways_baseTransforms.Length() == 0) return dep;

                return new IkBaseTransformCopyJob
                {
                    ikalways_baseTransformValues = buf.ikalways_baseTransformValues,
                }
                .ScheduleReadOnly(buf.ikalways_baseTransforms, 8, dep);
            }

            JobHandle GetLegIkAnchorJob_(JobHandle dep)
            {
                if (buf.ikleg_ikData.Length() == 0) return dep;

                return new GetLegIkAnchorJob<TPFinder, TRFinder>
                {
                    model_finders = buf.model_finders,
                    model_procedureSelectors = buf.model_procedureSelectors,
                    ikalways_baseTransformValues = buf.ikalways_baseTransformValues,
                    ikleg_ikData = buf.ikleg_ikData,
                    boneroothip_posResults = buf.boneroothip_posResults,
                    legalways_ikAnchors = buf.legalways_ikAnchors,
                    model_timer = buf.model_timer,
                }
                .Schedule(buf.ikleg_ikData.Length, 8, dep);
            }

            JobHandle GetFootIkAnchorJob_(JobHandle dep)
            {
                if (buf.ikfoot_ikData.Length() == 0) return dep;

                return new GetFootIkAnchorJob<TPFinder, TRFinder>
                {
                    model_finders = buf.model_finders,
                    model_procedureSelectors = buf.model_procedureSelectors,
                    ikalways_baseTransformValues = buf.ikalways_baseTransformValues,
                    ikfoot_ikData = buf.ikfoot_ikData,
                    footalways_ikAnchors = buf.ikfoot_ikAnchors,
                    model_timer = buf.model_timer,
                }
                .Schedule(buf.ikfoot_ikData.Length, 8, dep);
            }

            JobHandle IkAnchorToCastCommandJob_(JobHandle dep)
            {
                if (buf.ground_castCommands.Length() == 0) return dep;

                return new IkAnchorToCastCommandJob
                {
                    ikalways_baseTransformValues = buf.ikalways_baseTransformValues,
                    legalways_legIkAnchors = buf.legalways_ikAnchors,
                    ground_hitCastCommands = buf.ground_castCommands,
                    ground_hitData = buf.ground_hitData,
                    ground_rootHeigts = buf.ground_hitHeightStorages,
                }
                .Schedule(buf.ground_castCommands.Length, 8, dep);
            }



            JobHandle GroundcastCommand_(JobHandle dep)
            {
                if (buf.ground_castCommands.Length() == 0) return dep;

                return SpherecastCommand.ScheduleBatch(
                    buf.ground_castCommands.Reinterpret<SpherecastCommand>(sizeof(LegGroundcastCommandLR)),
                    buf.ground_hits.Reinterpret<RaycastHit>(sizeof(LegRaycastHitLR)),
                    minCommandsPerJob: 2,
                    maxHits: 1,
                    dep);
            }

            JobHandle HitApplyToIkAnchorJob_(JobHandle dep)
            {
                if (buf.ground_castCommands.Length() == 0) return dep;

                return new HitApplyToIkAnchorJob
                {
                    ground_hits = buf.ground_hits,
                    ground_hitData = buf.ground_hitData,
                    boneroothip_posResults = buf.boneroothip_posResults,
                    legalways_ikAnchors = buf.legalways_ikAnchors,
                    //footalways_ikAnchors = buf.footalways_ikAnchors,
                    ground_hitHeightStorages = buf.ground_hitHeightStorages,
                    ikalways_baseTransformValues = buf.ikalways_baseTransformValues,
                    model_timer = buf.model_timer,
                }
                .Schedule(buf.ground_castCommands.Length, 8, dep);
            }

            JobHandle LegIkApplyRootHeightJob_(JobHandle dep)
            {
                if (buf.ground_rootTransforms.Length() == 0) return dep;

                return new LegIkApplyRootHeightJob
                {
                    ground_hitHeightStorages = buf.ground_hitHeightStorages,
                }
                .Schedule(buf.ground_rootTransforms, dep);
            }


            JobHandle SolveCopyFromTransformJob_(JobHandle dep)
            {
                if (buf.ikalways_legTransformValues.Length() == 0) return dep;

                return new SolveCopyFromTransformJob
                {
                    ikalways_legTransformValues = buf.ikalways_legTransformValueSets
                        .Reinterpret<SolveIkAppliedTransformValue>(sizeof(SolveIkTransformValueSet)),
                }
                .ScheduleReadOnly(buf.ikalways_legTransformValues, 8, dep);
            }
            
            JobHandle SolveIkJob_(JobHandle dep)
            {
                if (buf.ikalways_legTransformValueSets.Length() == 0) return dep;

                return new SolveIkJob
                {
                    legalways_ikAnchors = buf.legalways_ikAnchors,
                    footalways_ikAnchors = buf.ikfoot_ikAnchors,
                    ikalways_ikAnchorIndices = buf.ikalways_ikAnchorIndices,
                    ikalways_legTransformValueSets = buf.ikalways_legTransformValueSets,
                }
                .Schedule(buf.ikalways_legTransformValueSets.Length, 8, dep);
            }

            JobHandle SolveCopyToTransformJob_(JobHandle dep)
            {
                if (buf.ikalways_legTransformValues.Length() == 0) return dep;

                return new SolveCopyToTransformJob
                {
                    ikalways_legTransformValues = buf.ikalways_legTransformValueSets
                        .Reinterpret<SolveIkAppliedTransformValue>(sizeof(SolveIkTransformValueSet)),
                }
                .Schedule(buf.ikalways_legTransformValues, dep);
            }



            JobHandle CopyTransformToFootFkValueJob_(JobHandle dep)
            {
                if (buf.ground_footFkTransforms.Length() == 0) return dep;

                return new CopyTransformToFootFkValueJob
                {
                    ground_footFkTransformValues = buf.ground_footFkTransformValues
                        .Reinterpret<GroundFootFkTransformValue>(sizeof(GroundFootFkTransformValueLR)),
                }
                .ScheduleReadOnly(buf.ground_footFkTransforms, 8, dep);
            }

            JobHandle GroundFootInterpolateJob_(JobHandle dep)
            {
                if (buf.ground_hits.Length() == 0) return dep;

                return new GroundFootInterpolateJob
                {
                    model_timer = buf.model_timer,
                    ikalways_baseTransformValues = buf.ikalways_baseTransformValues,

                    ground_hits = buf.ground_hits,
                    ground_hitData = buf.ground_hitData,
                    ground_footFkTransformValues = buf.ground_footFkTransformValues,
                    ground_footStorages = buf.ground_footStorages,

                    ground_transformValueResults = buf.ground_footTransformValueResults,
                }
                .Schedule(buf.ground_hits.Length, 8, dep);
            }

            JobHandle CopyGroundFootToTransformJob_(JobHandle dep)
            {
                if (buf.ground_footResultTransforms.Length() == 0) return dep;

                return new CopyGroundFootToTransformJob
                {
                    ground_transformValues = buf.ground_footTransformValueResults
                        .Reinterpret<GroundFootResultValue>(sizeof(GroundFootResultValueLR)),
                }
                .Schedule(buf.ground_footResultTransforms, dep);
            }

        }


        public static JobHandle Combine(this (JobHandle x0, JobHandle x1, JobHandle x2, JobHandle x3, JobHandle x4, JobHandle x5) dep)
        {
            var deplist = new NativeArray<JobHandle>(6, Allocator.Temp);
            deplist[0] = dep.x0;
            deplist[1] = dep.x1;
            deplist[2] = dep.x2;
            deplist[3] = dep.x3;
            deplist[4] = dep.x4;
            deplist[5] = dep.x5;
            var depresult = JobHandle.CombineDependencies(deplist);
            deplist.Dispose();

            return depresult;
        }
        public static JobHandle Combine(this (JobHandle x0, JobHandle x1, JobHandle x2, JobHandle x3, JobHandle x4) dep)
        {
            var deplist = new NativeArray<JobHandle>(5, Allocator.Temp);
            deplist[0] = dep.x0;
            deplist[1] = dep.x1;
            deplist[2] = dep.x2;
            deplist[3] = dep.x3;
            deplist[4] = dep.x4;
            var depresult = JobHandle.CombineDependencies(deplist);
            deplist.Dispose();

            return depresult;
        }
        public static JobHandle Combine(this (JobHandle x0, JobHandle x1, JobHandle x2, JobHandle x3) dep)
        {
            var deplist = new NativeArray<JobHandle>(4, Allocator.Temp);
            deplist[0] = dep.x0;
            deplist[1] = dep.x1;
            deplist[2] = dep.x2;
            deplist[3] = dep.x3;
            var depresult = JobHandle.CombineDependencies(deplist);
            deplist.Dispose();

            return depresult;
        }

        public static JobHandle Combine(this (JobHandle x0, JobHandle x1, JobHandle x2) dep) =>
            JobHandle.CombineDependencies(dep.x0, dep.x1, dep.x2);
        
        public static JobHandle Combine(this (JobHandle x0, JobHandle x1) dep) =>
            JobHandle.CombineDependencies(dep.x0, dep.x1);


        public static JobHandle CompleteThrough(this JobHandle dep)
        {
            dep.Complete();
            return dep;
        }
    }

}
