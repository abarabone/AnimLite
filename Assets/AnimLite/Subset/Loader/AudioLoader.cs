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
            this PathUnit path, ZipArchive archive, CancellationToken ct)
        {

            if (archive != null && !path.IsFullPath())
            {
                var clip = await archive.UnzipAsync(path, s => s.LoadAudioClipViaTmpFileAsync(path, ct));

                if (clip.clip != null) return clip;
            }

            return await path.LoadAudioClipExAsync(ct);
        }





        public static async ValueTask<AudioClipAsDisposable> LoadAudioClipExAsync(this PathUnit path, CancellationToken ct)
        {
            ValueTask<Stream> openAsync_(PathUnit path) => path.OpenStreamFileOrWebAsync(ct);

            var fullpath = path.ToFullPath();
            fullpath.ThrowIfAccessedOutsideOfParentFolder();

            return fullpath.DividZipAndEntry() switch
            {
                var (zippath, entrypath) when entrypath != "" =>
                    await openAsync_(zippath).UnzipAsync(entrypath, s => s.LoadAudioClipViaTmpFileAsync(entrypath, ct)),
                var (zippath, _) when zippath != "" =>
                    await openAsync_(zippath).UnzipFirstEntryAsync(".mp3;.ogg;.acc;.wav", (s, n) => s.LoadAudioClipViaTmpFileAsync(n, ct)),
                var (_, _) when fullpath.IsResource() =>
                    await fullpath.ToResourceName().LoadAudioClipFromResourceAsync(ct),
                var (_, _) =>
                    await fullpath.LoadAudioClipAsync(ct),
            };
        }



        //public static async Awaitable<AudioClip> LoadAudioClipExAsync(this PathUnit path, CancellationToken ct) =>
        //    path.IsResource()
        //        ? await path.ToResourceName().LoadAudioClipFromResourceAsync(ct)
        //        : await path.LoadAudioClipAsync(ct);

        public static async ValueTask<AudioClipAsDisposable> LoadAudioClipAsync(
            this PathUnit path, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var atype = Path.GetExtension(path) switch
            {
                var x when x.StartsWith(".mp3") => AudioType.MPEG,
                var x when x.StartsWith(".ogg") => AudioType.OGGVORBIS,
                var x when x.StartsWith(".acc") => AudioType.ACC,
                var x when x.StartsWith(".wav") => AudioType.WAV,
                _ => AudioType.UNKNOWN,
            };
#if UNITY_EDITOR
            Debug.Log($"{atype} {path.Value} {File.Exists(path)}");
#endif
            //if (atype == AudioType.UNKNOWN || !File.Exists(path)) return default;
            if (atype == AudioType.UNKNOWN) return default;

            return await audio_(path, atype);


            async ValueTask<AudioClipAsDisposable> audio_(PathUnit path, AudioType audioType)
            {
                await Awaitable.MainThreadAsync();
                using var req = UnityWebRequestMultimedia.GetAudioClip(path, audioType);

                ((DownloadHandlerAudioClip)req.downloadHandler).streamAudio = true;

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

        public static async ValueTask<AudioClipAsDisposable> LoadAudioClipViaTmpFileAsync(
            this Stream s, string name, CancellationToken ct)
        {
            await Awaitable.MainThreadAsync();
            var tmppath = $"{Application.temporaryCachePath}/{Path.GetFileName(name)}".ToPath();
            using var fileCleaner = new Disposable(() => File.Delete(tmppath));
            tmppath.Value.ShowDebugLog();

            {
                using var writer = new StreamWriter(tmppath, append: false);
                await s.CopyToAsync(writer.BaseStream);
            }

            return await tmppath.LoadAudioClipAsync(ct);
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

    public struct AudioClipAsDisposable : IDisposable
    {
        public AudioClip clip;// { get; private set; }
        public Action disposeAction { private get; set; }
        //public void Dispose() => this.disposeAction?.Invoke();
        public void Dispose()
        {
            this.disposeAction?.Invoke();
            $"dispose audio : {this.clip?.name}".ShowDebugLog();
        }

        public static implicit operator AudioClip(AudioClipAsDisposable src) => src.clip;
    }

}
