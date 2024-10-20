using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;


namespace AnimLite.Vmd
{
    using AnimLite.Utility;



    public static partial class VmdParser
    {

        /// <summary>
        /// í«â¡Ç∑ÇÈ
        /// ìØÇ∂ÉLÅ[Ç™Ç†ÇÍÇŒ appenddata Ç≈è„èëÇ´Ç∑ÇÈ
        /// </summary>
        public static VmdMotionData AppendOrOverwrite(this VmdMotionData basedata, VmdMotionData appenddata)
        {
            appenddata.bodyKeyStreams
                .ForEach(pair => basedata.bodyKeyStreams[pair.Key] = pair.Value);
            appenddata.faceKeyStreams
                .ForEach(pair => basedata.faceKeyStreams[pair.Key] = pair.Value);
            
            return basedata;
        }


        public static VmdMotionData ParseVmd(this PathUnit filepath)
        {
            using var f = new FileStream(filepath, FileMode.Open, FileAccess.Read);

            return ParseVmd(f);
        }
        public static VmdMotionData ParseVmd(byte[] byteData)
        {
            using var m = new MemoryStream(byteData);

            return ParseVmd(m);
        }


        public static VmdMotionData ParseVmd(this Stream s)
        {
            //System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using var r = new BinaryReader(s);

            var (h, n) = header_(r);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            $"format name : {h}".ShowDebugLog();
            $"model name : {n}".ShowDebugLog();
#endif


            var bodydata = body_(r);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string.Join(", ", bodydata.Select(x => $"{x.Key.name}:{x.Value.Length}")).ShowDebugLog();
#endif

            var facedata = face_(r);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string.Join(", ", facedata.Select(x => $"{x.Key.name}:{x.Value.Length}")).ShowDebugLog();
#endif

//            var cameradata = camera_(r);
//#if UNITY_EDITOR || DEVELOPMENT_BUILD
//            $"camera:{cameradata.Length}".ShowDebugLog();
//#endif

            return new VmdMotionData
            {
                bodyKeyStreams = bodydata,
                faceKeyStreams = facedata,
            };
        }





        public static VmdCameraData ParseVmdCamera(this PathUnit filepath)
        {
            using var f = new FileStream(filepath, FileMode.Open, FileAccess.Read);

            return ParseVmdCamera(f);
        }
        public static VmdCameraData ParseVmdCamera(byte[] byteData)
        {
            using var m = new MemoryStream(byteData);

            return ParseVmdCamera(m);
        }


        public static VmdCameraData ParseVmdCamera(this Stream s)
        {
            //System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using var r = new BinaryReader(s);

            var (h, n) = header_(r);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            $"format name : {h}".ShowDebugLog();
            $"model name : {n}".ShowDebugLog();
#endif

            skip_();

            var cameradata = camera_(r);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            $"camera:{cameradata.Length}".ShowDebugLog();
#endif

            return new VmdCameraData
            {
                cameraKeyStream = cameradata,
            };


            void skip_()
            {
                var bodyKeyLength = r.ReadUInt32();
                r.ReadBytes(111 * (int)bodyKeyLength);

                var faceKeyLength = r.ReadUInt32();
                r.ReadBytes(23 * (int)faceKeyLength);
            }
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
                let _ = r.ReadBytes(64)
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
                //.Do(x => { if (x.Key.name.EndsWith("ÇhÇj") || x.Key.name.EndsWith("IK")) Debug.Log($"{x.Key.name} {x.Count()}"); })
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
                //.Do(x => { if (x.Key.name.EndsWith("ÇhÇj") || x.Key.name.EndsWith("IK")) Debug.Log($"{x.Key.name} {x.Count()}"); })
                .ToDictionary(x => x.Key, x =>
                    x.OrderBy(x => x.time)
                    .ToArray())
                ;
        }



        static VmdCameraMotionKey[] camera_(BinaryReader r)
        {
            const float frametime_rate = (float)(1.0 / 30.0);

            var keyLength = r.ReadUInt32();

            var q =
                from i in Enumerable.Range(0, (int)keyLength)
                let frameno = r.ReadUInt32()
                let distance = r.ReadSingle()
                let vx = r.ReadSingle()
                let vy = r.ReadSingle()
                let vz = r.ReadSingle()
                let rx = r.ReadSingle()
                let ry = r.ReadSingle()
                let rz = r.ReadSingle()
                let _ = r.ReadBytes(24)
                let fov = r.ReadUInt32()
                let isPerthed = r.ReadByte()
                
                let rot = Quaternion.Euler(new Vector3(-rx, ry, rz))
                let look_at_pos = new Vector3(-vx, vy, -vz)
                let move = Vector3.forward * -distance
                let camera_pos = look_at_pos + rot * move
                
                select new VmdCameraMotionKey
                {
                    frameno = frameno,
                    time = frameno * frametime_rate,
                    pos = new float4(camera_pos, 1.0f),
                    //pos = new float4(look_at_pos, 1.0f),
                    rot = rot,
                    distance = distance,
                    fov = (float)fov
                };

            return q
                //.Do(x => Debug.Log($"{x.Key.name} {x.Count()}"))
                //.Do(x => { if (x.Key.name.EndsWith("ÇhÇj") || x.Key.name.EndsWith("IK")) Debug.Log($"{x.Key.name} {x.Count()}"); })
                .ToArray()
                ;
        }
    }

}
