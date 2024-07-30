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
    using System.IO.Compression;

    /// <summary>
    /// ＶＭＤデータをキャッシュする。
    /// キーはパス。フェイスマップが異なるものは異なるデータとして保持する。
    /// 保持されるデータは ボディ回転、ボディ位置、フェイス、であり、いずれもストリームキャッシュを持たず、コアデータといえる。
    /// ストリームキャッシュを付加するには、.CloneShallowlyWithCache() でデータのクローンを作成する。
    /// VmdStreamDataCache.enabled が false になったときキャッシュはクリアされる。
    /// ただしＶＭＤデータは参照カウント付きで、参照が全てゼロになった時に破棄される。
    /// 参照カウントはコアデータ生成時、クローン作成時、に１ずつ加算される。
    /// </summary>
    public class VmdStreamDataCache : MonoBehaviour
    {

        VmdCacheDictionary Cache = new();


        public async Awaitable OnDisable()
        {
            await this.Cache.ClearCache();
        }




        public Task<(VmdStreamData vmddata, VmdFaceMapping facemap)> GetOrLoadVmdStreamDataAsync(
            PathUnit vmdFilePath, PathUnit faceMapFilePath, CancellationToken ct)
        =>
            this.GetOrLoadVmdStreamDataAsync(vmdFilePath, faceMapFilePath, null, ct);


        public async Task<(VmdStreamData vmddata, VmdFaceMapping facemap)> GetOrLoadVmdStreamDataAsync(
            PathUnit vmdFilePath, PathUnit faceMapFilePath, ZipArchive archive, CancellationToken ct)
        {
            var (_vmddata, facemap) = await this.Cache.GetOrLoadAsync(vmdFilePath, faceMapFilePath, archive, ct);

            var vmddata = _vmddata.CloneShallowlyWithCache();

            return (vmddata, facemap);
        }


    }

}