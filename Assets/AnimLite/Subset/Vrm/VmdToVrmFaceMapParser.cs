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


    public struct VmdFaceMapping
    {
        public Dictionary<VmdFaceName, VrmExpressionName> VmdToVrmMaps;
        public bool IsCreated => this.VmdToVrmMaps != null;

        public static implicit operator VmdFaceMapping(Dictionary<VmdFaceName, VrmExpressionName> mapdict) =>
            new VmdFaceMapping
            {
                VmdToVrmMaps = mapdict,
            };
    }




    /// <summary>
    /// ＶＭＤの表情をＶＲＭの表情に変換する対応表テキストファイルを読み下し、対応辞書を構築する。
    /// </summary>
    public static partial class VrmParser
    {

        static public async ValueTask<VmdFaceMapping> ParseFaceMapAsync(this PathUnit filepath, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            using var s = new StreamReader(filepath);
                
            var txt = await s.ReadToEndAsync();
            ct.ThrowIfCancellationRequested();

            var result = (VmdFaceMapping)parse_(txt);
            ct.ThrowIfCancellationRequested();

            return result;
        }

        static public VmdFaceMapping ParseFaceMap(this PathUnit filepath)
        {
            using var s = new StreamReader(filepath);

            var txt = s.ReadToEnd();

            return parse_(txt);
        }



        static public async ValueTask<VmdFaceMapping> ParseFaceMapAsync(this Stream stream, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            using var s = new StreamReader(stream);

            var txt = await s.ReadToEndAsync();
            ct.ThrowIfCancellationRequested();

            var result = (VmdFaceMapping)parse_(txt);
            ct.ThrowIfCancellationRequested();

            return result;
        }

        static public VmdFaceMapping ParseFaceMap(this Stream stream)
        {
            using var s = new StreamReader(stream);

            var txt = s.ReadToEnd();

            return parse_(txt);
        }



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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string.Join(", ", q.Select((x, i) => $"{i}:{x.vmd.name}:{x.vrm.name}")).ShowDebugLog();
#endif

            return q.ToDictionary(x => x.vmd, x => x.vrm);
        }
    }



}
