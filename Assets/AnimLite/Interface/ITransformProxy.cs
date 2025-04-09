using Unity.Mathematics;
using UnityEngine;

namespace AnimLite
{

    public interface ITransformProxy
    {
        void SetTransform(Animator anim, Transform tf);
    }


    public interface ITransformProxy<TSrc> : ITransformWorldProxy<TSrc>, ITransformLocalProxy<TSrc>
        where TSrc : ITransformStreamSource
    {

    }
    public interface ITransformWorldProxy<TSrc> : ITransformWorldPositionProxy<TSrc>, ITransformWorldRotationProxy<TSrc>
        where TSrc : ITransformStreamSource
    {

    }
    public interface ITransformLocalProxy<TSrc> : ITransformLocalPositionProxy<TSrc>, ITransformeLocalRotationProxy<TSrc>
        where TSrc : ITransformStreamSource
    {

    }


    public interface ITransformWorldPositionProxy<TSrc> : ITransformProxy
        where TSrc : ITransformStreamSource
    {
        float3 GetPosition(TSrc stream);

        void SetPosition(TSrc stream, float3 p);
    }
    public interface ITransformWorldRotationProxy<TSrc> : ITransformProxy
        where TSrc : ITransformStreamSource
    {
        quaternion GetRotation(TSrc stream);

        void SetRotation(TSrc stream, quaternion r);
    }

    public interface ITransformLocalPositionProxy<TSrc> : ITransformProxy
        where TSrc : ITransformStreamSource
    {
        float3 GetLocalPosition(TSrc stream);

        void SetLocalPosition(TSrc stream, float3 p);
    }
    public interface ITransformeLocalRotationProxy<TSrc> : ITransformProxy
        where TSrc : ITransformStreamSource
    {
        quaternion GetLocalRotation(TSrc stream);

        void SetLocalRotation(TSrc stream, quaternion r);
    }





    public interface ITransformStreamSource
    {

    }






    public static class TransformProxyExtension
    {
        public static TTfp CreateTransformProxy<TTfp>(this Animator anim, Transform tf)
            where TTfp : ITransformProxy, new()
        {
            var t = new TTfp();

            t.SetTransform(anim, tf);

            return t;
        }
    }



}

