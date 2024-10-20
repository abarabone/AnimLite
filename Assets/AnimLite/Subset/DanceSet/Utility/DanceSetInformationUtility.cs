using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace AnimLite.DancePlayable
{
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;

    using AnimLite.Vmd;
    using AnimLite.Vrm;
    using static AnimLite.DancePlayable.DanceGraphy2;
    //using static UnityEditor.Progress;

#nullable enable

    public static class DanceSetInformationUtility
    {


        public static async ValueTask OrverrideInformationIfBlankAsync(
            this DanceSetJson ds, DanceGraphy2.Order order)
        {
            await Awaitable.MainThreadAsync();

            if (ds.AudioInformation.IsBlank())
            {
                ds.AudioInformation =
                    ds.Audio.ToInformation(order.Audio.AudioClip.clip);
            }

            var q = (ds.Motions.Values, order.Motions).Zip();
            foreach (var (d, o) in q)
            {
                if (d.ModelInformation.IsBlank())
                {
                    d.ModelInformation =
                        d.Model.ToInformation(o.Model?.Value?.GetComponent<UniVRM10.Vrm10Instance>());
                }

                if (d.AnimationInformation.IsBlank())
                {
                    d.AnimationInformation =
                        d.Animation.ToInformation(o);
                }
            }
        }



        public static bool IsBlank(this InformationDefine i) =>
            (i.Author ?? "") == ""
            &&
            (i.Caption ?? "") == ""
            &&
            (i.Description ?? "") == ""
            &&
            (i.Url ?? "") == ""
            ;

        public static InformationDefine ToInformation(this ModelDefineJson md, UniVRM10.Vrm10Instance? vrm)
        {
            if (vrm.IsUnityNull()) return new InformationDefine
            {
                Caption = "不明",
                Author = "不明",
            };

            var info = vrm!.Vrm.Meta;

            return new InformationDefine
            {
                Caption = info.Name != "" ? info.Name : Path.GetFileNameWithoutExtension(md.ModelFilePath),
                Author = string.Join("/", info.Authors.FirstOrDefault() ?? "作者不明"),
                Description = info.CopyrightInformation ?? "",
                Url = info.ContactInformation ?? "",
            };
        }

        public static InformationDefine ToInformation(this AnimationDefineJson ad, MotionOrderBase order)
        {
            if (order.IsMotionBlank) return new InformationDefine
            {
                Caption = "不明",
                Author = "作者不明",
            };

            return new InformationDefine
            {
                Caption = Path.GetFileNameWithoutExtension(ad.AnimationFilePath.Paths.First()),
                Author = "作者不明",
                Description = "",
                Url = "",
            };
        }

        public static InformationDefine ToInformation(this AudioDefineJson ad, AudioClip clip)
        {
            if (clip.IsUnityNull()) return new InformationDefine
            {
                //Caption = "不明",
                //Author = "作者不明",
            };

            return new InformationDefine
            {
                Caption = clip.name != "" ? clip.name : Path.GetFileNameWithoutExtension(ad.AudioFilePath),
                Author = "作者不明",
                Description = "",
                Url = "",
            };
        }

    }
}