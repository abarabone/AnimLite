using System;

namespace AnimLite
{


    /// <summary>
    /// ストリームに関するデータをセットにした構造体
    /// ・回転、位置、表情、ごとに作成して使用する
    /// </summary>
    public struct StreamData<T> : IDisposable
        where T : unmanaged
    {

        public KeyStreamsInOneArray<T> KeyStreams;

        public KeyStreamSections Sections;


        public bool IsCreated => this.KeyStreams.Values.IsCreated;

        public void Dispose()
        {
            this.Sections.Dispose();
            this.KeyStreams.Dispose();
        }
    }

}
