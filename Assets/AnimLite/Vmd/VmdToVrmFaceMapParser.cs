using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AnimLite.Vrm
{
    using AnimLite.Utility;
    using AnimLite.Vmd;


    /// <summary>
    /// ＶＭＤの表情をＶＲＭの表情に変換する対応表テキストファイルを読み下し、対応辞書を構築する。
    /// </summary>
    public static class VrmParser
    {
        
        static public Task<VmdFaceMapping> ParseFaceMapAsync(string filepath, CancellationToken ct) =>
            Task.Run(() => ParseFaceMap(filepath), ct);


        static public VmdFaceMapping ParseFaceMap(string filepath)
        {

            using var s = new StreamReader(filepath);

            //var txt = await s.ReadToEndAsync();
            var txt = s.ReadToEnd();
            return parse_();


            Dictionary<VmdFaceName, VrmExpressionName> parse_()
            {
                var q =
                    from line in txt.Split("\n", StringSplitOptions.RemoveEmptyEntries)
                    select line.Trim().Split("\t", StringSplitOptions.RemoveEmptyEntries)
                    ;

#if UNITY_EDITOR
                string.Join(", ", q.Select((x, i) => $"{i}:{x[0]}:{x[1]}")).ShowDebugLog();
#endif

                return q.ToDictionary(x => x[0].Trim().AsVmdFaceName(), x => x[1].Trim().AsVrmExpressionName());
            }
        }

    }


    public struct VmdFaceMapping
    {
        public Dictionary<VmdFaceName, VrmExpressionName> VmdToVrmMaps;

        public static implicit operator VmdFaceMapping(Dictionary<VmdFaceName, VrmExpressionName> mapdict) =>
            new VmdFaceMapping
            {
                VmdToVrmMaps = mapdict,
            };
    }


}
