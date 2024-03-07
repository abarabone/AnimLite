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
                where TTf : ITransformProxy
                where TStream : ITransformStreamSource<TTf>
        {
            stream.SolveTwoBoneIk(topHandle, midHandle, lowHandle, posEffector);

            stream.SetRotation(lowHandle, rotEffector);
        }

        public static void SolveTwoBoneIk<TTf, TStream>(
            this TStream stream,
            TTf topHandle, TTf midHandle, TTf lowHandle,
            float3 posEffector)
            where TTf : ITransformProxy
            where TStream : ITransformStreamSource<TTf>
        {
            var aRotation = stream.GetRotation(topHandle);
            var bRotation = stream.GetRotation(midHandle);
            //var eRotation = rotEffector;

            var aPosition = stream.GetPosition(topHandle);
            var bPosition = stream.GetPosition(midHandle);
            var cPosition = stream.GetPosition(lowHandle);
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
            stream.SetRotation(midHandle, wrot);

            cPosition = stream.GetPosition(lowHandle);
            ac = cPosition - aPosition;
            //var fromTo = Quaternion.FromToRotation(ac, ae).As_quaternion();
            var fromTo = IkExtension.fromToRotation(ac, ae);
            stream.SetRotation(topHandle, math.mul(fromTo, aRotation));

            //lowHandle.SetRotation(stream, eRotation);


            static float TriangleAngle(float aLen, float aLen1, float aLen2)
            {
                var c = math.clamp((aLen1 * aLen1 + aLen2 * aLen2 - aLen * aLen) / (aLen1 * aLen2) / 2.0f, -1.0f, 1.0f);
                //float c = Mathf.Clamp((aLen1 * aLen1 + aLen2 * aLen2 - aLen * aLen) / (aLen1 * aLen2) / 2.0f, -1.0f, 1.0f);
                return math.acos(c);
            }
        }
        static quaternion fromToRotation(float3 from, float3 to)
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
                where TTf : ITransformProxy
                where TStream : ITransformStreamSource<TTf>
        {
            stream.SolveTwoBonePairIk(
                topHandle1, midHandle1, lowHandle1, posEffector1,
                topHandle2, midHandle2, lowHandle2, posEffector2);

            stream.SetRotation(lowHandle1, rotEffector1);
            stream.SetRotation(lowHandle2, rotEffector2);
        }

        public static void SolveTwoBonePairIk<TTf, TStream>(
            this TStream stream,
            TTf topHandle1, TTf midHandle1, TTf lowHandle1, float3 posEffector1,
            TTf topHandle2, TTf midHandle2, TTf lowHandle2, float3 posEffector2)
                where TTf : ITransformProxy
                where TStream : ITransformStreamSource<TTf>
        {
            var rotA1 = stream.GetRotation(topHandle1);
            var rotB1 = stream.GetRotation(midHandle1);

            var posA1 = stream.GetPosition(topHandle1);
            var posB1 = stream.GetPosition(midHandle1);
            var posC1 = stream.GetPosition(lowHandle1);
            var posE1 = posEffector1;

            var ab1 = posB1 - posA1;
            var bc1 = posC1 - posB1;
            var ac1 = posC1 - posA1;
            var ae1 = posE1 - posA1;


            var rotA2 = stream.GetRotation(topHandle2);
            var rotB2 = stream.GetRotation(midHandle2);

            var posA2 = stream.GetPosition(topHandle2);
            var posB2 = stream.GetPosition(midHandle2);
            var posC2 = stream.GetPosition(lowHandle2);
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
            stream.SetRotation(midHandle1, wrot1);

            posC1 = stream.GetPosition(lowHandle1);
            ac1 = posC1 - posA1;
            var fromTo1 = fromToRotation(ac1, ae1);
            stream.SetRotation(topHandle1, math.mul(fromTo1, rotA1));


            var angle2 = a.abc2 - a.abe2;
            var axis2 = math.rotate(rotB2, Vector3.right);

            var rotFromTo2 = quaternion.AxisAngle(axis2, angle2);

            var wrot2 = math.mul(rotFromTo2, rotB2);
            stream.SetRotation(midHandle2, wrot2);

            posC2 = stream.GetPosition(lowHandle2);
            ac2 = posC2 - posA2;
            var fromTo2 = fromToRotation(ac2, ae2);
            stream.SetRotation(topHandle2, math.mul(fromTo2, rotA2));


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
}
