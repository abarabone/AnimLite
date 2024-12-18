using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace AnimLite
{
    using AnimLite.Utility;
    using AnimLite.Vmd;
    using AnimLite.Vrm;
    using AnimLite.Loader;



    public class PrototypeCacheHolder : IAsyncDisposable
    {

        PrototypeCache<PathUnit, VmdFaceMapping> faceMapCache;
        PrototypeCache<PathList, VmdStreamData> vmdCoreCache;

        public (PrototypeCache<PathList, VmdStreamData> stream, PrototypeCache<PathUnit, VmdFaceMapping> facemap) VmdCache =>
            (this.vmdCoreCache, this.faceMapCache);


        public PrototypeCache<PathUnit, GameObject> ModelCache { get; private set; }


        public PrototypeCacheHolder(bool useVmdCache = false, bool useModelCache = false)
        {
            if (useVmdCache)
            {
                this.faceMapCache = new();
                this.vmdCoreCache = new();
            }

            if (useModelCache)
            {
                this.ModelCache = new();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await this.ModelCache.NullableAsync(x => x.ClearCacheAsync());

            await this.VmdCache.stream.NullableAsync(x => x.ClearCacheAsync());
            await this.VmdCache.facemap.NullableAsync(x => x.ClearCacheAsync());
        }
    }




    public static class PrototypeCacheUtility
    {


        public static Task<Instance<GameObject>> GetOrLoadModelAsync(
            this PrototypeCache<PathUnit, GameObject> cache, PathUnit path, IArchive archive, CancellationToken ct)
        =>
            cache.GetOrLoadAsync(path, () =>
                archive.LoadModelPrototypeAsync(path, ct).AsTask());




        public static Task<Instance<VmdFaceMapping>> GetOrLoadVmdFaceMappingAsync(
            this PrototypeCache<PathUnit, VmdFaceMapping> cache, PathUnit path, IArchive archive, CancellationToken ct)
        =>
            cache.GetOrLoadAsync(path, async () =>
            {
                var fmap = await archive.LoadFaceMapAsync(path, ct);

                return new Prototype<VmdFaceMapping>(fmap);
            });





        public static Task<Instance<VmdStreamData>> GetOrLoadVmdAsync(
            this (PrototypeCache<PathList, VmdStreamData> vmd, PrototypeCache<PathUnit, VmdFaceMapping> facemap) cache,
            PathUnit vmdFilePath, PathUnit faceMapFilePath, IArchive archive,
            CancellationToken ct)
        =>
            cache.GetOrLoadVmdAsync(vmdFilePath.ToPathList(), faceMapFilePath, archive, ct);



        public static Task<Instance<VmdStreamData>> GetOrLoadVmdAsync(
            this (PrototypeCache<PathList, VmdStreamData> vmd, PrototypeCache<PathUnit, VmdFaceMapping> facemap) cache,
            PathList vmdFilePathList, PathUnit faceMapFilePath, IArchive archive,
            CancellationToken ct)
        {

            var pathSet = vmdFilePathList.Append(faceMapFilePath);

            return cache.vmd.GetOrLoadAsync(pathSet, async () =>
            {
                var vmddata = await archive.LoadVmdExAsync(vmdFilePathList, ct);
                var facemap = await cache.facemap.GetOrLoadVmdFaceMappingAsync(faceMapFilePath, archive, ct);

                var vmdcore = vmddata.BuildStreamCoreData(facemap.Value, ct);
                return new VmdStreamCorePrototype(vmdcore);
            });
        }
    }

}
