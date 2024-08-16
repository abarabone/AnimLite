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
    using static AnimLite.DancePlayable.DanceGraphy;
    //using static UnityEditor.Progress;


    public static class DanceSetInformationUtility
    {


        public static async ValueTask OrverrideInformationIfBlankAsync(
            this DanceSetDefineData ds, DanceGraphy2.Order order)
        {
            await Awaitable.MainThreadAsync();

            if (ds.AudioInformation.IsBlank())
            {
                ds.AudioInformation =
                    ds.Audio.ToInformation(order.Audio.AudioClip.clip);
            }

            for (var i = 0; i < ds.Motions.Length; i++)
            {
                var o = order.Motions[i];
                var d = ds.Motions[i];

                if (d.ModelInformation.IsBlank())
                {
                    d.ModelInformation =
                        d.Model.ToInformation(o.Model?.GetComponent<UniVRM10.Vrm10Instance>());
                }

                if (d.AnimationInformation.IsBlank())
                {
                    d.AnimationInformation =
                        d.Animation.ToInformation(o.vmddata);
                }
            }
        }



        public static bool IsBlank(this InformationDefain i) =>
            (i.Author ?? "") == ""
            &&
            (i.Caption ?? "") == ""
            &&
            (i.Description ?? "") == ""
            &&
            (i.Url ?? "") == ""
            ;

        public static InformationDefain ToInformation(this ModelDefineData md, UniVRM10.Vrm10Instance vrm)
        {
            if (vrm.IsUnityNull()) return default;

            var info = vrm.Vrm.Meta;

            return new InformationDefain
            {
                Caption = info.Name != "" ? info.Name : Path.GetFileNameWithoutExtension(md.ModelFilePath),
                Author = string.Join("/", info.Authors.FirstOrDefault() ?? "çÏé“ïsñæ"),
                Description = info.CopyrightInformation ?? "",
                Url = info.ContactInformation ?? "",
            };
        }

        public static InformationDefain ToInformation(this AnimationDefineData ad, VmdStreamData vmd)
        {
            if (vmd == null) return default;

            return new InformationDefain
            {
                Caption = Path.GetFileNameWithoutExtension(ad.AnimationFilePath),
                Author = "çÏé“ïsñæ",
                Description = "",
                Url = "",
            };
        }

        public static InformationDefain ToInformation(this AudioDefineData ad, AudioClip clip)
        {
            if (clip.IsUnityNull()) return default;

            return new InformationDefain
            {
                Caption = clip.name != "" ? clip.name : Path.GetFileNameWithoutExtension(ad.AudioFilePath),
                Author = "çÏé“ïsñæ",
                Description = "",
                Url = "",
            };
        }

    }
}