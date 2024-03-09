using System;

namespace AnimLite
{


    public struct StreamDataHolder<T, TCache, TIndex> : IDisposable
        where T : unmanaged
        where TCache : IStreamCache, IDisposable
        where TIndex : IStreamIndex, IDisposable
    {

        //public KeyStreamsInOneArray<T> Streams;

        //public KeyStreamSections Sections;

        public StreamData<T> Streams { get; set; }

        public TCache Cache;

        public TIndex Index;


        public void Dispose()
        {
            this.Streams.Dispose();
            this.Cache.Dispose();
            this.Index.Dispose();
        }



        // •ÏŠ· --------------------------------

        public KeyFinder<T, TKey, TClip, TProcedure, TCache, TIndex> ToKeyFinderWith<TKey, TClip, TProcedure>(StreamingTimer timer)
            where TKey : struct, IKey<T>, IKeyWithCache<TCache>, IKeyInterpolative<T>, IKeyCursor
            where TProcedure : IStreamProcedure, new()
            where TClip : IKeyClipper, new()
        =>
            new KeyFinder<T, TKey, TClip, TProcedure, TCache, TIndex>
            {
                core = new KeyFinderCore<T, TCache, TIndex>
                {
                    Streams = this.Streams,
                    cache = this.Cache,
                    index = this.Index,
                },
                Timer = timer,
            };

        public KeyFinderWithoutProcedure<T, TKey, TClip, TCache, TIndex> ToKeyFinderWith<TKey, TClip>()
            where TKey : struct, IKey<T>, IKeyWithCache<TCache>, IKeyInterpolative<T>, IKeyCursor
            where TClip : IKeyClipper, new()
        =>
            new KeyFinderWithoutProcedure<T, TKey, TClip, TCache, TIndex>
            {
                core = new KeyFinderCore<T, TCache, TIndex>
                {
                    Streams = this.Streams,
                    cache = this.Cache,
                    index = this.Index,
                },
            };

        // --------------------------------
    }



    public static class StreamDataHolderExtension
    {


        public static StreamDataHolder<T, DummyStreamCache, DummyStreamIndex> ToHolder<T>(this StreamData<T> streams)
            where T : unmanaged
        =>
            new StreamDataHolder<T, DummyStreamCache, DummyStreamIndex>
            {
                Streams = streams,
            };

        public static StreamDataHolder<T, DummyStreamCache, StreamIndex> ToHolderWith<T>(this StreamData<T> streams, StreamIndex index)
            where T : unmanaged
        =>
            new StreamDataHolder<T, DummyStreamCache, StreamIndex>
            {
                Streams = streams,
                Index = index,
            };

        public static StreamDataHolder<T, TCache, DummyStreamIndex> ToHolderWith<T, TCache>(this StreamData<T> streams, TCache cache)
            where T : unmanaged
            where TCache : IStreamCache, IDisposable
        =>
            new StreamDataHolder<T, TCache, DummyStreamIndex>
            {
                Streams = streams,
                Cache = cache,
            };

        public static StreamDataHolder<T, TCache, StreamIndex> ToHolderWith<T, TCache>(this StreamData<T> streams, TCache cache, StreamIndex index)
            where T : unmanaged
            where TCache : IStreamCache, IDisposable
        =>
            new StreamDataHolder<T, TCache, StreamIndex>
            {
                Streams = streams,
                Cache = cache,
                Index = index,
            };
    }

}
