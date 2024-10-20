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
using Unity.VisualScripting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace AnimLite.Utility.old
{
    using AnimLite.DancePlayable;
    using AnimLite.Vmd;
    using AnimLite.Vrm;
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;


    public class DanceSetJsonReaderWriter : MonoBehaviour
    {

        public DanceSetHolder Holder;


        [ContextMenu("save 'dance_set.json' to desktop")]
        private async Awaitable Write()
        {
            try
            {
                var desktoppath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                await this.Holder.WriteJsonAsync(desktoppath + "/dance_set.json", this.destroyCancellationToken);
                Debug.Log($"write to {desktoppath}/dance_set.json");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [ContextMenu("load 'dance_set.json' from desktop")]
        private async Awaitable Read()
        {
            var desktoppath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            await this.Holder.ReadJsonAsync(desktoppath + "/dance_set.json", this.destroyCancellationToken);
            Debug.Log($"read from {desktoppath}/dance_set.json");
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

        //public static async Awaitable<DanceSet> ReadJsonAsync(PathUnit path, AudioSource audiosrc, CancellationToken ct)
        //{
        //    //if (Path.GetExtension(path) != ".json" || !File.Exists(path)) return default;

        //    //var json = await json_(path, ct);
        //    var json = await path.ReadJsonExAsync<DanceSetJson>(ct);

        //    return await json.ToDanceSetAsync(audiosrc, ct);


        //    //async Task<DanceSetJson> json_(PathUnit path, CancellationToken ct)
        //    //{
        //    //    var json = await File.ReadAllTextAsync(path, ct);

        //    //    return JsonUtility.FromJson<DanceSetJson>(json);
        //    //}
        //}

        public static async Task ReadJsonAsync(this DanceSetHolder holder, PathUnit path, CancellationToken ct)
        {
            //var ds = await ReadJsonAsync(path, holder.dance.Audio.AudioSource, ct);
            var json = await path.LoadJsonAsync<DanceSetJson>(ct);
            var ds = await json.ToDanceSetAsync(holder.dance.Audio.AudioSource, ct);
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
        static IEnumerable<Transform> GetChildren(this Transform tfSelf)
        {
            foreach (var tf in tfSelf)
            {
                yield return tf as Transform;
            }
        }



        public static DanceSetJson ToDanceSetJson(this DanceSet src) =>
            new DanceSetJson
            {
                Audio = new()
                {
                    AudioFilePath = src.Audio.AudioClip.IsUnityNull()
                        ? ""
                        : $"{src.Audio.AudioClip.clip.name} as resource",
                    Volume = src.Audio.AudioSource?.volume ?? 1.0f,
                    DelayTime = src.Audio.DelayTime,
                },

                //DefaultAnimation = new ()
                //{
                //    AnimationFilePath = ,
                //    FaceMappingFilePath =,
                //    DelayTime =,
                //},

                CaptionMode = "simple",
                AudioInformation = src.AudioInfo,
                AnimationInformation = src.AnimationInfo,

                Motions = src.Motions
                    .Select(x => x.ToDanceMotionDefineJson())
                    .ToArray(),
            };
        public static DanceSetJson ToDanceSetJson(this DanceSet src, Transform tf) =>
            new DanceSetJson
            {
                Audio = new()
                {
                    AudioFilePath = src.Audio.AudioClip.IsUnityNull()
                        ? ""
                        : $"{src.Audio.AudioClip.clip.name} as resource",
                    Volume = src.Audio.AudioSource?.volume ?? 1.0f,
                    DelayTime = src.Audio.DelayTime,
                },

                //DefaultAnimation =,

                CaptionMode = "simple",
                AudioInformation = src.AudioInfo ?? default,
                AnimationInformation = src.AnimationInfo ?? default,

                Motions =
                    Enumerable.Concat(
                        src.Motions
                            .Select(x => x.ToDanceMotionDefineJson()),
                        tf.GetComponentsInChildren<DanceHumanDefine>()
                            .Select(x => x.Motion.ToDanceMotionDefineJson(x.transform)))
                    .ToArray(),
            };

        public static async ValueTask<DanceSet> ToDanceSetAsync(this DanceSetJson src, AudioSource audiosrc, CancellationToken ct) =>
            new DanceSet
            {
                Audio = new AudioDefine
                {
                    AudioClip = await src.Audio.AudioFilePath.ToPath().LoadAudioClipExAsync(ct),
                    DelayTime = src.Audio.DelayTime,
                    AudioSource = setvolume_(audiosrc, src.Audio.Volume),
                },

                //DefaultAnimation = ,

                AudioInfo = src.AudioInformation,
                AnimationInfo = src.AnimationInformation,

                Motions = await src.Motions
                    .Select(x => x.ToDanceMotionDefineAsync(ct))
                    .WhenAll(),
            };
        static AudioSource setvolume_(AudioSource src, float volume)
        {
            src.volume = volume;
            return src;
        }


        public static DanceMotionDefineJson ToDanceMotionDefineJson(this DanceMotionDefine src) =>
            new DanceMotionDefineJson
            {
                Model = new ModelDefineJson
                {
                    ModelFilePath = $"{src.Model.name} as resource",
                    //UsePositionAndDirection = false,
                    Position = src.Position,
                    EulerAngles = src.Rotation.eulerAngles,
                    Scale = src.Scale,
                },
                Animation = new AnimationDefineJson
                {
                    AnimationFilePath = src.AnimationFilePath,
                    FaceMappingFilePath = src.FaceMappingFilePath,
                    DelayTime = src.DelayTime,
                },
                Options = new MotionOptionsJson
                {
                    BodyScaleFromHuman = src.BodyScale,
                    FootIkMode = src.FootIkMode,
                },
                ModelInformation = src.ModelInfo,
                AnimationInformation = src.AnimationInfo,
            };
        public static DanceMotionDefineJson ToDanceMotionDefineJson(this DanceMotionDefine src, Transform tf) =>
            new DanceMotionDefineJson
            {
                Model = new ModelDefineJson
                {
                    ModelFilePath = $"{src.Model.name} as resource",
                    //UsePositionAndDirection = true,
                    Position = tf.position,
                    EulerAngles = tf.rotation.eulerAngles,
                    Scale = tf.localScale.y,    // とりあえず高さがスケールの基本とする
                },
                Animation = new AnimationDefineJson
                {
                    AnimationFilePath = src.AnimationFilePath,
                    FaceMappingFilePath = src.FaceMappingFilePath,
                    DelayTime = src.DelayTime,
                },
                Options = new MotionOptionsJson
                {
                    BodyScaleFromHuman = src.BodyScale,
                    FootIkMode = src.FootIkMode,
                },
                ModelInformation = src.ModelInfo,
                AnimationInformation = src.AnimationInfo,
            };
        public static async ValueTask<DanceMotionDefine> ToDanceMotionDefineAsync(this DanceMotionDefineJson src, CancellationToken ct) =>
            new DanceMotionDefine
            {
                Model = await src.Model.ModelFilePath.ToPath().LoadModelExAsync(ct),
                FaceMappingFilePath = src.Animation.FaceMappingFilePath.ToPath(),
                AnimationFilePath = src.Animation.AnimationFilePath.ToPath(),
                FaceRenderer = null,

                DelayTime = src.Animation.DelayTime,
                FootIkMode = src.Options.FootIkMode,
                BodyScale = src.Options.BodyScaleFromHuman,

                //OverWritePositionAndRotation = src.Model.UsePositionAndDirection,
                OverWritePositionAndRotation = !(src.Model.Position.IsZero() && src.Model.EulerAngles.IsZero()),
                Position = src.Model.Position,
                Rotation = Quaternion.Euler(src.Model.EulerAngles),
                Scale = src.Model.Scale,

                ModelInfo = src.ModelInformation,
                AnimationInfo = src.AnimationInformation,
            };
    }



    [System.Serializable]
    public struct DanceSetJson
    {
        public AudioDefineJson Audio;
        public AnimationDefineJson DefaultAnimation;

        public ModelDefineJson[] BackGrounds;
        public DanceMotionDefineJson[] Motions;

        public string CaptionMode;
        public InformationDefine AudioInformation;
        public InformationDefine AnimationInformation;
    }
    [System.Serializable]
    public struct DanceMotionDefineJson
    {
        public ModelDefineJson Model;
        public AnimationDefineJson Animation;
        public MotionOptionsJson Options;

        public InformationDefine ModelInformation;
        public InformationDefine AnimationInformation;
    }

    [System.Serializable]
    public class AnimationDefineJson
    {
        public string AnimationFilePath;
        public string FaceMappingFilePath;

        public float DelayTime;
    }
    [System.Serializable]
    public class AudioDefineJson
    {
        public string AudioFilePath;

        public float Volume;
        public float DelayTime;
    }
    [System.Serializable]
    public class ModelDefineJson
    {
        public string ModelFilePath;

        //public bool UsePositionAndDirection;
        public Vector3 Position;
        public Vector3 EulerAngles;
        public float Scale;
    }
    //[System.Serializable]
    //public class MotionOptionsJson
    //{
    //    public float BodyScaleFromHuman;
    //    public string FootIkMode;
    //}

    //[System.Serializable]
    //public class InformationDefain
    //{
    //    public string Caption;
    //    public string Author;
    //    public string Url;
    //    public string Description;
    //}

}
