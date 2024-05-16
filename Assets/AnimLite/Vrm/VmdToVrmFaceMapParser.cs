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
        
        static public Task<VmdFaceMapping> ParseFaceMapAsync(PathUnit filepath, CancellationToken ct) =>
            Task.Run(async () =>
            {
                ct.ThrowIfCancellationRequested();

                using var s = new StreamReader(filepath);
                
                var txt = await s.ReadToEndAsync();

                ct.ThrowIfCancellationRequested();

                return (VmdFaceMapping)parse_(txt);
            }, ct);

        static public VmdFaceMapping ParseFaceMap(PathUnit filepath)
        {
            using var s = new StreamReader(filepath);

            var txt = s.ReadToEnd();

            return parse_(txt);
        }


        static public Task<VmdFaceMapping> ParseFaceMapAsync(TextAsset textfile, CancellationToken ct) =>
            ParseFaceMapAsync(textfile.text, ct);

        static public VmdFaceMapping ParseFaceMap(TextAsset textfile) =>
            parse_(textfile.text);


        static public Task<VmdFaceMapping> ParseFaceMapAsync(string text, CancellationToken ct) =>
            Task.Run(() => ParseFaceMap(text), ct);

        static public VmdFaceMapping ParseFaceMap(string text) =>
            parse_(text);


        static Dictionary<VmdFaceName, VrmExpressionName> parse_(string text)
        {
            var opt = StringSplitOptions.RemoveEmptyEntries;

            var q =
                from line in text.Split("\n", opt)
                let words = line.Trim().Split("\t", 2, opt)
                let vmd = words[0].Trim().AsVmdFaceName()
                let vrm = words[1].Trim().AsVrmExpressionName()
                select (vmd, vrm)
                ;

            #if UNITY_EDITOR
                string.Join(", ", q.Select((x, i) => $"{i}:{x.vmd.name}:{x.vrm.name}")).ShowDebugLog();
            #endif

            return q.ToDictionary(x => x.vmd, x => x.vrm);
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
