using System;
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



    /// <summary>
    /// 
    /// </summary>
    public struct VmdStreamData : IDisposable
    {
        public StreamDataHolder<quaternion, Key4StreamCache<quaternion>, StreamIndex> RotationStreams;
        public StreamDataHolder<float4, Key4StreamCache<float4>, StreamIndex> PositionStreams;
        public StreamDataHolder<float, Key2StreamCache<float>, StreamIndex> FaceStreams;

        public bool IsCreated => this.RotationStreams.Streams.KeyStreams.Values.IsCreated;

        public void Dispose()
        {
            this.RotationStreams.Dispose();
            this.PositionStreams.Dispose();
            this.FaceStreams.Dispose();
        }
    }





    public struct VmdBoneName
    {
        public string name;

        static public implicit operator VmdBoneName(string name) => name.AsVmdBoneName();
    }

    public struct BoneIndex
    {
        public int index;

        static public implicit operator BoneIndex(int i) => i.AsBoneIndex();
    }

    public struct VmdFaceName
    {
        public string name;

        static public implicit operator VmdFaceName(string name) => name.AsVmdFaceName();
    }
    public struct VrmFaceName
    {
        public string name;

        static public implicit operator VrmFaceName(string name) => name.AsVrmFaceName();
    }


    public struct VmdMotionData
    {
        public Dictionary<VmdBoneName, VmdBodyMotionKey[]> bodyKeyStreams;
        public Dictionary<VmdFaceName, VmdFaceKey[]> faceKeyStreams;
    }


    public struct VmdBodyMotionKey
    {
        public uint frameno;
        public float time;
        public float4 pos;
        public quaternion rot;
        //public float4[] interpolation;
    }

    //public struct BoneMappingEntry
    //{
    //    public HumanBoneName humanBoneName;

    //    public VmdBoneName vmdBoneName;
    //}

    public struct VmdFaceKey
    {
        public uint frameNo;
        public float time;
        public float weight;
    }

    public static class VmdUtilityExtension
    {

        public static VmdBoneName AsVmdBoneName(this string name) => new VmdBoneName { name = name };

        public static BoneIndex AsBoneIndex(this int index) => new BoneIndex { index = index };

        public static VmdFaceName AsVmdFaceName(this string name) => new VmdFaceName { name = name };

        public static VrmFaceName AsVrmFaceName(this string name) => new VrmFaceName { name = name };
    }





    public static class VmdParser
    {


        public static async Awaitable<VmdMotionData> ParseVmdAsync(string filepath, CancellationToken ct)
        //public static VmdMotionData LoadVmd(string filepath)
        {
            using var f = new FileStream(filepath, FileMode.Open, FileAccess.Read);

            if (f == null)
            {
                return default;
            }

            using var m = new MemoryStream();
            await f.CopyToAsync(m, ct);
            //f.CopyTo(m);

            return await Task.Run(() => LoadVmd(m), ct);
        }
        public static async Awaitable<VmdMotionData> LoadVmdAsync(this TextAsset vmdFile, CancellationToken ct)
        {
            using var m = new MemoryStream(vmdFile.bytes);// © ‚±‚ê‚Í .Run() ‚É“ü‚ê‚ç‚ê‚é‚¾‚ë‚¤‚©H

            return await Task.Run(() => LoadVmd(m), ct);
        }

        public static VmdMotionData LoadVmd(MemoryStream m)
        {
            //System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            m.Seek(0, SeekOrigin.Begin);

            using var r = new BinaryReader(m);

            //m.Seek(50, SeekOrigin.Begin);

            var (h, n) = header_(r);
            $"format name : {h}".ShowDebugLog();
            $"model name : {n}".ShowDebugLog();

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


        static public async Awaitable<Dictionary<VmdFaceName, VrmFaceName>> ParseFaceMapAsync(string filepath, CancellationToken ct)
        {

            using var s = new StreamReader(filepath);

            var txt = await s.ReadToEndAsync();

            return await Task.Run(() => parse_(), ct);


            Dictionary<VmdFaceName, VrmFaceName> parse_()
            {
                var q =
                    from line in txt.Split("\n", StringSplitOptions.RemoveEmptyEntries)
                    select line.Trim().Split("\t", StringSplitOptions.RemoveEmptyEntries)
                    ;

#if UNITY_EDITOR
                string.Join(", ", q.Select((x, i) => $"{i}:{x[0]}:{x[1]}")).ShowDebugLog();
#endif

                return q.ToDictionary(x => x[0].Trim().AsVmdFaceName(), x => x[1].Trim().AsVrmFaceName());
            }
        }

    }

}
