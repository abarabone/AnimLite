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


        public static JobHandle BuildMotionJobsAndSchedule<TPFinder, TRFinder>(this JobBuffers<TPFinder, TRFinder> buf, float deltaTime, JobHandle dep = default)
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
            var depfkikfoot = CopyFootFkToIkAnchorJob_((depapplybody, depikfoot).Combine());
            //var depfkikleg = (depapplybody, depikleg).Combine();
            //var depfkikfoot = (depapplybody, depikfoot).Combine();

            var depikhit = IkAnchorToCastCommandJob_((depfkikleg, depikleg, depiktfbase).Combine());

            var depikcast = RaycastCommand_(depikhit);

            var dephitapply = HitApplyToIkAnchorJob_((depikcast, depfkikfoot, depikfoot).Combine());

            var dephitroot = LegIkApplyRootHeightJob_(dephitapply);


            var depsolve_fromtf = SolveCopyFromTransformJob_(depapplybody);

            var depsolve_apply = SolveIkJob_((depsolve_fromtf, dephitroot).Combine());

            var depsolve_totf = SolveCopyToTransformJob_(depsolve_apply);


            return depsolve_totf;
            



            JobHandle UpdateTimeAndProcedureSelectorJob_(JobHandle dep)
            {
                return new UpdateTimeAndProcedureSelectorJob
                {
                    model_timer = buf.model_timer,
                    model_procedureSelectors = buf.model_procedureSelectors,
                    deltaTime = deltaTime,
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
                if (buf.noikleg_ikIndices.Length == 0) return dep;

                return new CopyLegFkToIkAnchorJob
                {
                    legalways_ikAnchors = buf.legalways_ikAnchors.Reinterpret<LegIkAnchor>(sizeof(LegIkAnchorLR)),
                    noikleg_anchorIndices = buf.noikleg_ikIndices,
                }
                .ScheduleReadOnly(buf.noikleg_footTransforms, 32, dep);
            }

            JobHandle CopyFootFkToIkAnchorJob_(JobHandle dep)
            {
                if (buf.noikfoot_ikIndices.Length == 0) return dep;

                return new CopyFootFkToIkAnchorJob
                {
                    footalways_ikAnchors = buf.footalways_ikAnchors.Reinterpret<FootIkAnchor>(sizeof(FootIkAnchorLR)),
                    noikfoot_anchorIndices = buf.noikfoot_ikIndices,
                }
                .ScheduleReadOnly(buf.noikfoot_footTransforms, 32, dep);
            }


            JobHandle IkBaseTransformCopyJob_(JobHandle dep)
            {
                if (buf.ikalways_baseTransforms.length == 0) return dep;

                return new IkBaseTransformCopyJob
                {
                    ikalways_baseTransformValues = buf.ikalways_baseTransformValues,
                }
                .ScheduleReadOnly(buf.ikalways_baseTransforms, 8, dep);
            }

            JobHandle GetLegIkAnchorJob_(JobHandle dep)
            {
                if (buf.ikleg_ikData.Length == 0) return dep;

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
                if (buf.ikfoot_ikData.Length == 0) return dep;

                return new GetFootIkAnchorJob<TPFinder, TRFinder>
                {
                    model_finders = buf.model_finders,
                    model_procedureSelectors = buf.model_procedureSelectors,
                    ikalways_baseTransformValues = buf.ikalways_baseTransformValues,
                    ikfoot_ikData = buf.ikfoot_ikData,
                    footalways_ikAnchors = buf.footalways_ikAnchors,
                    model_timer = buf.model_timer,
                }
                .Schedule(buf.ikfoot_ikData.Length, 8, dep);
            }

            JobHandle IkAnchorToCastCommandJob_(JobHandle dep)
            {
                if (buf.ground_hitCastCommands.Length == 0) return dep;

                return new IkAnchorToCastCommandJob
                {
                    ikalways_baseTransformValues = buf.ikalways_baseTransformValues,
                    legalways_legIkAnchors = buf.legalways_ikAnchors,
                    ground_hitCastCommands = buf.ground_hitCastCommands,
                    ground_hitData = buf.ground_hitData,
                    ground_rootHeigts = buf.ground_hitHeightStorages,
                }
                .Schedule(buf.ground_hitCastCommands.Length, 8, dep);
            }



            JobHandle RaycastCommand_(JobHandle dep)
            {
                if (buf.ground_hitCastCommands.Length == 0) return dep;

                //return SpherecastCommand.ScheduleBatch(
                //    buf.leg_hitCastCommands.Reinterpret<SpherecastCommand>(sizeof(LegHitCastCommand)),
                return RaycastCommand.ScheduleBatch(
                    buf.ground_hitCastCommands.Reinterpret<RaycastCommand>(sizeof(LegHitCastCommandLR)),
                    buf.ground_hits.Reinterpret<RaycastHit>(sizeof(LegHitRaycastHitLR)),
                    minCommandsPerJob: 2,
                    maxHits: 1,
                    dep);
            }

            JobHandle HitApplyToIkAnchorJob_(JobHandle dep)
            {
                if (buf.ground_hitCastCommands.Length == 0) return dep;

                return new HitApplyToIkAnchorJob
                {
                    ground_hits = buf.ground_hits,
                    ground_hitData = buf.ground_hitData,
                    boneroothip_posResults = buf.boneroothip_posResults,
                    legalways_ikAnchors = buf.legalways_ikAnchors,
                    footalways_ikAnchors = buf.footalways_ikAnchors,
                    ground_hitHeightStorages = buf.ground_hitHeightStorages,
                    ikalways_baseTransformValues = buf.ikalways_baseTransformValues,
                    model_timer = buf.model_timer,
                }
                .Schedule(buf.ground_hitCastCommands.Length, 8, dep);
            }

            JobHandle LegIkApplyRootHeightJob_(JobHandle dep)
            {
                if (buf.ground_rootTransforms.length == 0) return dep;

                return new LegIkApplyRootHeightJob
                {
                    ground_hitHeightStorages = buf.ground_hitHeightStorages,
                }
                .Schedule(buf.ground_rootTransforms, dep);
            }


            JobHandle SolveCopyFromTransformJob_(JobHandle dep)
            {
                if (buf.ikalways_legTransformValues.length == 0) return dep;

                return new SolveCopyFromTransformJob
                {
                    ikalways_legTransformValues = buf.ikalways_legTransformValueSets
                        .Reinterpret<SolveIkAppliedTransformValue>(sizeof(SolveIkTransformValueSet)),
                }
                .ScheduleReadOnly(buf.ikalways_legTransformValues, 8, dep);
            }
            
            JobHandle SolveIkJob_(JobHandle dep)
            {
                if (buf.ikalways_legTransformValueSets.Length == 0) return dep;

                return new SolveIkJob
                {
                    legalways_ikAnchors = buf.legalways_ikAnchors,
                    footalways_ikAnchors = buf.footalways_ikAnchors,
                    ikalways_ikAnchorIndices = buf.ikalways_ikAnchorIndices,
                    ikalways_legTransformValueSets = buf.ikalways_legTransformValueSets,
                }
                .Schedule(buf.ikalways_legTransformValueSets.Length, 8, dep);
            }

            JobHandle SolveCopyToTransformJob_(JobHandle dep)
            {
                if (buf.ikalways_legTransformValues.length == 0) return dep;

                return new SolveCopyToTransformJob
                {
                    ikalways_legTransformValues = buf.ikalways_legTransformValueSets
                        .Reinterpret<SolveIkAppliedTransformValue>(sizeof(SolveIkTransformValueSet)),
                }
                .Schedule(buf.ikalways_legTransformValues, dep);
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
