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
using UnityEngine.Animations;

namespace AnimLite.Utility
{





    public static class ModelPrototypeExtension
    {


        public static async ValueTask<Instance<GameObject>> LoadModelInstanceAsync(
            this IArchive archive, PathUnit path, PrototypeReleaseMode mode, CancellationToken ct)
        {
            var prototype = await archive.LoadModelPrototypeAsync(path, mode, ct);

            await Awaitable.MainThreadAsync();
            return await prototype.InstantiateAsync();
        }

        public static async ValueTask<IPrototype<GameObject>> LoadModelPrototypeAsync(
            this IArchive archive, PathUnit path, PrototypeReleaseMode mode, CancellationToken ct)
        {
            var model = await archive.LoadModelExAsync(path, ct);
            if (model.IsUnityNull()) return null;

            return path.IsResource() switch
            {
                true =>
                    new ModelPrefab(model, mode),
                false =>
                    new ModelOrigin(model, mode),
            };
        }


        public static async ValueTask<IPrototype<AnimationClip>> LoadAnimationClipAsync(
            this ResourceName name, PrototypeReleaseMode mode, CancellationToken ct)
        {
            var clip = await name.loadAnimationClipFromResourceAsync(ct);
            if (clip.IsUnityNull()) return null;

            return new Resource<AnimationClip>(clip, mode);
        }


    }








}
