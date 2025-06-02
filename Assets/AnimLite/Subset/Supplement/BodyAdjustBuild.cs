using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using Unity.VisualScripting;
using Sprache;
using FastEnumUtility;

namespace AnimLite
{
    using AnimLite.experimental.a;
    using AnimLite.Utility;
    using AnimLite.Loader;


    public struct BodyAdjust
    {
        public float4 position;
        public quaternion rotation;
    }

    public class BodyAdjustData : Dictionary<HumanBodyBones, BodyAdjust>
    {
        //public bool IsCreated => this is not null;

    }

    public static class BodyAdjustExtension
    {


        //static public ValueTask<BodyAdjustData> ParseBodyAdjustAsync(this PathUnit filepath, CancellationToken ct)
        //{
        //    using var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);

        //    return fs.ParseBodyAdjustAsync(ct);
        //}

        static public ValueTask<BodyAdjustData> ParseBodyAdjustAsync(this Stream stream, CancellationToken ct) =>
            new ValueTask<BodyAdjustData>(ParseBodyAdjust(stream));



        //static public BodyAdjustData ParseBodyAdjust(this PathUnit filepath)
        //{
        //    using var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);

        //    return fs.ParseBodyAdjust();
        //}

        static public BodyAdjustData ParseBodyAdjust(this Stream stream)
        {
            using var r = new StreamReader(stream);

            var txt = r.ReadToEnd();

            var bones = parse_(txt);

            return build_(bones);


            BodyAdjustData build_((string name, BodyAdjust adjust)[] src)
            {
                return src
                    .Select(x => (id: toBoneId_(x.name), x.adjust))
                    .Distinct(x => x.id)
                    .Aggregate(new BodyAdjustData(), (dict, x) => dict.AddInChain(x.id, x.adjust));

                HumanBodyBones toBoneId_(string name) =>
                    FastEnum.TryParse<HumanBodyBones>(name, ignoreCase: true, out var id)
                        ? id
                        : HumanBodyBones.LastBone;
            }


            (string, BodyAdjust)[] parse_(string txt)
            {
                var qNumber =
                    from sign in Parse.Char('+').Or(Parse.Char('-')).OneOrEmpty().Text()
                    from digit in Parse.Number.GetOrBlank()
                    from piriod in Parse.Char('.').OneOrEmpty().Text()
                    from frac in Parse.Number.GetOrBlank()
                    select float.Parse($"{sign}{digit}{piriod}{frac}")
                    ;

                var qRotUnit =
                    from type in
                        Parse.Char('x').Or(Parse.Char('y')).Or(Parse.Char('z'))
                        .Or(Parse.Char('X')).Or(Parse.Char('Y')).Or(Parse.Char('Z'))
                    from num in qNumber
                    let rad = math.radians(num)
                    select type switch
                    {
                        'x' or 'X' => quaternion.RotateX(rad),
                        'y' or 'Y' => quaternion.RotateY(rad),
                        'z' or 'Z' => quaternion.RotateZ(rad),
                        _ => quaternion.identity,
                    };
                var qLocalRotation =
                    from _label in Parse.String("local rotation")
                        .Or(Parse.String("LocalRotation"))
                        .Or(Parse.String("lrot")).Token()
                    from rots in qRotUnit.Token().DelimitedBy(Parse.Char('*').Token())
                    select rots.Aggregate((pre, cur) => math.mul(pre, cur));
                    ;

                var qBone =
                    from name in Parse.Letter.Many().Text().Token()
                    from lrot in qLocalRotation
                    select (name, data: new BodyAdjust
                    {
                        rotation = lrot,
                    });

                var q =
                    from bone in qBone.Many().Token()
                    select bone
                    ;

                return q.Parse(txt).ToArray();
            }
        }
    }

    static class TextParseUtility
    {
        public static Parser<IEnumerable<T>> OneOrEmpty<T>(this Parser<T> src) =>
            src.Many().Optional().Select(opt => opt.GetOrElse(Enumerable.Empty<T>()));

        public static Parser<string> GetOrBlank(this Parser<string> src) =>
            src.Optional().Select(x => x.GetOrElse(""));

        public static HumanBodyBones ParseOrDefault(this string name, HumanBodyBones defautValue)
        {
            if (FastEnum.TryParse<HumanBodyBones>(name, ignoreCase: true, out var id)) return defautValue;

            return id;
        }
    }
}
