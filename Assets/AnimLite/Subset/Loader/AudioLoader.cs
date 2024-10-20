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
    using AnimLite.Utility;
    using System.Runtime.InteropServices.ComTypes;

    static public class AudioLoader
    {

        // オーディオはストリームからだと audio clip にできない（デコード処理とかかかないといけない）ので、他のメディアと違うやり方
        // unity の UnityWebRequestMultimedia か addressables か、の２通り
        // ただし zip は stream からの方法をとったため、いったん tmp file にしてから UnityWebRequestMultimedia で読ませている





        //public static async ValueTask<AudioClipAsDisposable> LoadAudioClipExAsync(
        //    this PathUnit entrypath, ZipArchive archive, CancellationToken ct)
        //=>
        //    archive == null
        //        ? await entrypath.LoadAudioClipExAsync(ct)
        //        : await entrypath.LoadAudioClipInArchiveExAsync(archive, ct);


        public static async ValueTask<AudioClipAsDisposable> LoadAudioClipExAsync(
            this IArchive archive, PathUnit path, CancellationToken ct)
        {
            if (path.IsBlank()) return default;

            if (archive is not null && !path.IsFullPath())
            {
                var clip = await LoadErr.LoggingAsync(() =>
                    archive.ExtractAsync(path, s => s.loadAudioClipViaTmpFileAsync(path, ct)));

                if (!clip.clip.IsUnityNull()) return clip;

                if (archive.FallbackArchive is not null)
                    return await archive.FallbackArchive.LoadAudioClipExAsync(path, ct);
            }

            return await path.LoadAudioClipExAsync(ct);
        }

        static async ValueTask<AudioClipAsDisposable> loadAudioClipViaTmpFileAsync(
            this Stream stream, PathUnit path, CancellationToken ct)
        {
            var clip = await path.GetCachePathAsync(stream, ct).Await(LoadAudioClipAsync, ct);

            await Awaitable.MainThreadAsync();
            clip.clip.name = Path.GetFileNameWithoutExtension(path);

            return clip;
        }





        public static ValueTask<AudioClipAsDisposable> LoadAudioClipExAsync(this PathUnit path, CancellationToken ct) =>
            LoadErr.LoggingAsync(async () =>
        {

            ValueTask<Stream> openAsync_(PathUnit path) => path.OpenStreamFileOrWebAsync(ct);


            if (path.IsBlank()) return default;

            var (fullpath, queryString) = path.ToFullPath().DividToPathAndQueryString();
            fullpath.ThrowIfAccessedOutsideOfParentFolder();

            return fullpath.DividZipToArchiveAndEntry() switch
            {
                var (zippath, entrypath) when entrypath != "" =>
                    await openAsync_(zippath + queryString).UnzipAwait(entrypath, s => s.loadAudioClipViaTmpFileAsync(entrypath, ct)),
                var (zippath, _) when zippath != "" =>
                    await openAsync_(zippath + queryString).UnzipFirstEntryAwait(".mp3;.ogg;.acc;.wav", (s, n) => s.loadAudioClipViaTmpFileAsync(n, ct)),
                var (_, _) when fullpath.IsResource() =>
                    await fullpath.ToResourceName().LoadAudioClipFromResourceAsync(ct),
                var (_, _) =>
                    await (fullpath + queryString).LoadAudioClipAsync(ct),
            };
        });



        //public static async Awaitable<AudioClip> LoadAudioClipExAsync(this PathUnit path, CancellationToken ct) =>
        //    path.IsResource()
        //        ? await path.ToResourceName().LoadAudioClipFromResourceAsync(ct)
        //        : await path.LoadAudioClipAsync(ct);

        public static async ValueTask<AudioClipAsDisposable> LoadAudioClipAsync(
            this PathUnit path, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var atype = path
                .GetExt().ToPath()
                .TrimQueryString().Value
                .ToLower()
            switch
            {
                ".mp3" => AudioType.MPEG,
                ".ogg" => AudioType.OGGVORBIS,
                ".acc" => AudioType.ACC,
                ".wav" => AudioType.WAV,
                _ => AudioType.UNKNOWN,
            };
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"{atype} {path.Value} {File.Exists(path)}");
#endif
            //if (atype == AudioType.UNKNOWN || !File.Exists(path)) return default;
            if (atype == AudioType.UNKNOWN) return default;

            return await audio_(path, atype);


            async ValueTask<AudioClipAsDisposable> audio_(PathUnit path, AudioType audioType)
            {
                var schemedpath = path.IsHttp()
                    ? path
                    : $"file://{path.Value}".ToPath();// android だとスキーム必要ぽい

                await Awaitable.MainThreadAsync();
                using var req = UnityWebRequestMultimedia.GetAudioClip(schemedpath, audioType);

                //((DownloadHandlerAudioClip)req.downloadHandler).streamAudio = true;// graph.Evalute() での音ズレの原因である可能性ありなのでやめる
                ((DownloadHandlerAudioClip)req.downloadHandler).streamAudio = false;

                await req.SendWebRequest();

                var _clip = DownloadHandlerAudioClip.GetContent(req);
                if (_clip == null) return default;

                var clip = new AudioClipAsDisposable
                {
                    clip = _clip,
                    disposeAction = _clip.Destroy,
                };
                _clip.name = Path.GetFileNameWithoutExtension(path);

                ct.ThrowIfCancellationRequested(clip.Dispose);

                return clip;
            }
        }



        /// <summary>
        /// 
        /// </summary>
        public static async Task<AudioClipAsDisposable> LoadAudioClipFromResourceAsync(
            this ResourceName name, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            await Awaitable.MainThreadAsync();
            var clip = new AudioClipAsDisposable
            {
                clip = await name.LoadAssetAsync<AudioClip>(),
            };

            ct.ThrowIfCancellationRequested(clip.Dispose);

            return clip;
        }

    }



    [Serializable]
    public struct AudioClipAsDisposable : IDisposable
    {
        public AudioClip clip;// { get; private set; }
        public Action disposeAction { private get; set; }
        //public void Dispose() => this.disposeAction?.Invoke();
        public void Dispose()
        {
            this.disposeAction?.Invoke();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            $"dispose audio : {this.clip?.name}".ShowDebugLog();
#endif
        }

        public static implicit operator AudioClip(AudioClipAsDisposable src) => src.clip;
    }

}
