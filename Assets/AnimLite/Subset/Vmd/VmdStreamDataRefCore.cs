//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using Unity.Mathematics;
//using UnityEngine;

//namespace AnimLite.Vmd
//{
//    using AnimLite.Vrm;
//    using AnimLite.Utility;
//    using VRM;


//    /// <summary>
//    /// 
//    /// </summary>
//    public class VmdStreamDataRefCore : IDisposable
//    {
//        public StreamDataHolder<quaternion, Key4StreamCache<quaternion>, StreamIndex> RotationStreams;
//        public StreamDataHolder<float4, Key4StreamCache<float4>, StreamIndex> PositionStreams;
//        public StreamDataHolder<float, Key2StreamCache<float>, StreamIndex> FaceStreams;

//        public bool IsCreated => this.RotationStreams.Streams.KeyStreams.Values.IsCreated;


//        IDisposable dataSource;


//        public void Dispose()
//        {
//            // �L�[�L���b�V�������j������B�ق��̓f�[�^�L���b�V���ɒu�����̂Ŕj�����Ȃ��B
//            this.PositionStreams.Cache.Dispose();
//            this.RotationStreams.Cache.Dispose();
//            this.FaceStreams.Cache.Dispose();

//            this.dataSource.Dispose();

//            "VmdStreamDataRefCore disposed".ShowDebugLog();
//        }
//    }

//}
