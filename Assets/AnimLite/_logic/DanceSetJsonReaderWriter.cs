using AnimLite.DancePlayable;
using AnimLite.Vmd;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UniVRM10;

namespace AnimLite.Utility
{
    using AnimLite.Utility.Linq;


    public class DanceSetJsonReaderWriter : MonoBehaviour
    {

        public DanceSetHolder Holder;


        [ContextMenu("save 'dance_set.json' to desktop")]
        private async Awaitable Write()
        {
            var desktoppath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            await this.Holder.WriteJsonAsync(desktoppath + "/dance_set.json", this.destroyCancellationToken);
        }

        [ContextMenu("load 'dance_set.json' from desktop")]
        private async Awaitable Read()
        {
            var audioSouce = this.Holder.dance.Audio.AudioSource;

            var desktoppath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            await this.Holder.ReadJsonAsync(desktoppath + "/dance_set.json", CancellationToken.None);// this.destroyCancellationToken);

            this.Holder.dance.Audio.AudioSource = audioSouce;
        }
    }


    public static class DanceSetJsonConverter
    {

        public static async Task WriteJsonAsync(this DanceSetHolder holder, PathUnit path, CancellationToken ct)
        {
            var danceset = holder.dance.ToDanceSetJson(holder.transform);

            var jsontext = JsonUtility.ToJson(danceset, prettyPrint: true);

            await System.IO.File.WriteAllTextAsync(path, jsontext);
        }

        public static async Awaitable<DanceSet> ReadJsonAsync(PathUnit path, CancellationToken ct)
        {
            if (Path.GetExtension(path) != ".json" || !File.Exists(path)) return default;

            var json = await json_(path, ct);

            return await json.ToDanceSetAsync(ct);


            async Task<DanceSetJson> json_(PathUnit path, CancellationToken ct)
            {
                var json = await File.ReadAllTextAsync(path, ct);

                return JsonUtility.FromJson<DanceSetJson>(json);
            }
        }

        public static async Awaitable ReadJsonAsync(this DanceSetHolder holder, PathUnit path, CancellationToken ct)
        {
            var ds = await ReadJsonAsync(path, ct);
            if (ds == default) return;

            var qMotionWithPos = ds.Motions.Where(motion => motion.OverWritePositionAndRotation);
            var qMotionWithoutPos = ds.Motions.Where(motion => !motion.OverWritePositionAndRotation);

            var tf = holder.transform;
            tf.GetChildren()
                .ForEach(tfc => GameObject.DestroyImmediate(tfc.gameObject));
            qMotionWithPos
                .ForEach(motion =>
                {
                    var def = new GameObject().AddComponent<DanceHumanDefine>();
                    def.transform.SetPositionAndRotation(motion.Position, motion.Rotation);
                    def.transform.SetParent(tf, worldPositionStays: true);

                    def.Motion = motion;
                });

            ds.Motions = qMotionWithoutPos.ToArray();
            holder.dance = ds;
        }

        public static async Awaitable<AudioClip> ReadAudioAsync(PathUnit path, CancellationToken ct)
        {
            var atype = Path.GetExtension(path) switch
            {
                ".mp3" => AudioType.MPEG,
                ".ogg" => AudioType.OGGVORBIS,
                ".acc" => AudioType.ACC,
                ".wav" => AudioType.WAV,
                _ => AudioType.UNKNOWN,
            };

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

        public static async Awaitable<Animator> LoadAnimatorResourceAsync(string path)
        {
            var name = path.ToLower().Split("as ")[0].Trim();

            var req = Resources.LoadAsync<GameObject>(name);
            await req;

            var go = req.asset as GameObject;
            return GameObject.Instantiate(go.GetComponent<Animator>());
        }
        public static async Awaitable<AudioClip> LoadAudioClipResourceAsync(string path)
        {
            var name = path.ToLower().Split("as ")[0].Trim();

            var req = Resources.LoadAsync<AudioClip>(name);
            await req;

            return req.asset as AudioClip;
        }

        public static async Awaitable<Animator> ReadModelAnimatorVrmAsync(PathUnit path, CancellationToken ct)
        {
            if (Path.GetExtension(path) != ".vrm" || !File.Exists(path)) return default;

            var vrm10 = await Vrm10.LoadPathAsync(
                path, true, ControlRigGenerationOption.None, true, null, null, null, null, ct);

            return vrm10.GetComponent<Animator>();
        }



        public static DanceSetJson ToDanceSetJson(this DanceSet src) =>
            new DanceSetJson
            {
                AudioPath = default,
                DelayTime = src.Audio.DelayTime,
                motions = src.Motions.Select(x => x.ToDanceMotionDefineJson()).ToArray(),
            };
        public static DanceSetJson ToDanceSetJson(this DanceSet src, Transform tf) =>
            new DanceSetJson
            {
                AudioPath = default,
                DelayTime = src.Audio.DelayTime,
                motions =
                    Enumerable.Concat(
                        src.Motions
                            .Select(x => x.ToDanceMotionDefineJson()),
                        tf.GetComponentsInChildren<DanceHumanDefine>()
                            .Select(x => x.Motion.ToDanceMotionDefineJson(x.transform)))
                    .ToArray(),
            };
        public static async Awaitable<DanceSet> ToDanceSetAsync(this DanceSetJson src, CancellationToken ct) =>
            new DanceSet
            {
                Audio = new AudioDefine
                {
                    AudioClip = src.AudioPath.EndsWith("as audioclip", StringComparison.OrdinalIgnoreCase)
                        ? await LoadAudioClipResourceAsync(src.AudioPath)
                        : await ReadAudioAsync(src.AudioPath.ToPath().ToFullPath(), ct),
                    DelayTime = src.DelayTime,
                },
                Motions = await src.motions.Select(x => x.ToDanceMotionDefineAsync(ct)).AwaitAllAsync(),
            };

        public static DanceMotionDefineJson ToDanceMotionDefineJson(this DanceMotionDefine src) =>
            new DanceMotionDefineJson
            {
                VrmFilePath = default,
                FaceMappingFilePath = src.FaceMappingFilePath,
                VmdFilePath = src.VmdFilePath,

                DelayTime = src.DelayTime,
                BodyScaleFromHuman = src.BodyScale,
                FootIkMode = src.FootIkMode,

                UsePositionAndDirection = false,//src.OverWritePositionAndRotation,
                Position = src.Position,
                EulerAngles = src.Rotation.eulerAngles,
            };
        public static DanceMotionDefineJson ToDanceMotionDefineJson(this DanceMotionDefine src, Transform tf) =>
            new DanceMotionDefineJson
            {
                VrmFilePath = default,
                FaceMappingFilePath = src.FaceMappingFilePath,
                VmdFilePath = src.VmdFilePath,

                DelayTime = src.DelayTime,
                BodyScaleFromHuman = src.BodyScale,
                FootIkMode = src.FootIkMode,

                UsePositionAndDirection = true,
                Position = tf.position,
                EulerAngles = tf.rotation.eulerAngles,
            };
        public static async Awaitable<DanceMotionDefine> ToDanceMotionDefineAsync(this DanceMotionDefineJson src, CancellationToken ct) =>
            new DanceMotionDefine
            {
                ModelAnimator = src.VrmFilePath.EndsWith("as animator", StringComparison.OrdinalIgnoreCase)
                    ? await LoadAnimatorResourceAsync(src.VrmFilePath)
                    : await ReadModelAnimatorVrmAsync(src.VrmFilePath.ToPath().ToFullPath(), ct),
                FaceMappingFilePath = src.FaceMappingFilePath.ToPath().ToFullPath(),
                VmdFilePath = src.VmdFilePath.ToPath().ToFullPath(),
                FaceRenderer = null,

                DelayTime = src.DelayTime,
                FootIkMode = src.FootIkMode,
                BodyScale = src.BodyScaleFromHuman,

                OverWritePositionAndRotation = src.UsePositionAndDirection,
                Position = src.Position,
                Rotation = Quaternion.Euler(src.EulerAngles),
            };
    }



    [System.Serializable]
    public class DanceSetJson
    {
        public string AudioPath;
        public float DelayTime;
        public DanceMotionDefineJson[] motions;
    }
    [System.Serializable]
    public class DanceMotionDefineJson
    {
        public string VrmFilePath;
        public string FaceMappingFilePath;
        public string VmdFilePath;

        public float DelayTime;
        public float BodyScaleFromHuman;
        public VmdFootIkMode FootIkMode;

        public bool UsePositionAndDirection;
        public Vector3 Position;
        public Vector3 EulerAngles;
    }
}
