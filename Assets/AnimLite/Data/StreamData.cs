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

    //public struct StreamData<T, TCache> : IDisposable
    //    where T : unmanaged
    //    where TCache : IStreamCache, IDisposable
    //{

    //    //public KeyStreamsInOneArray<T> KeyStreams;

    //    //public KeyStreamSections Sections;

    //    public StreamData<T> Streams;

    //    public TCache Cache;


    //    public void Dispose()
    //    {
    //        //this.Sections.Dispose();
    //        //this.KeyStreams.Dispose();
    //        this.Streams.Dispose();
    //        this.Cache.Dispose();
    //    }
    //}

    //public struct IndexedStreamData<T> : IDisposable
    //    where T : unmanaged
    //{

    //    //public KeyStreamsInOneArray<T> KeyStreams;

    //    //public KeyStreamSections Sections;

    //    public StreamData<T> Streams;

    //    public StreamIndex Index;


    //    public void Dispose()
    //    {
    //        //this.Sections.Dispose();
    //        //this.KeyStreams.Dispose();
    //        this.Streams.Dispose();
    //        this.Index.Dispose();
    //    }
    //}

    //public struct IndexedStreamData<T, TCache> : IDisposable
    //    where T : unmanaged
    //    where TCache : IStreamCache, IDisposable
    //{

    //    //public KeyStreamsInOneArray<T> KeyStreams;

    //    //public KeyStreamSections Sections;

    //    public StreamData<T> Streams;

    //    public StreamIndex Index;

    //    public TCache Cache;


    //    public void Dispose()
    //    {
    //        //this.Sections.Dispose();
    //        //this.KeyStreams.Dispose();
    //        this.Streams.Dispose();
    //        this.Cache.Dispose();
    //        this.Index.Dispose();
    //    }
    //}





    public static class StreamPakageExtension
    {

        ///// <summary>
        ///// 
        ///// </summary>
        //public static (int ikey, float time, T value) GetKeyDirect<T>(
        //    this StreamPackage<T> src, int istream, float time)
        //    where T : unmanaged
        //{
        //    var s = src.GetStream(istream);

        //    var ikey = (s.Times, src.Index).GetKeyIndex(istream, time);

        //    return s.GetKey<Direct>(ikey);
        //}


        //public static IndexedStreamData<T, TCache> WithCacheAndIndex<T, TCache>(this StreamData<T> data, int indexBlockLength)
        //    where T : unmanaged
        //    where TCache : IStreamCache, IAllocatable<T>, IDisposable, new()
        //{
        //    var index = new StreamIndex(data.KeyStreams.FrameTimes, data.Sections, indexBlockLength);
        //    var cache = new TCache();
        //    cache.Alloc(data);
        //    return new IndexedStreamData<T, TCache>
        //    {
        //        Streams = data,
        //        Index = index,
        //        Cache = cache,
        //    };
        //}

        //public static StreamData<T, TCache> WithCache<T, TCache>(this StreamData<T> data)
        //    where T : unmanaged
        //    where TCache : IStreamCache, IAllocatable<T>, IDisposable, new()
        //{
        //    var cache = new TCache();
        //    cache.Alloc(data);
        //    return new StreamData<T, TCache>
        //    {
        //        Streams = data,
        //        Cache = cache,
        //    };
        //}

        //public static IndexedStreamData<T> WithIndex<T>(this StreamData<T> data, int indexBlockLength)
        //    where T : unmanaged
        //{
        //    var index = new StreamIndex(data.KeyStreams.FrameTimes, data.Sections, indexBlockLength);
        //    return new IndexedStreamData<T>
        //    {
        //        Streams = data,
        //        Index = index,
        //    };
        //}
    }

}
