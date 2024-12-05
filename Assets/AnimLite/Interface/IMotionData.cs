//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using Unity.Mathematics;
//using UnityEngine;

//namespace AnimLite
//{

//    /// <summary>
//    /// 
//    /// </summary>
//    public interface IMotionData<TRotStream, TPosStream, TFaceStream> : IDisposable
//        where TRotStream : streamdataho
//    {
//        StreamDataHolder<quaternion, Key4StreamCache<quaternion>, StreamIndex> RotationStreams { get; }
//        StreamDataHolder<float4, Key4StreamCache<float4>, StreamIndex> PositionStreams { get; }
//        StreamDataHolder<float, Key2StreamCache<float>, StreamIndex> FaceStreams { get; }

//        //bool IsCreated => this.RotationStreams.Streams.KeyStreams.Values.IsCreated;
//    }

//}
