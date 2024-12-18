﻿using System;
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
using UniGLTF;

namespace AnimLite.Loader
{
    using AnimLite.Utility;
    using AnimLite.Vmd;
    using AnimLite.Vrm;

    // todo
    // リソース時、gameobject でロードできなかったら .vrm でロードする


    public static partial class VrmLoader
    {




        public static async ValueTask<GameObject> LoadModelAsync(
            this IArchive archive, PathUnit path, CancellationToken ct)
        {
            if (path.IsBlank()) return default;

            if (archive is not null && !path.IsFullPath())
            {
                var model = await LoadErr.LoggingAsync(() =>
                    archive.GetEntryAsync(path, s => s.convertAsync(path, ct), ct));

                if (model is not null)
                    return model;

                if (archive.FallbackArchive is not null)
                    return await archive.FallbackArchive.LoadModelAsync(path, ct);
            }

            return await path.LoadModelAsync(ct);
        }


        //public static async ValueTask<Animator> LoadModelExAsync(
        //    this PathUnit entrypath, ZipArchive archive, CancellationToken ct)
        //=>
        //    archive == null
        //        ? await entrypath.LoadModelExAsync(ct)
        //        : await entrypath.LoadModelInArchiveExAsync(archive, ct);



        public static async ValueTask<GameObject> LoadModelAsync(this PathUnit path, CancellationToken ct) =>
            await LoadErr.LoggingAsync(async () =>
        {
            ValueTask<Stream> openAsync_(PathUnit path) => path.OpenStreamFileOrWebAsync(ct);


            if (path.IsBlank()) return default;

            var (fullpath, queryString) = path.ToFullPath().DividToPathAndQueryString();
            fullpath.ThrowIfAccessedOutsideOfParentFolder();

            return fullpath.DividZipToArchiveAndEntry() switch
            {
                var (zippath, entrypath) when entrypath != "" =>
                    await openAsync_(zippath + queryString)
                        .UsingAwait(s => s.UnzipAsync(entrypath, s => s.convertAsync(entrypath, ct))),
                var (zippath, _) when fullpath.IsZipArchive() =>
                    await openAsync_(zippath + queryString)
                        .UsingAwait(s => s.UnzipFirstEntryAsync(".vrm;.glb", (s, path) => s.convertAsync(path, ct))),
                var (_, _) when fullpath.IsResource() =>
                    await fullpath.ToResourceName()
                        .loadModelFromResourceAsync(ct),
                var (_, _) =>
                    await openAsync_(fullpath + queryString)
                        .UsingAwait(s => s.convertAsync(fullpath, ct)),
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



        //static async ValueTask<GameObject> convertGlbToModelAsync(this Stream s, CancellationToken ct)
        //{
        //    ct.ThrowIfCancellationRequested();

        //    using var m = new MemoryStream();
        //    await s.CopyToAsync(m, ct);

        //    using var data = new GlbBinaryParser(m.ToArray(), "_").Parse();
        //    await Awaitable.MainThreadAsync();
        //    using var context = new UniGLTF.ImporterContext(data);
        //    var instance = await context.LoadAsync(new UniGLTF.RuntimeOnlyNoThreadAwaitCaller());

        //    await Awaitable.MainThreadAsync();
        //    instance.ShowMeshes();
        //    ct.ThrowIfCancellationRequested(instance.gameObject.Destroy);

        //    return instance.gameObject.hideModel();
        //}
        static async ValueTask<GameObject> convertGlbToModelAsync(this Stream s, CancellationToken ct)
        {

            if (s.CanSeek)// もっとうまいことできないかなぁ…
            {
                return await convert_(s);
            }

            using var m = new MemoryStream();
            await s.CopyToAsync(m, ct);
            m.Seek(0, SeekOrigin.Begin);

            return await convert_(m);


            async ValueTask<GameObject> convert_(Stream s)
            {
                ct.ThrowIfCancellationRequested();

                using var br = new System.IO.BinaryReader(s);
                using var data = new GlbBinaryParser(br.ReadBytes((int)s.Length), "_").Parse();

                await Awaitable.MainThreadAsync();
                using var context = new UniGLTF.ImporterContext(data);
                var instance = await context.LoadAsync(new UniGLTF.RuntimeOnlyNoThreadAwaitCaller());

                await Awaitable.MainThreadAsync();
                instance.ShowMeshes();

                await ct.ThrowIfCancellationRequested(instance.gameObject.DestroyOnMainThreadAsync);

                return await instance.gameObject.hideModelAsync();
            }
        }

        //static async ValueTask<GameObject> convertVrmToModelAsync(this Stream s, CancellationToken ct)
        //{
        //    ct.ThrowIfCancellationRequested();

        //    using var m = new MemoryStream();
        //    await s.CopyToAsync(m, ct);

        //    await Awaitable.MainThreadAsync();
        //    var vrm10 = await Vrm10.LoadBytesAsync(m.ToArray(), true, ControlRigGenerationOption.None, ct: ct);

        //    await ct.ThrowIfCancellationRequested(vrm10.gameObject.DestroyOnMainThreadAsync);

        //    await Awaitable.MainThreadAsync();
        //    return vrm10.gameObject.hideModel();
        //}
        static async ValueTask<GameObject> convertVrmToModelAsync(this Stream s, CancellationToken ct)
        {

            if (s.CanSeek)// もっとうまいことできないかなぁ…
            {
                return await convert_(s);
            }

            using var m = new MemoryStream();
            await s.CopyToAsync(m, ct);
            m.Seek(0, SeekOrigin.Begin);

            return await convert_(m);

            async ValueTask<GameObject> convert_(Stream s)
            {
                ct.ThrowIfCancellationRequested();

                using var br = new System.IO.BinaryReader(s);

                await Awaitable.MainThreadAsync();
                var vrm10 = await Vrm10.LoadBytesAsync(br.ReadBytes((int)s.Length), true, ControlRigGenerationOption.None, ct: ct);

                await ct.ThrowIfCancellationRequested(vrm10.gameObject.DestroyOnMainThreadAsync);

                return await vrm10.gameObject.hideModelAsync();
            }
        }

        // プレハブリソースを返す
        static async ValueTask<GameObject> loadModelFromResourceAsync(this ResourceName name, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            await Awaitable.MainThreadAsync();
            var go = await name.LoadAssetAsync<GameObject>();

            ct.ThrowIfCancellationRequested(go.Release);

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