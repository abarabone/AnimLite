using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Sprache;
using Unity.Mathematics;

namespace AnimLite.Bvh
{
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;
    using AnimLite.Vmd;


    public static class BvhParser
    {


        public static Dictionary<VmdBoneName, HumanBodyBones> LogicalToPhysicalBones = new()
        {
            {"hips",            HumanBodyBones.Hips},
            {"chest",           HumanBodyBones.Spine},
            {"chest2",          HumanBodyBones.Chest},
            //{"chest3",          HumanBodyBones.Chest},
            {"chest4",          HumanBodyBones.UpperChest},
            {"neck",            HumanBodyBones.Neck},
            {"head",            HumanBodyBones.Head},
            {"rightcollar",     HumanBodyBones.RightShoulder},
            {"rightshoulder",   HumanBodyBones.RightUpperArm},
            {"rightelbow",      HumanBodyBones.RightLowerArm},
            {"rightwrist",      HumanBodyBones.RightHand},
            {"leftcollar",      HumanBodyBones.LeftShoulder},
            {"leftshoulder",    HumanBodyBones.LeftUpperArm},
            {"leftelbow",       HumanBodyBones.LeftLowerArm},
            {"leftwrist",       HumanBodyBones.LeftHand},
            {"righthip",        HumanBodyBones.RightUpperLeg},
            {"rightknee",       HumanBodyBones.RightLowerLeg},
            {"rightankle",      HumanBodyBones.RightFoot},
            {"righttoe",        HumanBodyBones.RightToes},
            {"lefthip",         HumanBodyBones.LeftUpperLeg},
            {"leftknee",        HumanBodyBones.LeftLowerLeg},
            {"leftankle",       HumanBodyBones.LeftFoot},
            {"lefttoe",         HumanBodyBones.LeftToes},
        };


        // bvh
        // �E���W�n�A���W�̈Ӗ��A��]�������ׂĂ��C�ӂ炵��
        // �E���W�n�͊e�v�f�̔��]�ŏC���ł���
        // �@- unity �͍���n�A�E��n�� bvh �Ȃ�ǂꂩ�̎��𔽓]����B
        // �@�@hierachy �̈ʒu�֌W�����āA
        // �@�@> �������������Ă���Ȃ� y ���v���X
        // �@�@> �E���Ȃǂ̕������v���X�Ȃ� x �v���X���E
        // �@�@> �ܐ�̕��������Ȃ� z ���v���X
        // �@�@�Ȃǔ��f����
        // �E���W�̈Ӗ��� Hip �𓮂����Ă݂āA�Ȃ�ƂȂ��� xyz �� �����������s ���Ƃ킩����
        // �E��]������ unity �͍��˂��ŁA���� inverse ���K�v�������̂ŁA�E�˂��炵��
        // �@- ��]�������t�ɂ���ɂ� inverse ����΂悳����
        // �@- bvh �� YRotation XRotation ZRotation �Ȃǂ� hierarchy �\�L���ɉ�]�����āA�Ō�� inverse ����΂悢�݂���

        // �����Ƃ������Ղ�������Ȃ�
        // �E���W�n�̕␳�̂��߁Axyz ���Ƃɐ���
        // �E��]�����̕␳�̂��߁A�E�˂��Ȃ� inverse
        // �Exyz �̈Ӗ��Ƃ��āA�V���b�t��������΂Ȃ��悢

        // ��]�� humanoid tf local �ɂ��ꂽ�畁�ʂɉ�]�ł���




        /// <summary>
        /// ��] stream data ���r���h����B
        /// �C�Ӄt�H�[�}�b�g�̃{�[��������AHumanBodyBones �̂h�c�Ƀ}�b�v�����B
        /// �Ή��{�[�����Ȃ����ʂ́Aempty keys ���i�[�����B
        /// </summary>
        public static StreamData<quaternion> CreateRotationData2(
            this Dictionary<VmdBoneName, VmdBodyMotionKey[]> nameToStream, Dictionary<VmdBoneName, HumanBodyBones> logicalToPhysicalBones)
        {

            // dict<name, keys> -> (id, keys)[]
            var qNameToId =
                from x in nameToStream
                let boneid = logicalToPhysicalBones.TryGetOrDefault(x.Key.name.ToLower(), HumanBodyBones.LastBone)
                where boneid != HumanBodyBones.LastBone
                select (boneid, keys: x.Value.AsEnumerable())
                ;
            var src = qNameToId
                .ToArray();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(string.Join(", ", src.Select(x => $"{x.boneid}:{x.keys.Count()}")));
#endif

            // MmdBodyBones �̏��Ԓʂ�ɕ��ѕς���B
            // �Ή��{�[�����Ȃ��ꍇ�́Aempty �𐶐�����B
            var qOrderNormalize =
                from targetid in Enumerable.Range(0, (int)HumanBodyBones.LastBone)
                join thisid in src on (HumanBodyBones)targetid equals thisid.boneid into ys
                let boneid = (HumanBodyBones)targetid
                let blankkeys = new EmptyEnumerableStruct<VmdBodyMotionKey>()
                from ny in ys.DefaultIfEmpty((boneid, blankkeys))
                select ny
                ;
            var rotsrc = qOrderNormalize
                //.Do(x => Debug.Log(x))
                .Select(x => x.keys)
                .ToArray();

            // stream data �Ƃ��ăr���h����
            return new StreamData<quaternion>
            {
                KeyStreams = rotsrc.BuildKeyData(key => key.rot, key => key.time, defaultKey: VmdBodyMotionKey.Identity),
                Sections = rotsrc.BuildSectionData(defaultKey: VmdBodyMotionKey.Identity),
            };
        }

        /// <summary>
        /// �ʒu stream data ���r���h����B
        /// �Ώۂ̓��[�g�{�[���P����
        /// </summary>
        public static StreamData<float4> CreatePositionData2(
            this Dictionary<VmdBoneName, VmdBodyMotionKey[]> nameToStream)
        {
            var qPosSrc = nameToStream
                .Take(1)
                .Select(x => x.Value);
            
            return new StreamData<float4>
            {
                KeyStreams = qPosSrc.BuildKeyData(key => key.pos, key => key.time, defaultKey: VmdBodyMotionKey.Identity),
                Sections = qPosSrc.BuildSectionData(defaultKey: VmdBodyMotionKey.Identity),
            };
        }




        public struct FileParseOption
        {
            public float sizerate;
            public float timerate;
            public math.ShuffleComponent shfx;
            public math.ShuffleComponent shfy;
            public math.ShuffleComponent shfz;

            public float3 axisDirection;

            public Func<quaternion, quaternion> rotopt;


            public static FileParseOption Instance = new()
            {
                sizerate = 0.01f,
                timerate = 1.0f,
                shfx = math.ShuffleComponent.LeftX,
                shfy = math.ShuffleComponent.LeftY,
                shfz = math.ShuffleComponent.RightZ,
                axisDirection = new float3(-1, 1, 1),
                rotopt = rot => math.inverse(rot),
            };
        }



        public static VmdMotionData BvhToVmdMotionData(this BvhMotionData bvh)//, FileParseOption opt = default)
        {
            var opt = FileParseOption.Instance;

            var qRootBone = (bone: bvh.Bones[0], key:
                from frame in bvh.Frames.Select((x, i) => (i, value: x))
                
                let pos = frame.value.poss[0] * opt.sizerate * opt.axisDirection
                let rot = math.radians(frame.value.rots[0]) * opt.axisDirection

                select new VmdBodyMotionKey
                {
                    frameno = (uint)frame.i,
                    time = frame.i * bvh.FrameTime * opt.timerate,
                    pos = new float4(pos.x, pos.y, pos.z, 1.0f),
                    rot = opt.rotopt(quaternion.EulerYXZ(rot)),
                }
            );

            var qJointbone =
                from bone in bvh.Bones.Select((x, i) => (i, value: x)).Skip(1)
                select (bone: bone.value, key:
                    from frame in bvh.Frames.Select((x, i) => (i, value: x))

                    let rot = math.radians(frame.value.rots[bone.i]) * opt.axisDirection

                    select new VmdBodyMotionKey
                    {
                        frameno = (uint)frame.i,
                        time = frame.i * bvh.FrameTime * opt.timerate,
                        pos = new float4(0.0f, 0.0f, 0.0f, 1.0f),
                        rot = opt.rotopt(quaternion.EulerYXZ(rot)),
                    }
                );

            return new VmdMotionData
            {
                bodyKeyStreams = qJointbone.Prepend(qRootBone)
                    .ToDictionary(x => x.bone.name.AsVmdBoneName(), x => x.key.ToArray()),

                faceKeyStreams =
                    new Dictionary<VmdFaceName, VmdFaceKey[]>(),
            };
        }



        public static BvhMotionData ParseBvh(this PathUnit path)
        {

            using var r = new StreamReader(path.ToFullPath())!;
            var txt = r.ReadToEnd();

            var bvh = buildParser_(path).Parse(txt);
            return bvh;


            static Parser<BvhMotionData> buildParser_(PathUnit path)
            {
                var qNumber =
                    from sign in Parse.Char('+').Or(Parse.Char('-')).OneOrEmpty().Text()
                    from digit in Parse.Number.GetOrBlank()
                    from piriod in Parse.Char('.').OneOrEmpty().Text()
                    from frac in Parse.Number
                    select float.Parse($"{sign}{digit}{piriod}{frac}")
                    ;
                var qVector3 =
                    from x in qNumber.Token()
                    from y in qNumber.Token()
                    from z in qNumber.Token()
                    select new float3(x, y, z)
                    ;
                var qEularYXZ =
                    from y in qNumber.Token()
                    from x in qNumber.Token()
                    from z in qNumber.Token()
                    select new float3(x, y, z)
                    ;

                var qOffset =
                    from _ in Parse.String("OFFSET").Token()
                    from offsets in qNumber.Token().Many()
                    select offsets
                    ;
                var qChannel =
                    from _ in Parse.String("CHANNELS").Token()
                    from _length in Parse.Number
                    let length = int.Parse(_length)
                    from channels in Parse.Letter.Many().Token().Text().Repeat(length)
                    select (length, channels)
                    ;
                Parser<IEnumerable<(string name, string[] channels)>> node() =>
                    from nodetype in Parse.String("ROOT").Or(Parse.String("JOINT")).Or(Parse.String("End")).Token().Text()
                    from nodename in Parse.LetterOrDigit.Many().Token().Text()
                    from open in Parse.Char('{').Token()
                    from ofs in qOffset
                    from chn in qChannel.OneOrEmpty()
                    from childnodes in nodetype.ToUpper() switch
                    {
                        "END" => Parse.Return(Enumerable.Empty<(string, string[])>()),
                        _ => from nodes in node().Many()
                             select nodes.SelectMany(x => x),
                    }
                    from close in Parse.Char('}').Token()
                    select nodetype.ToUpper() switch
                    {
                        "END" => childnodes,
                        _ => childnodes.Prepend((nodename, chn.First().channels.ToArray())),
                    };


                var qMotionHeader =
                    from fr in Parse.String("Frames").Token()
                    from fr_ in Parse.Char(':').Token()
                    from frame_length in Parse.Number
                    let frameLength = int.Parse(frame_length)
                    from ft1 in Parse.String("Frame").Token()
                    from ft2 in Parse.String("Time").Token()
                    from ft_ in Parse.Char(':').Token()
                    from frameTime in qNumber
                    select (frameLength, frameTime)
                    ;

                Parser<IEnumerable<float3>> poss(int boneLength) =>
                    from poss in qVector3.Repeat(boneLength)
                    select poss
                    ;
                Parser<IEnumerable<float3>> rots(int boneLength) =>
                    from rots in qEularYXZ.Repeat(boneLength)
                    select rots
                    ;
                Parser<Frame> frame(int boneLength) =>
                    from poss in poss(1)
                    from rots in rots(boneLength)
                    select new Frame
                    {
                        poss = poss.ToArray(),
                        rots = rots.ToArray(),
                    };


                var qBvh =
                    from hi in Parse.String("HIERARCHY").Token()
                    from nodes in node().Many()
                    let bones = nodes.SelectMany(x => x).ToArray()
                    from mo in Parse.String("MOTION").Token()
                    from motionHeader in qMotionHeader
                    from frames in frame(bones.Length).Many()
                    select new BvhMotionData
                    {
                        Bones = bones,
                        FrameTime = motionHeader.frameTime,
                        Frames = frames.ToArray(),
                    };

                return qBvh;
            }
        }
    }

    public class BvhMotionData
    {
        public (string name, string[] channels)[] Bones;
        public float FrameTime;
        public Frame[] Frames;
    }

    public struct Frame
    {
        public float3[] poss;
        public float3[] rots;
    }

    static class TextParseUtility
    {
        public static Parser<IEnumerable<T>> OneOrEmpty<T>(this Parser<T> src) =>
            src.Many().Optional().Select(opt => opt.GetOrElse(Enumerable.Empty<T>()));

        public static Parser<string> GetOrBlank(this Parser<string> src) =>
            src.Optional().Select(x => x.GetOrElse(""));
    }

}
