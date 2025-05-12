using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Jobs;
using System;
using System.Runtime.InteropServices;

namespace AnimLite
{

    using AnimLite.Utility;


    public struct TfHandle : ITransformProxy<TfHandle.StreamSource>
    {

        public TransformStreamHandle handle;

        public void SetTransform(Animator anim, Transform tf) => this.handle = anim.BindStreamTransform(tf);


        public float3 GetPosition(StreamSource src) => this.handle.GetPosition(src.stream);
        public void SetPosition(StreamSource src, float3 p) => this.handle.SetPosition(src.stream, p);

        public quaternion GetRotation(StreamSource src) => this.handle.GetRotation(src.stream);
        public void SetRotation(StreamSource src, quaternion r) => this.handle.SetRotation(src.stream, r);

        public float3 GetLocalPosition(StreamSource src) => this.handle.GetLocalPosition(src.stream);
        public void SetLocalPosition(StreamSource src, float3 p) => this.handle.SetLocalPosition(src.stream, p);

        public quaternion GetLocalRotation(StreamSource src) => this.handle.GetLocalRotation(src.stream);
        public void SetLocalRotation(StreamSource src, quaternion r) => this.handle.SetLocalRotation(src.stream, r);


        public struct StreamSource : ITransformStreamSource
        {
            public AnimationStream stream;
        }
    }

    public struct Tf : ITransformProxy<Tf.StreamSource>
    {

        public Transform tf;

        public void SetTransform(Animator anim, Transform tf) => this.tf = tf;


        public float3 GetPosition(StreamSource src) => this.tf.position;
        public void SetPosition(StreamSource src, float3 p) => this.tf.position = p;

        public quaternion GetRotation(StreamSource src) => this.tf.rotation;
        public void SetRotation(StreamSource src, quaternion r) => this.tf.rotation = r;

        public float3 GetLocalPosition(StreamSource src) => this.tf.localPosition;
        public void SetLocalPosition(StreamSource src, float3 p) => this.tf.localPosition = p;

        public quaternion GetLocalRotation(StreamSource src) => this.tf.localRotation;
        public void SetLocalRotation(StreamSource src, quaternion r) => this.tf.localRotation = r;


        public struct StreamSource : ITransformStreamSource
        { }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct TfPosision : ITransformWorldPositionProxy<ValueStreamSource>
    {
        [FieldOffset(0)]
        public float3 pos;
        [FieldOffset(0)]
        public float4 posAs4;

        public void SetTransform(Animator anim, Transform tf) => this.pos = tf.position;


        public float3 GetPosition(ValueStreamSource src) => this.pos;
        public void SetPosition(ValueStreamSource src, float3 p) => this.pos = p;


        //public static implicit operator TfPosision(float3 src) => new TfPosision { pos = src };
    }
    public struct TfRotation : ITransformWorldRotationProxy<ValueStreamSource>
    {

        public quaternion rot;

        public void SetTransform(Animator anim, Transform tf) => this.rot = tf.rotation;


        public quaternion GetRotation(ValueStreamSource src) => this.rot;
        public void SetRotation(ValueStreamSource src, quaternion r) => this.rot = r;


        //public static implicit operator TfRotation(quaternion src) => new TfRotation { rot = src };
    }

    public struct ValueStreamSource : ITransformStreamSource
    { }


    public static class TfProxyUtility
    {
        public static TfPosision ToProxy(this float3 src) => new TfPosision { pos = src };
        public static TfPosision ToProxy(this float4 src) => new TfPosision { pos = src.xyz };
        public static TfRotation ToProxy(this quaternion src) => new TfRotation { rot = src };
    }


    //public static class StreamSourceUtility
    //{
    //    public static float3 GetPosition<TSrc, Tf>(this TSrc srcstream, Tf tfproxy)
    //        where TSrc : ITransformStreamSource
    //        where Tf : ITransformWorldPosition
    //    =>
    //        tfproxy.GetPosition(srcstream);

    //    public static void SetPosition<TSrc, Tf>(this TSrc srcstream, Tf tfproxy, float3 p)
    //        where TSrc : ITransformStreamSource
    //        where Tf : ITransformWorldPosition
    //    =>
    //        tfproxy.SetPosition(srcstream, p);

    //    public static quaternion GetRotation<TSrc, Tf>(this TSrc srcstream, Tf tfproxy)
    //        where TSrc : ITransformStreamSource
    //        where Tf : ITransformWorldRotation
    //    =>
    //        tfproxy.GetRotation(srcstream);

    //    public static void SetRotation<TSrc, Tf>(this TSrc srcstream, Tf tfproxy, quaternion r)
    //        where TSrc : ITransformStreamSource
    //        where Tf : ITransformWorldRotation
    //    =>
    //        tfproxy.SetRotation(srcstream, r);

    //    public static float3 GetLocalPosition<TSrc, Tf>(this TSrc srcstream, Tf tfproxy)
    //        where TSrc : ITransformStreamSource
    //        where Tf : ITransformLocalPosition
    //    =>
    //        tfproxy.GetLocalPosition(srcstream);

    //    public static void SetLocalPosition<TSrc, Tf>(this TSrc srcstream, Tf tfproxy, float3 p)
    //        where TSrc : ITransformStreamSource
    //        where Tf : ITransformLocalPosition
    //    =>
    //        tfproxy.SetLocalPosition(srcstream, p);

    //    public static quaternion GetLocalRotation<TSrc, Tf>(this TSrc srcstream, Tf tfproxy)
    //        where TSrc : ITransformStreamSource
    //        where Tf : ITransformeLocalRotation
    //    =>
    //        tfproxy.GetLocalRotation(srcstream);

    //    public static void SetLocalRotation<TSrc, Tf>(this TSrc srcstream, Tf tfproxy, quaternion r)
    //        where TSrc : ITransformStreamSource
    //        where Tf : ITransformeLocalRotation
    //    =>
    //        tfproxy.SetLocalRotation(srcstream, r);

    //}

    //public struct TfAccess : ITransformProxy
    //{

    //    public void SetTransform(Animator anim, Transform tf) { }


    //    public struct StreamSource : ITransformStreamSource<TfAccessProxy>
    //    {
    //        public float3 GetPosition(TfAccessProxy tfp) => tfp.tf.position;
    //        public float3 GetLocalPosition(TfAccessProxy tfp) => tfp.tf.localPosition;

    //        public quaternion GetRotation(TfAccessProxy tfp) => tfp.tf.rotation;
    //        public quaternion GetLocalRotation(TfAccessProxy tfp) => tfp.tf.localRotation;

    //        public void SetPosition(TfAccessProxy tfp, float3 p) => tfp.tf.position = p;
    //        public void SetLocalPosition(TfAccessProxy tfp, float3 p) => tfp.tf.localPosition = p;

    //        public void SetRotation(TfAccessProxy tfp, quaternion r) => tfp.tf.rotation = r;
    //        public void SetLocalRotation(TfAccessProxy tfp, quaternion r) => tfp.tf.localRotation = r;
    //    }

    //    public struct TfAccessProxy : ITransformProxy
    //    {
    //        public TransformAccess tf;

    //        public void SetTransform(Animator anim, Transform tf) { }
    //    }
    //}
}
