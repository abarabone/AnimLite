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

namespace AnimLite.Vmd
{
    using AnimLite.Utility;


    public static partial class AnimationClipLoader
    {



        public static ValueTask<AnimationClip> loadAnimationClipFromResourceAsync(
            this ResourceName name, CancellationToken ct)
        =>
            LoadErr.LoggingAsync(async () =>
        {
            if ((name.Value ?? "") == "") return default;


            ct.ThrowIfCancellationRequested();

            await Awaitable.MainThreadAsync();
            var clip = await name.LoadAssetAsync<AnimationClip>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"load anim clip {clip?.name}");
#endif

            ct.ThrowIfCancellationRequested(clip.Release);

            return clip;
        });



    }
}
