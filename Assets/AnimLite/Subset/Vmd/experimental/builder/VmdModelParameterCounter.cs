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
    using AnimLite.Vmd.experimental.Data;
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;



    public struct ParamCount
    {
        public int model_total_length;

        public int model_offset;
        public int legalways_offset;
        //public int footalways_offset;
        public int footik_offset;
        public int ground_offset;

        public int ikalways_offset;

        //public int pos_offset;
        public int rot_offset;
    }


    public static class ModelParamCounterExtension
    {


        public static ParamCount[] CountParams<TPFinder, TRFinder>(
            this IEnumerable<ModelParams<TPFinder, TRFinder>> paramlist)
                where TPFinder : unmanaged, IKeyFinderWithoutProcedure<float4>
                where TRFinder : unmanaged, IKeyFinderWithoutProcedure<quaternion>
        {
            var counter = new ParamCount { model_total_length = paramlist.Count() };

            var countlist = paramlist
                .Scan(counter, (count, p) =>
                {
                    var md = p.model_data;


                    count.model_offset += 1;
                    count.rot_offset += md.bodyop.bones.BoneLength;


                    count.ikalways_offset +=
                        md.footop.useGroundHit | md.footop.useLegPositionIk | md.footop.useLegPositionIk ? 1 : 0;

                    count.ground_offset +=
                        md.footop.useGroundHit ? 1 : 0;

                    count.legalways_offset +=
                        md.footop.useLegPositionIk | md.footop.useGroundHit ? 1 : 0;
                    
                    //count.footalways_offset +=
                    //    md.footop.useFootRotationIk | md.footop.useGroundHit ? 1 : 0;
                    count.footik_offset +=
                        md.footop.useFootRotationIk ? 1 : 0;
                    

                    return count;
                })
                .Prepend(counter)
                .SkipLast(1)
                .ToArray();

            return countlist;
        }

        static IEnumerable<T> SkipLast<T>(this IEnumerable<T> src, int i) => EnumerableEx.SkipLast(src, i);




    }
}
