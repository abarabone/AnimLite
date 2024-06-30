using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;
using System.Collections.Concurrent;
using VRM;

namespace AnimLite.Vmd
{
    using AnimLite.Vrm;
    using AnimLite.Utility;
    using AnimLite.DancePlayable;
    using AnimLite.Utility.Linq;

    public class VmdStreamDataCache : MonoBehaviour
    {

        VmdCacheDictionary Cache = new();


        public async Awaitable OnDisable()
        {
            await this.Cache.ClearCache();
        }


        public async Task<(VmdStreamData vmddata, VmdFaceMapping facemap)> GetOrLoadVmdStreamDataAsync(
            PathUnit vmdFilePath, PathUnit faceMapFilePath, CancellationToken ct)
        {
            var (_vmddata, facemap) = await this.Cache.GetOrLoadAsync(vmdFilePath, faceMapFilePath, ct);

            var vmddata = _vmddata.CloneShallowlyWithCache();

            return (vmddata, facemap);
        }


    }

}