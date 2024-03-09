using Unity.Mathematics;
using UnityEngine;

namespace AnimLite
{

    public interface ITransformProxy
    {
        void SetTransform(Animator anim, Transform tf);
    }


    public interface ITransformStreamSource<TProxy>
        where TProxy : ITransformProxy
    {
        float3 GetPosition(TProxy tfp);
        float3 GetLocalPosition(TProxy tfp);

        quaternion GetRotation(TProxy tfp);
        quaternion GetLocalRotation(TProxy tfp);

        void SetPosition(TProxy tfp, float3 p);
        void SetLocalPosition(TProxy s, float3 p);

        void SetRotation(TProxy tfp, quaternion r);
        void SetLocalRotation(TProxy tfp, quaternion r);
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

