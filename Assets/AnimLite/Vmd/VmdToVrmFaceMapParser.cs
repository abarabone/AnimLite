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
    /// �u�l�c�̕\����u�q�l�̕\��ɕϊ�����Ή��\�e�L�X�g�t�@�C����ǂ݉����A�Ή��������\�z����B
    /// </summary>
    public static class VrmParser
    {

        static public async Awaitable<Dictionary<VmdFaceName, VrmExpressionName>> ParseFaceMapAsync(
            string filepath, CancellationToken ct)
        {

            using var s = new StreamReader(filepath);

            var txt = await s.ReadToEndAsync();
            return await Task.Run(() => parse_(), ct);


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



}
