using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;
using System.Collections.Concurrent;
//using VRM;

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

        [SerializeField]
        VmdCacheDictionary Cache = new();

        [SerializeField]
        ModelGameObjectStocker ModelStocker = new();// とりあえずここに置くが、考慮すること


        public async Awaitable OnDisable()
        {
            await this.Cache.ClearCache();
        }


        public ValueTask<VmdFaceMapping> GetFaceMapAsync(PathUnit facemappath) =>
            this.Cache.GetFaceMapAsync(facemappath);




        public Task<(VmdStreamData vmddata, VmdFaceMapping facemap)> GetOrLoadVmdStreamDataAsync(
            PathList vmdFilePaths, PathUnit faceMapFilePath, CancellationToken ct)
        =>
            this.GetOrLoadVmdStreamDataAsync(vmdFilePaths, faceMapFilePath, null, ct);


        public async Task<(VmdStreamData vmddata, VmdFaceMapping facemap)> GetOrLoadVmdStreamDataAsync(
            PathList vmdFilePaths, PathUnit faceMapFilePath, IArchive archive, CancellationToken ct)
        {
            var (_vmddata, facemap) = await this.Cache.GetOrLoadAsync(vmdFilePaths, faceMapFilePath, archive, ct);

            var vmddata = _vmddata.CloneShallowlyWithCache();

            return (vmddata, facemap);
        }




        public Task<GameObject> GetOrLoadModelAsync(PathUnit path, CancellationToken ct) =>
            this.GetOrLoadModelAsync(path, null, ct);

        public Task<GameObject> GetOrLoadModelAsync(PathUnit path, IArchive archive, CancellationToken ct) =>
            this.ModelStocker.GetOrLoadAsync(path, archive, ct);


        public ValueTask HideAndDestroyModelAsync() =>
            this.ModelStocker.TrimGameObjectsAsync();



    }

}