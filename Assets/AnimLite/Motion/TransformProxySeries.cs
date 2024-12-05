using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;

namespace AnimLite
{



    public struct TfHandle : ITransformProxy
    {

        public TransformStreamHandle handle;

        public void SetTransform(Animator anim, Transform tf) => this.handle = anim.BindStreamTransform(tf);


        public struct StreamSource : ITransformStreamSource<TfHandle>
        {
            public AnimationStream stream;


            public float3 GetPosition(TfHandle tfp) => tfp.handle.GetPosition(stream);
            public float3 GetLocalPosition(TfHandle tfp) => tfp.handle.GetLocalPosition(stream);

            public quaternion GetRotation(TfHandle tfp) => tfp.handle.GetRotation(stream);
            public quaternion GetLocalRotation(TfHandle tfp) => tfp.handle.GetLocalRotation(stream);

            public void SetPosition(TfHandle tfp, float3 p) => tfp.handle.SetPosition(stream, p);
            public void SetLocalPosition(TfHandle tfp, float3 p) => tfp.handle.SetLocalPosition(stream, p);

            public void SetRotation(TfHandle tfp, quaternion r) => tfp.handle.SetRotation(stream, r);
            public void SetLocalRotation(TfHandle tfp, quaternion r) => tfp.handle.SetLocalRotation(stream, r);
        }
    }

    public struct Tf : ITransformProxy
    {

        public Transform tf;

        public void SetTransform(Animator anim, Transform tf) => this.tf = tf;


        public struct StreamSource : ITransformStreamSource<Tf>
        {
            public float3 GetPosition(Tf tfp) => tfp.tf.position;
            public float3 GetLocalPosition(Tf tfp) => tfp.tf.localPosition;

            public quaternion GetRotation(Tf tfp) => tfp.tf.rotation;
            public quaternion GetLocalRotation(Tf tfp) => tfp.tf.localRotation;

            public void SetPosition(Tf tfp, float3 p) => tfp.tf.position = p;
            public void SetLocalPosition(Tf tfp, float3 p) => tfp.tf.localPosition = p;

            public void SetRotation(Tf tfp, quaternion r) => tfp.tf.rotation = r;
            public void SetLocalRotation(Tf tfp, quaternion r) => tfp.tf.localRotation = r;
        }
    }

}
