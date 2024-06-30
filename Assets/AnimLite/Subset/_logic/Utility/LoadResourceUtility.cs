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

namespace AnimLite.Utility
{
    using AnimLite.Vmd;
    using AnimLite.Vrm;


    static public class ResourceLoadUtility
    {


        public static Task<T> LoadAssetAsync<T>(this PathUnit path)
        {
            return Addressables.LoadAssetAsync<T>(path.Value).Task;
        }



        public static async Awaitable<string> ReadTextExAsync(this PathUnit textfilepath, CancellationToken ct) =>
            textfilepath.IsResource()
                ? await textfilepath.LoadTextFromResourceAsync(ct)
                : await File.ReadAllTextAsync(textfilepath, ct);

        public static async Awaitable<string> LoadTextFromResourceAsync(this PathUnit filepath, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var name = filepath.ToPathForResource();

            //await Awaitable.MainThreadAsync();
            var asset = await name.LoadAssetAsync<TextAsset>();

            var text = asset.text;// �����[�X������A�N�Z�X�G���[�ɂȂ�Ȃ����H
            Addressables.Release(asset);

            return text;
        }



        /// <summary>
        /// �N���b�v�͎g���I�������j�����ׂ��Ǝv���
        /// </summary>
        public static async Awaitable<AudioClip> LoadAudioClipExAsync(this PathUnit path, CancellationToken ct) =>
            path.IsResource()
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

                if (ct.IsCancellationRequested) UnityEngine.Object.Destroy(clip);
                ct.ThrowIfCancellationRequested();

                return clip;
            }
        }

        /// <summary>
        /// �Ԃ��ꂽ�N���b�v�̓��\�[�X���畡���������̂Ȃ̂ŁA�j���K�v�Ǝv����
        /// �i���ʂ�������Ȃ����A�����݂����낦�邽�߁j
        /// </summary>
        public static async Task<AudioClip> LoadAudioClipFromResourceAsync(this PathUnit filepath, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var name = filepath.ToPathForResource();

            await Awaitable.MainThreadAsync();
            var _clip = await name.LoadAssetAsync<AudioClip>();
            var clip = UnityEngine.Object.Instantiate(_clip);
            Addressables.Release(_clip);

            if (ct.IsCancellationRequested) UnityEngine.Object.Destroy(clip);
            ct.ThrowIfCancellationRequested();

            return clip;
        }





        public static async Awaitable<T> ReadJsonExAsync<T>(this PathUnit jsonfilepath, CancellationToken ct) =>
            jsonfilepath.IsResource()
                ? await jsonfilepath.LoadJsonFromResourceAsync<T>(ct)
                : await jsonfilepath.ReadJsonAsync<T>(ct);

        public static async Awaitable<T> LoadJsonFromResourceAsync<T>(this PathUnit jsonfilepath, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var name = jsonfilepath.ToPathForResource();

            await Awaitable.MainThreadAsync();
            var asset = await name.LoadAssetAsync<TextAsset>();

            var json = asset.text;
            var content = JsonUtility.FromJson<T>(json);

            Addressables.Release(asset);
            return content;
        }

        public static async Task<T> ReadJsonAsync<T>(this PathUnit jsonfilepath, CancellationToken ct)
        {
            var json = await File.ReadAllTextAsync(jsonfilepath, ct);

            return JsonUtility.FromJson<T>(json);
        }

    }

}

namespace AnimLite.Vrm
{
    using AnimLite.Utility;

    public static partial class VrmParser
    {


        public static async Task<Animator> LoadModelExAsync(this PathUnit path, CancellationToken ct) =>
            path.IsResource()
                ? await path.LoadModelFromResourceAsync(ct)
                : await path.LoadModelFromVrmAsync(ct);

        public static async Task<Animator> LoadModelFromVrmAsync(this PathUnit path, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (Path.GetExtension(path) != ".vrm" || !File.Exists(path)) return default;

            var vrm10 = await Vrm10.LoadPathAsync(
                path, true, ControlRigGenerationOption.None, true, null, null, null, null, ct);

            if (ct.IsCancellationRequested) GameObject.Destroy(vrm10.gameObject);
            ct.ThrowIfCancellationRequested();

            return vrm10.GetComponent<Animator>();
        }

        public static async Task<Animator> LoadModelFromResourceAsync(this PathUnit filepath, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var name = filepath.ToPathForResource();


            await Awaitable.MainThreadAsync();
            var _go = await name.LoadAssetAsync<GameObject>();
            var go = GameObject.Instantiate(_go);
            Addressables.Release(_go);

            if (ct.IsCancellationRequested) GameObject.Destroy(go);
            ct.ThrowIfCancellationRequested();

            return go.GetComponent<Animator>();
        }



        /// <summary>
        /// path ���u�����N �c �f�t�H���g���\�[�X�i���݂��Ȃ���� default ���Ԃ�j
        /// as resourse     �c ���\�[�X�i���݂��Ȃ���� default ���Ԃ�j
        /// ���̑�          �c �t�@�C���i���s���̓G���[�j
        /// </summary>
        public static async Awaitable<VmdFaceMapping> ParseFaceMapExAsync(this PathUnit path, CancellationToken ct)
        {
            if (path.IsBlank()) path = "face_map_default as resource";

            return path.IsResource()
                ? await path.LoadFaceMapFromResourceAsync(ct)
                : await VrmParser.ParseFaceMapAsync(path, ct);
        }

        /// <summary>
        /// ���\�[�X�����݂��Ȃ��ꍇ�́Adefault ���Ԃ�
        /// </summary>
        public static async Awaitable<VmdFaceMapping> LoadFaceMapFromResourceAsync(this PathUnit filepath, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var name = filepath.ToPathForResource();


            await Awaitable.MainThreadAsync();
            var asset = await name.LoadAssetAsync<TextAsset>();
            //var asset = await Err<UnityException>.OnErrToDefault(async () =>
            //{
            //    return await name.LoadAssetAsync<TextAsset>();
            //});
            //if (asset == default) return default;

            var text = asset.text;
            ct.ThrowIfCancellationRequested();

            var result = VrmParser.ParseFaceMapFromText(text);
            Addressables.Release(asset);
            return result;
        }

    }

}
