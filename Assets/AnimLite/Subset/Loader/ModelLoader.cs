using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.VisualScripting;
using System.IO;
using System.Collections.Concurrent;
using UnityEngine.Networking;
using UniVRM10;
using UnityEngine.AddressableAssets;
using System.Net.Http;
using System.IO.Compression;

namespace AnimLite.Utility
{
    using AnimLite.Vmd;
    using AnimLite.Vrm;

}

namespace AnimLite.Vrm
{
    using AnimLite.Utility;
    using AnimLite.Vmd;
    using UniGLTF;

    // todo
    // リソース時、gameobject でロードできなかったら .vrm でロードする


    public static partial class VrmLoader
    {




        public static async ValueTask<GameObject> LoadModelExAsync(
            this IArchive archive, PathUnit path, CancellationToken ct)
        {
            if (path.IsBlank()) return default;

            if (archive is not null && !path.IsFullPath())
            {
                var model = await LoadErr.LoggingAsync(() =>
                    archive.ExtractAsync(path, s => s.convertAsync(path, ct)));

                if (model is not null)
                    return model;

                if (archive.FallbackArchive is not null)
                    return await archive.FallbackArchive.LoadModelExAsync(path, ct);
            }

            return await path.LoadModelExAsync(ct);
        }


        //public static async ValueTask<Animator> LoadModelExAsync(
        //    this PathUnit entrypath, ZipArchive archive, CancellationToken ct)
        //=>
        //    archive == null
        //        ? await entrypath.LoadModelExAsync(ct)
        //        : await entrypath.LoadModelInArchiveExAsync(archive, ct);



        public static async ValueTask<GameObject> LoadModelExAsync(this PathUnit path, CancellationToken ct) =>
            await LoadErr.LoggingAsync(async () =>
        {
            ValueTask<Stream> openAsync_(PathUnit path) => path.OpenStreamFileOrWebAsync(ct);


            if (path.IsBlank()) return default;

            var (fullpath, queryString) = path.ToFullPath().DividToPathAndQueryString();
            fullpath.ThrowIfAccessedOutsideOfParentFolder();

            return fullpath.DividZipToArchiveAndEntry() switch
            {
                var (zippath, entrypath) when entrypath != "" =>
                    await openAsync_(zippath + queryString).UnzipAwait(entrypath, s => s.convertAsync(entrypath, ct)),
                var (zippath, _) when fullpath.IsZipArchive() =>
                    await openAsync_(zippath + queryString).UnzipFirstEntryAwait(".vrm;.glb", (s, path) => s.convertAsync(path, ct)),
                var (_, _) when fullpath.IsResource() =>
                    await fullpath.ToResourceName().loadModelFromResourceAsync(ct),
                var (_, _) =>
                    await openAsync_(fullpath + queryString).UsingAwait(s => s.convertAsync(fullpath, ct)),
            };
        });




        static ValueTask<GameObject> convertAsync(this Stream s, PathUnit path, CancellationToken ct) =>
            path.TrimQueryString()
                .GetExt()
                .ToLower()
            switch
            {
                ".vrm" => s.convertVrmToModelAsync(ct),
                ".glb" => s.convertGlbToModelAsync(ct),
                _ => default,
            };



        static async ValueTask<GameObject> convertGlbToModelAsync(this Stream s, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            using var m = new MemoryStream();
            await s.CopyToAsync(m, ct);

            using var data = new GlbBinaryParser(m.ToArray(), "_").Parse();
            using var context = new UniGLTF.ImporterContext(data);
            await Awaitable.MainThreadAsync();
            var instance = await context.LoadAsync(new VRMShaders.RuntimeOnlyNoThreadAwaitCaller());

            await Awaitable.MainThreadAsync();
            instance.ShowMeshes();
            ct.ThrowIfCancellationRequested(instance.gameObject.Destroy);

            return instance.gameObject.hideModel();
        }

        static async ValueTask<GameObject> convertVrmToModelAsync(this Stream s, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            using var m = new MemoryStream();
            await s.CopyToAsync(m, ct);

            await Awaitable.MainThreadAsync();
            var vrm10 = await Vrm10.LoadBytesAsync(m.ToArray(), true, ControlRigGenerationOption.None, ct: ct);

            await ct.ThrowIfCancellationRequested(vrm10.gameObject.DestroyOnMainThreadAsync);

            await Awaitable.MainThreadAsync();
            return vrm10.gameObject.hideModel();
        }

        static async ValueTask<GameObject> loadModelFromResourceAsync(this ResourceName name, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            await Awaitable.MainThreadAsync();
            var _go = await name.LoadAssetAsync<GameObject>();
            await Awaitable.MainThreadAsync();
            var go = GameObject.Instantiate(_go);
            Addressables.Release(_go);

            await ct.ThrowIfCancellationRequested(go.DestroyOnMainThreadAsync);

            await Awaitable.MainThreadAsync();
            return go.hideModel();
        }



        static GameObject hideModel(this GameObject go)
        {
            go.SetActive(false);
            return go;
        }
        static async ValueTask<GameObject> hideModelAsync(this GameObject go)
        {
            await Awaitable.MainThreadAsync();
            go.SetActive(false);
            return go;
        }

    }
}