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

    using AnimLite.Vmd;
    using AnimLite.Vrm;
    using AnimLite.Loader;


    public class VmdStreamCorePrototype : Prototype<CoreVmdStreamData, VmdStreamData>
    {
        public VmdStreamCorePrototype(CoreVmdStreamData vmdcore) : base(vmdcore)
        {
            this.InstantiateActionAsync = vmdcore =>
                new ValueTask<VmdStreamData>(vmdcore.CloneShallowlyWithCache());

            this.DisposeInstanceActionAsync = vmdinstance =>
            {
                vmdinstance.Dispose();
                return new ValueTask();
            };

            this.DisposeActionAsync = vmdcore =>
            {
                vmdcore.Dispose();
                return new ValueTask();
            };
        }
    }

    public class AudioClipPrototype : Prototype<AudioClip>
    {
        public AudioClipPrototype(AudioClip clip) : base(clip)
        {
            this.DisposeActionAsync = async clip =>
            {
                await Awaitable.MainThreadAsync();
                clip.UnloadAudioData();
                clip.Destroy();
            };
        }
    }




    public static class PrototypeExtension
    {

        // 単なるダミーの IPrototype を作って持たせる方がいいかも？めんどくさいか、場合分けが
        public static async ValueTask<Instance<GameObject>> LoadModelInstanceAsync(
            this IArchive archive, PathUnit path, CancellationToken ct)
        {
            var prototype = await archive.LoadModelPrototypeAsync(path, ct);
            if (prototype is null) return null;

            //await Awaitable.MainThreadAsync();
            var i = await prototype.InstantiateAsync();

            await prototype.DisposeAsync();
            return i;
        }

        public static async ValueTask<IPrototype<GameObject>> LoadModelPrototypeAsync(
            this IArchive archive, PathUnit path, CancellationToken ct)
        {
            var model = await archive.LoadModelAsync(path, ct);
            if (model.IsUnityNull()) return null;

            return path.IsResource() switch
            {
                true =>
                    new ModelPrefab(model),
                false =>
                    new ModelOrigin(model),
            };
        }


        public static async ValueTask<IPrototype<AnimationClip>> LoadAnimationClipPrototypeAsync(
            this ResourceName name, CancellationToken ct)
        {
            var clip = await name.loadAnimationClipFromResourceAsync(ct);
            if (clip.IsUnityNull()) return null;

            return new Resource<AnimationClip>(clip);
        }


        public static async ValueTask<(IPrototype<VmdStreamData> vmdstream, IPrototype<VmdFaceMapping> facemap)> LoadVmdStreamDataPrototypeAsync(
            this IArchive archive, PathList pathlist, PathUnit facemapPath, CancellationToken ct)
        {
            var vmddata = await archive.LoadVmdExAsync(pathlist, ct);
            var facemap = await archive.LoadFaceMapAsync(facemapPath, ct);

            var vmdcore = vmddata.BuildStreamCoreData(facemap, ct);
            var vmdprottype = new VmdStreamCorePrototype(vmdcore);

            return (vmdprottype, facemap.ToPrototype());
        }


        public static async ValueTask<IPrototype<AudioClip>> LoadAudioClipPrototypeAsync(
            this IArchive archive, PathUnit path, CancellationToken ct)
        {
            var clip = await archive.LoadAudioClipAsync(path, ct);
            if (clip.IsUnityNull()) return null;

            return path switch
            {
                _ when path.IsResource() =>
                    new Resource<AudioClip>(clip),
                _ =>
                    new AudioClipPrototype(clip),
            };
        }


    }








}
