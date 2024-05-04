using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;


namespace AnimLite.Vmd
{
    using AnimLite.Utility;



    public static class VmdParser
    {

        public static Task<VmdMotionData> ParseVmdAsync(PathUnit filepath, CancellationToken ct) =>
            Task.Run(async () =>
            {
                using var f = new FileStream(filepath, FileMode.Open, FileAccess.Read);

                ct.ThrowIfCancellationRequested();

                using var m = new MemoryStream();
                await f.CopyToAsync(m, ct);

                return ParseVmd(m);
            }, ct);
        
        public static Task<VmdMotionData> ParseVmdAsync(TextAsset vmdFileAsset, CancellationToken ct)
        {
            using var m = new MemoryStream(vmdFileAsset.bytes);

            return Task.Run(() => ParseVmd(m), ct);
        }


        public static VmdMotionData ParseVmd(PathUnit filepath)
        {
            using var f = new FileStream(filepath, FileMode.Open, FileAccess.Read);

            using var m = new MemoryStream();
            f.CopyTo(m);

            return ParseVmd(m);
        }
        public static VmdMotionData ParseVmd(TextAsset vmdFile)
        {
            using var m = new MemoryStream(vmdFile.bytes);

            return ParseVmd(m);
        }


        public static VmdMotionData ParseVmd(MemoryStream m)
        {
            //System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            m.Seek(0, SeekOrigin.Begin);

            using var r = new BinaryReader(m);

            //m.Seek(50, SeekOrigin.Begin);

            var (h, n) = header_(r);
            #if UNITY_EDITOR
                $"format name : {h}".ShowDebugLog();
                $"model name : {n}".ShowDebugLog();
            #endif

            var bodydata = body_(r);
            #if UNITY_EDITOR
                string.Join(", ", bodydata.Select(x => $"{x.Key.name}:{x.Value.Count()}")).ShowDebugLog();
            #endif


            var facedata = face_(r);

            return new VmdMotionData
            {
                bodyKeyStreams = bodydata,
                faceKeyStreams = facedata,
            };
        }


        static (string formatName, string modelName) header_(BinaryReader r)
        {
            var sjis = Encoding.GetEncoding("shift_jis");

            var formatName = sjis.GetString(r.ReadBytes(30)).TrimEnd('\0');
            var modelName = sjis.GetString(r.ReadBytes(20)).TrimEnd('\0');

            return (formatName, modelName);
        }


        static Dictionary<VmdBoneName, VmdBodyMotionKey[]> body_(BinaryReader r)
        {
            var sjis = Encoding.GetEncoding("shift_jis");

            const float frametime_rate = (float)(1.0 / 30.0);

            var keyLength = r.ReadUInt32();

            var q =
                from i in Enumerable.Range(0, (int)keyLength)
                    //let bonename = sjis.GetString(r.ReadBytes(15)).TrimEnd('\0').AsVmdBoneName()
                let bonename = sjis.GetString(r.ReadBytes(15)).Split('\0')[0].AsVmdBoneName()
                let frameno = r.ReadUInt32()
                let vx = r.ReadSingle()
                let vy = r.ReadSingle()
                let vz = r.ReadSingle()
                let qx = r.ReadSingle()
                let qy = r.ReadSingle()
                let qz = r.ReadSingle()
                let qw = r.ReadSingle()
                let _ = r.BaseStream.Seek(64, SeekOrigin.Current)
                let key = new VmdBodyMotionKey
                {
                    frameno = frameno,
                    time = frameno * frametime_rate,
                    pos = new float4(-vx, vy, -vz, 1.0f),
                    rot = new quaternion(-qx, qy, -qz, qw),
                }
                select (bonename, key)
                ;

            return q
                .ToLookup(x => x.bonename, x => x.key)
                //.Do(x => Debug.Log($"{x.Key.name} {x.Count()}"))
                //.Do(x => { if (x.Key.name.EndsWith("‚h‚j") || x.Key.name.EndsWith("IK")) Debug.Log($"{x.Key.name} {x.Count()}"); })
                .ToDictionary(x => x.Key, x =>
                    x.OrderBy(x => x.time)
                    .ToArray())
                ;
        }


        static Dictionary<VmdFaceName, VmdFaceKey[]> face_(BinaryReader r)
        {
            var sjis = Encoding.GetEncoding("shift_jis");

            const float frametime_rate = (float)(1.0 / 30.0);

            var skinLength = r.ReadUInt32();

            var q =
                from i in Enumerable.Range(0, (int)skinLength)
                    //let facename = sjis.GetString(r.ReadBytes(15)).TrimEnd('\0')
                let facename = sjis.GetString(r.ReadBytes(15)).Split('\0')[0].AsVmdFaceName()
                let frameno = r.ReadUInt32()
                let weight = r.ReadSingle()
                let key = new VmdFaceKey
                {
                    frameNo = frameno,
                    time = frameno * frametime_rate,
                    weight = weight// * 100
                }
                select (facename, key)
                ;

            return q
                .ToLookup(x => x.facename, x => x.key)
                //.Do(x => Debug.Log($"{x.Key.name} {x.Count()}"))
                //.Do(x => { if (x.Key.name.EndsWith("‚h‚j") || x.Key.name.EndsWith("IK")) Debug.Log($"{x.Key.name} {x.Count()}"); })
                .ToDictionary(x => x.Key, x =>
                    x.OrderBy(x => x.time)
                    .ToArray())
                ;
        }


    }

}
