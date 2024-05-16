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
using AnimLite.Vrm;


namespace AnimLite.Utility
{

    static public class ResourceLoadUtility
    {

        public static async Awaitable<AudioClip> LoadAudioClipExAsync(this PathUnit path, CancellationToken ct) =>
            path.Value.EndsWith("as resource", StringComparison.OrdinalIgnoreCase)
                ? await path.LoadAudioClipFromResourceAsync(ct)
                : await path.LoadAudioClipAsync(ct);

        public static async Awaitable<AudioClip> LoadAudioClipAsync(this PathUnit path, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var atype = Path.GetExtension(path) switch
            {
                ".mp3" => AudioType.MPEG,
                ".ogg" => AudioType.OGGVORBIS,
                ".acc" => AudioType.ACC,
                ".wav" => AudioType.WAV,
                _ => AudioType.UNKNOWN,
            };
            // Debug.Log($"{atype} {path.Value} {File.Exists(path)}");

            if (atype == AudioType.UNKNOWN || !File.Exists(path)) return default;

            return await audio_(path, atype);


            async Awaitable<AudioClip> audio_(PathUnit path, AudioType audioType)
            {
                using var req = UnityWebRequestMultimedia.GetAudioClip(path, audioType);

                ((DownloadHandlerAudioClip)req.downloadHandler).streamAudio = true;

                await req.SendWebRequest();

                var clip = DownloadHandlerAudioClip.GetContent(req);
                clip.name = Path.GetFileNameWithoutExtension(path);
                return clip;
            }
        }

        public static async Awaitable<AudioClip> LoadAudioClipFromResourceAsync(this PathUnit path, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var name = path.Value.ToLower().Split("as ")[0].Trim();

            var req = Resources.LoadAsync<AudioClip>(name);
            await req;

            return req.asset as AudioClip;
        }



        public static async Awaitable<Animator> LoadModelExAsync(this PathUnit path, CancellationToken ct) =>
            path.Value.EndsWith("as resource", StringComparison.OrdinalIgnoreCase)
                ? await path.LoadModelFromResourceAsync(ct)
                : await path.LoadModelFromVrmAsync(ct);

        public static async Awaitable<Animator> LoadModelFromResourceAsync(this PathUnit path, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var name = path.Value.ToLower().Split("as ")[0].Trim();

            var req = Resources.LoadAsync<GameObject>(name);
            await req;

            var go = req.asset as GameObject;
            return GameObject.Instantiate(go.GetComponent<Animator>());
        }

        public static async Awaitable<Animator> LoadModelFromVrmAsync(this PathUnit path, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (Path.GetExtension(path) != ".vrm" || !File.Exists(path)) return default;

            var vrm10 = await Vrm10.LoadPathAsync(
                path, true, ControlRigGenerationOption.None, true, null, null, null, null, ct);

            return vrm10.GetComponent<Animator>();
        }


        /// <summary>
        /// path がブランク … デフォルトリソース（存在しなければ default が返る）
        /// as resourse     … リソース（存在しなければ default が返る）
        /// その他          … ファイル（失敗時はエラー）
        /// </summary>
        public static async Awaitable<VmdFaceMapping> LoadFaceMapExAsync(this PathUnit path, CancellationToken ct)
        {
            if (path.IsBlank()) path = "face_map_default as resource";
            
            return path.Value.EndsWith("as resource", StringComparison.OrdinalIgnoreCase)
                ? await path.LoadFaceMapFromResourceAsync(ct)
                : await VrmParser.ParseFaceMapAsync(path, ct);
        }

        /// <summary>
        /// リソースが存在しない場合は、default が返る
        /// </summary>
        public static async Awaitable<VmdFaceMapping> LoadFaceMapFromResourceAsync(this PathUnit path, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            
            var name = path.Value.ToLower().Split("as ")[0].Trim();

            await Awaitable.MainThreadAsync();
            var req = Resources.LoadAsync<TextAsset>(name);
            await req;
            if (req.asset == default) return default;

            return await VrmParser.ParseFaceMapAsync(req.asset as TextAsset, ct);
        }

    }

}
