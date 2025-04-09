using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace AnimLite.IK
{


    public static class TwoBoneIk
    {
        public static void SolveIk(
            Transform tftop1, Transform tfmid1, Transform tflow1, float3 posEffector1, quaternion rotEffector1,
            Transform tftop2, Transform tfmid2, Transform tflow2, float3 posEffector2, quaternion rotEffector2)
        {
            new Tf.StreamSource().SolveTwoBonePairIk<Tf, Tf.StreamSource>(
                tftop1.AsTf(), tfmid1.AsTf(), tflow1.AsTf(), posEffector1, rotEffector1,
                tftop2.AsTf(), tfmid2.AsTf(), tflow2.AsTf(), posEffector2, rotEffector2);
        }
        public static void SolveIk(
            Transform tftop1, Transform tfmid1, Transform tflow1, float3 posEffector1,
            Transform tftop2, Transform tfmid2, Transform tflow2, float3 posEffector2)
        {
            new Tf.StreamSource().SolveTwoBonePairIk<Tf, Tf.StreamSource>(
                tftop1.AsTf(), tfmid1.AsTf(), tflow1.AsTf(), posEffector1,
                tftop2.AsTf(), tfmid2.AsTf(), tflow2.AsTf(), posEffector2);
        }

        public static void SolveIk(Transform tftop, Transform tfmid, Transform tflow, float3 posEffector)
        {
            new Tf.StreamSource().SolveTwoBoneIk<Tf, Tf.StreamSource>(
                tftop.AsTf(), tfmid.AsTf(), tflow.AsTf(), posEffector);
        }
        public static void SolveIk(Transform tftop, Transform tfmid, Transform tflow, float3 posEffector, quaternion rotEffector)
        {
            new Tf.StreamSource().SolveTwoBoneIk<Tf, Tf.StreamSource>(
                tftop.AsTf(), tfmid.AsTf(), tflow.AsTf(), posEffector, rotEffector);
        }
    }


    public static class IkExtension
    {

        public static Tf AsTf(this Transform tf) => new Tf()
        {
            tf = tf,
        };



        public static void SolveTwoBoneIk<TTf, TStream>(
            this TStream stream,
            TTf topHandle, TTf midHandle, TTf lowHandle,
            float3 posEffector, quaternion rotEffector)
                where TTf : ITransformProxy<TStream>
                where TStream : ITransformStreamSource
        {
            stream.SolveTwoBoneIk(topHandle, midHandle, lowHandle, posEffector);

            lowHandle.SetRotation(stream, rotEffector);
        }

        public static void SolveTwoBoneIk<TTf, TStream>(
            this TStream stream,
            TTf topHandle, TTf midHandle, TTf lowHandle,
            float3 posEffector)
            where TTf : ITransformProxy<TStream>
            where TStream : ITransformStreamSource
        {
            var aRotation = topHandle.GetRotation(stream);
            var bRotation = midHandle.GetRotation(stream);
            //var eRotation = rotEffector;

            var aPosition = topHandle.GetPosition(stream);
            var bPosition = midHandle.GetPosition(stream);
            var cPosition = lowHandle.GetPosition(stream);
            var ePosition = posEffector;

            var ab = bPosition - aPosition;
            var bc = cPosition - bPosition;
            var ac = cPosition - aPosition;
            var ae = ePosition - aPosition;

            var lab = math.length(ab);
            var lac = math.length(ac);
            var lae = math.length(ae);
            var lbc = math.length(bc);
            var abcAngle = TriangleAngle(lac, lab, lbc);
            var abeAngle = TriangleAngle(lae, lab, lbc);
            var angle = abcAngle - abeAngle;
            var axis = math.rotate(bRotation, Vector3.right);

            var fromToRotation = quaternion.AxisAngle(axis, angle);

            var wrot = math.mul(fromToRotation, bRotation);
            midHandle.SetRotation(stream, wrot);

            cPosition = lowHandle.GetPosition(stream);
            ac = cPosition - aPosition;
            //var fromTo = Quaternion.FromToRotation(ac, ae).As_quaternion();
            var fromTo = IkExtension.fromToRotation(ac, ae);
            topHandle.SetRotation(stream, math.mul(fromTo, aRotation));

            //lowstream.SetRotation(stream, eRotation);


            static float TriangleAngle(float aLen, float aLen1, float aLen2)
            {
                var c = math.clamp((aLen1 * aLen1 + aLen2 * aLen2 - aLen * aLen) / (aLen1 * aLen2) / 2.0f, -1.0f, 1.0f);
                //float c = Mathf.Clamp((aLen1 * aLen1 + aLen2 * aLen2 - aLen * aLen) / (aLen1 * aLen2) / 2.0f, -1.0f, 1.0f);
                return math.acos(c);
            }
        }
        public static quaternion fromToRotation(float3 from, float3 to)
        {
            //from = math.normalize(from);
            //to = math.normalize(to);
            var axis = math.normalize(math.cross(from, to));
            var r = math.dot(from, to) / (math.length(from) * math.length(to));
            var angle = math.acos(r);
            return quaternion.AxisAngle(axis, angle);
        }




        public static void SolveTwoBonePairIk<TTf, TStream>(
            this TStream stream,
            TTf topHandle1, TTf midHandle1, TTf lowHandle1, float3 posEffector1, quaternion rotEffector1,
            TTf topHandle2, TTf midHandle2, TTf lowHandle2, float3 posEffector2, quaternion rotEffector2)
                where TTf : ITransformProxy<TStream>
                where TStream : ITransformStreamSource
        {
            stream.SolveTwoBonePairIk(
                topHandle1, midHandle1, lowHandle1, posEffector1,
                topHandle2, midHandle2, lowHandle2, posEffector2);

            lowHandle1.SetRotation(stream, rotEffector1);
            lowHandle2.SetRotation(stream, rotEffector2);
        }

        public static void SolveTwoBonePairIk<TTf, TStream>(
            this TStream stream,
            TTf topHandle1, TTf midHandle1, TTf lowHandle1, float3 posEffector1,
            TTf topHandle2, TTf midHandle2, TTf lowHandle2, float3 posEffector2)
                where TTf : ITransformProxy<TStream>
                where TStream : ITransformStreamSource
        {
            var rotA1 = topHandle1.GetRotation(stream);
            var rotB1 = midHandle1.GetRotation(stream);

            var posA1 = topHandle1.GetPosition(stream);
            var posB1 = midHandle1.GetPosition(stream);
            var posC1 = lowHandle1.GetPosition(stream);
            var posE1 = posEffector1;

            var ab1 = posB1 - posA1;
            var bc1 = posC1 - posB1;
            var ac1 = posC1 - posA1;
            var ae1 = posE1 - posA1;


            var rotA2 = topHandle2.GetRotation(stream);
            var rotB2 = midHandle2.GetRotation(stream);

            var posA2 = topHandle2.GetPosition(stream);
            var posB2 = midHandle2.GetPosition(stream);
            var posC2 = lowHandle2.GetPosition(stream);
            var posE2 = posEffector2;

            var ab2 = posB2 - posA2;
            var bc2 = posC2 - posB2;
            var ac2 = posC2 - posA2;
            var ae2 = posE2 - posA2;


            var lab1 = math.length(ab1);
            var lac1 = math.length(ac1);
            var lae1 = math.length(ae1);
            var lbc1 = math.length(bc1);

            var lab2 = math.length(ab2);
            var lac2 = math.length(ac2);
            var lae2 = math.length(ae2);
            var lbc2 = math.length(bc2);

            var a = calculateAngle_((lac1, lae1, lab1, lbc1), (lac2, lae2, lab2, lbc2));


            var angle1 = a.abc1 - a.abe1;
            var axis1 = math.rotate(rotB1, Vector3.right);

            var rotFromTo1 = quaternion.AxisAngle(axis1, angle1);

            var wrot1 = math.mul(rotFromTo1, rotB1);
            midHandle1.SetRotation(stream, wrot1);

            posC1 = lowHandle1.GetPosition(stream);
            ac1 = posC1 - posA1;
            var fromTo1 = fromToRotation(ac1, ae1);
            topHandle1.SetRotation(stream, math.mul(fromTo1, rotA1));


            var angle2 = a.abc2 - a.abe2;
            var axis2 = math.rotate(rotB2, Vector3.right);

            var rotFromTo2 = quaternion.AxisAngle(axis2, angle2);

            var wrot2 = math.mul(rotFromTo2, rotB2);
            midHandle2.SetRotation(stream, wrot2);

            posC2 = lowHandle2.GetPosition(stream);
            ac2 = posC2 - posA2;
            var fromTo2 = fromToRotation(ac2, ae2);
            topHandle2.SetRotation(stream, math.mul(fromTo2, rotA2));


            static (float abc1, float abe1, float abc2, float abe2) calculateAngle_(
                (float lac, float lae, float lab, float lbc) _1,
                (float lac, float lae, float lab, float lbc) _2)
            {
                var acae = new float4(_1.lac, _1.lae, _2.lac, _2.lae);
                var ab = new float4(_1.lab, _1.lab, _2.lab, _2.lab);
                var bc = new float4(_1.lbc, _1.lbc, _2.lbc, _2.lbc);

                var a4 = TriangleAngle4(acae, ab, bc);
                //var abcAngle1ac = TriangleAngle(lac1, lab1, lbc1);
                //var abeAngle1ae = TriangleAngle(lae1, lab1, lbc1);
                //var abcAngle2ac = TriangleAngle(lac2, lab2, lbc2);
                //var abeAngle2ae = TriangleAngle(lae2, lab2, lbc2);

                return (a4.x, a4.y, a4.z, a4.w);

                static float4 TriangleAngle4(float4 aLen, float4 aLen1, float4 aLen2)
                {
                    var c = math.clamp((aLen1 * aLen1 + aLen2 * aLen2 - aLen * aLen) / (aLen1 * aLen2) / 2.0f, -1.0f, 1.0f);
                    return math.acos(c);
                }
            }
        }

    }


    public static class FootIkUtility
    {



        public static void SolveTwoBoneIk<TTfRot, TTfPos, TStream>(
            this TStream stream,
            ref TTfRot tfrotTop, ref TTfRot tfrotMid,
            TTfPos tfposTop, TTfPos tfposMid, TTfPos tfposFoot,
            float3 posEffector)
                where TTfRot : ITransformWorldRotationProxy<TStream>
                where TTfPos : ITransformWorldPositionProxy<TStream>
                where TStream : ITransformStreamSource
        {
            var aRotation = tfrotTop.GetRotation(stream);
            var bRotation = tfrotMid.GetRotation(stream);

            var aPosition = tfposTop.GetPosition(stream);
            var bPosition = tfposMid.GetPosition(stream);
            var cPosition = tfposFoot.GetPosition(stream);
            var ePosition = posEffector;

            var ab = bPosition - aPosition;
            var bc = cPosition - bPosition;
            var ac = cPosition - aPosition;
            var ae = ePosition - aPosition;

            var lab = math.length(ab);
            var lac = math.length(ac);
            var lae = math.length(ae);
            var lbc = math.length(bc);
            //var lbe = math.length(bPosition - ePosition);//
            var abcAngle = TriangleAngle(lac, lab, lbc);
            var abeAngle = TriangleAngle(lae, lab, lbc);
            var angle = abcAngle - abeAngle;
            var axis = math.rotate(bRotation, Vector3.right);
            //var axis = math.normalize(math.cross(ab, -bc));

            var rotFromTo_mid = quaternion.AxisAngle(axis, angle);
            var rot_mid = math.mul(rotFromTo_mid, bRotation);
            //tfrotMid.SetRotation(stream, rot_mid);

            var cPosition_applied = math.rotate(rotFromTo_mid, bc) + bPosition;
            var ac_applied = cPosition_applied - aPosition;
            var rotFromTo_top = IkExtension.fromToRotation(ac_applied, ae);
            var rot_top = math.mul(rotFromTo_top, aRotation);
            //tfrotTop.SetRotation(stream, rot_top);

            tfrotTop.SetRotation(stream, rot_top);
            tfrotMid.SetRotation(stream, math.mul(rotFromTo_top, rot_mid));
            //lowstream.SetRotation(stream, eRotation);


            static float TriangleAngle(float aLen, float aLen1, float aLen2)
            {
                //float c = Mathf.Clamp((aLen1 * aLen1 + aLen2 * aLen2 - aLen * aLen) / (aLen1 * aLen2) / 2.0f, -1.0f, 1.0f);
                var d = (aLen1 * aLen1 + aLen2 * aLen2 - aLen * aLen) / (aLen1 * aLen2) * 0.5f;
                var c = math.clamp(d, -1.0f, 1.0f);
                return math.acos(c);
            }
        }



        public static void SolveTwoBonePairIk<TTfRot, TTfPos, TStream>(
            this TStream stream,
            ref TTfRot tfrotTop1, ref TTfRot tfrotMid1,
            TTfPos tfposTop1, TTfPos tfposMid1, TTfPos tfposLow1,
            float3 posEffector1,
            ref TTfRot tfrotTop2, ref TTfRot tfrotMid2,
            TTfPos tfposTop2, TTfPos tfposMid2, TTfPos tfposLow2,
            float3 posEffector2)
                where TTfRot : ITransformWorldRotationProxy<TStream>
                where TTfPos : ITransformWorldPositionProxy<TStream>
                where TStream : ITransformStreamSource
        {
            var rottop1 = tfrotTop1.GetRotation(stream);
            var rotmid1 = tfrotMid1.GetRotation(stream);
            var postop1 = tfposTop1.GetPosition(stream);
            var posmid1 = tfposMid1.GetPosition(stream);
            var poslow1 = tfposLow1.GetPosition(stream);
            var rottop2 = tfrotTop2.GetRotation(stream);
            var rotmid2 = tfrotMid2.GetRotation(stream);
            var postop2 = tfposTop2.GetPosition(stream);
            var posmid2 = tfposMid2.GetPosition(stream);
            var poslow2 = tfposLow2.GetPosition(stream);

            var rot = SolveTwoBonePairIk_(
                rottop1, rotmid1, postop1, posmid1, poslow1, posEffector1,
                rottop2, rotmid2, postop2, posmid2, poslow2, posEffector2);

            // footpos == effector のとき、nan になることへの対策。
            // 計算前に抜けてもいいが、片足単位で分岐させるより平均的な速度で速い方がいいんじゃないかなと
            tfrotTop1.SetRotation(stream, math.select(rot.top1.value, rottop1.value, math.isnan(rot.top1.value.x)));
            tfrotMid1.SetRotation(stream, math.select(rot.mid1.value, rotmid1.value, math.isnan(rot.mid1.value.x)));
            tfrotTop2.SetRotation(stream, math.select(rot.top2.value, rottop2.value, math.isnan(rot.top2.value.x)));
            tfrotMid2.SetRotation(stream, math.select(rot.mid2.value, rotmid2.value, math.isnan(rot.mid2.value.x)));

            //var rot = SolveTwoBonePairIk_(
            //    tfrotTop1.GetRotation(stream), tfrotMid1.GetRotation(stream),
            //    tfposTop1.GetPosition(stream), tfposMid1.GetPosition(stream), tfposLow1.GetPosition(stream),
            //    posEffector1,
            //    tfrotTop2.GetRotation(stream), tfrotMid2.GetRotation(stream),
            //    tfposTop2.GetPosition(stream), tfposMid2.GetPosition(stream), tfposLow2.GetPosition(stream),
            //    posEffector2);
            //if (!float.IsNaN(rot.rotTop1.value.x))
            //{
            //    tfrotTop1.SetRotation(stream, rot.rotTop1);
            //    tfrotMid1.SetRotation(stream, rot.rotMid1);
            //}
            //if (!float.IsNaN(rot.rotTop2.value.x))
            //{
            //    tfrotTop2.SetRotation(stream, rot.rotTop2);
            //    tfrotMid2.SetRotation(stream, rot.rotMid2);
            //}
            //tfrotTop1.SetRotation(stream, rot.rotTop1);
            //tfrotMid1.SetRotation(stream, rot.rotMid1);
            //tfrotTop2.SetRotation(stream, rot.rotTop2);
            //tfrotMid2.SetRotation(stream, rot.rotMid2);
        }

        public static (quaternion top1, quaternion mid1, quaternion top2, quaternion mid2) SolveTwoBonePairIk_(
            quaternion rotA1, quaternion rotB1, float3 posA1, float3 posB1, float3 posC1, float3 posEffector1,
            quaternion rotA2, quaternion rotB2, float3 posA2, float3 posB2, float3 posC2, float3 posEffector2)
        {

            var posE1 = posEffector1;
            var posE2 = posEffector2;

            var ab1 = posB1 - posA1;
            var bc1 = posC1 - posB1;
            var ac1 = posC1 - posA1;
            var ae1 = posE1 - posA1;

            var ab2 = posB2 - posA2;
            var bc2 = posC2 - posB2;
            var ac2 = posC2 - posA2;
            var ae2 = posE2 - posA2;

            var angle = calculateAngle_(ab1, ac1, ae1, bc1, ab2, ac2, ae2, bc2);

            var rot1 = calcRotation_(rotA1, rotB1, posA1, posB1, bc1, ae1, angle.a1);
            var rot2 = calcRotation_(rotA2, rotB2, posA2, posB2, bc2, ae2, angle.a2);

            return (rot1.top, rot1.mid, rot2.top, rot2.mid);
            

            static (float a1, float a2) calculateAngle_(
                float3 ab1, float3 ac1, float3 ae1, float3 bc1,
                float3 ab2, float3 ac2, float3 ae2, float3 bc2)
            {
                var lab1 = math.lengthsq(ab1);
                var lac1 = math.lengthsq(ac1);
                var lae1 = math.lengthsq(ae1);
                var lbc1 = math.lengthsq(bc1);

                var lab2 = math.lengthsq(ab2);
                var lac2 = math.lengthsq(ac2);
                var lae2 = math.lengthsq(ae2);
                var lbc2 = math.lengthsq(bc2);

                var acae = new float4(lac1, lae1, lac2, lae2);
                var ab = new float4(lab1, lab1, lab2, lab2);
                var bc = new float4(lbc1, lbc1, lbc2, lbc2);

                var a4 = TriangleAngle4sq_(acae, ab, bc);
                //var abcAngle1ac = TriangleAngle(lac1, lab1, lbc1);
                //var abeAngle1ae = TriangleAngle(lae1, lab1, lbc1);
                //var abcAngle2ac = TriangleAngle(lac2, lab2, lbc2);
                //var abeAngle2ae = TriangleAngle(lae2, lab2, lbc2);

                return (a4.x - a4.y, a4.z - a4.w);
                //return (abcAngle1ac - abeAngle1ae, abcAngle2ac - abeAngle2ae);

                static float4 TriangleAngle4sq_(float4 aLenSq, float4 aLen1Sq, float4 aLen2Sq)
                {
                    //float c = Mathf.Clamp((aLen1 * aLen1 + aLen2 * aLen2 - aLen * aLen) / (aLen1 * aLen2) / 2.0f, -1.0f, 1.0f);
                    var d = (aLen1Sq + aLen2Sq - aLenSq) / (math.sqrt(aLen1Sq) * math.sqrt(aLen2Sq)) * 0.5f;
                    var c = math.clamp(d, -1.0f, 1.0f);
                    return math.acos(c);
                }
            }

            static (quaternion top, quaternion mid) calcRotation_(
                quaternion rotA, quaternion rotB, float3 posA, float3 posB, float3 bc, float3 ae, float angle)
            {
                var axis = math.rotate(rotB, Vector3.right);

                var rotFromTo_mid = quaternion.AxisAngle(axis, angle);
                var rot_mid = math.mul(rotFromTo_mid, rotB);

                var posC_new = math.rotate(rotFromTo_mid, bc) + posB;
                var ac_new = posC_new - posA;
                var rotFromTo_top = IkExtension.fromToRotation(ac_new, ae);
                var rot_top = math.mul(rotFromTo_top, rotA);

                var rot_top_result = rot_top;
                var rot_mid_result = math.mul(rotFromTo_top, rot_mid);

                return (rot_top_result, rot_mid_result);
            }
        }
    }

}
