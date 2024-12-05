namespace AnimLite
{




    public struct KeyFinder<T, TKey, TClip, TProcedure, TCache, TIndex> : IKeyFinder<T>
        where T : unmanaged
        where TCache : IStreamCache
        where TIndex : IStreamIndex
        where TClip : IKeyClipper, new()
        where TProcedure : IStreamProcedure, new()
        where TKey : struct, IKey<T>, IKeyWithCache<TCache>, IKeyInterpolative<T>, IKeyCursor
    {

        public StreamData<T> Streams => this.core.Streams;

        public float IndexBlockTimeRange => this.core.index.IndexBlockTimeRange;


        public StreamingTimer Timer { get; set; }

        public KeyFinderCore<T, TCache, TIndex> core;
        //public StreamDataHolder<T, TCache, TIndex> Data { get; set; }

        public TClip clip;
        public TProcedure procedure;


        public T get(int istream) =>
            this.core.get<TKey, TClip, TProcedure>(istream, this.Timer, this.clip, this.procedure);
        //this.procedure.GetValue<T, TKey, TClip, TCache, TIndex>(this.Data.Streams, istream, this.Timer, clip, this.Data.Cache, this.Data.Index);

    }



    public struct KeyFinderCore<T, TCache, TIndex>
        where T : unmanaged
        where TCache : IStreamCache
        where TIndex : IStreamIndex
    {

        public StreamData<T> Streams { get; set; }

        //public KeyStreamsInOneArray<T> Streams;

        //public KeyStreamSections Sections;

        public TCache cache { get; set; }

        public TIndex index { get; set; }


        public T get<TKey, TClip, TProcedure>(
            int istream, StreamingTimer timer, TClip clip = default, TProcedure procedure = default)
                where TClip : IKeyClipper, new()
                where TProcedure : IStreamProcedure, new()
                where TKey : struct, IKey<T>, IKeyWithCache<TCache>, IKeyInterpolative<T>, IKeyCursor
        =>
            procedure.GetValue<T, TKey, TClip, TCache, TIndex>(this.Streams, istream, timer, clip, this.cache, this.index);



        // 変換 --------------------------------

        public KeyFinderWithoutProcedure<T, TKey, TClip, TCache, TIndex> With<TKey, TClip>()
            where TKey : struct, IKey<T>, IKeyWithCache<TCache>, IKeyInterpolative<T>, IKeyCursor
            where TClip : IKeyClipper, new()
        =>
            new KeyFinderWithoutProcedure<T, TKey, TClip, TCache, TIndex>
            {
                core = this,
            };

        public KeyFinder<T, TKey, TClip, TProcedure, TCache, TIndex> With<TKey, TClip, TProcedure>(StreamingTimer timer)
            where TKey : struct, IKey<T>, IKeyWithCache<TCache>, IKeyInterpolative<T>, IKeyCursor
            where TProcedure : IStreamProcedure, new()
            where TClip : IKeyClipper, new()
        =>
            new KeyFinder<T, TKey, TClip, TProcedure, TCache, TIndex>
            {
                core = this,
                Timer = timer,
            };

        // --------------------------------
    }





    public struct KeyFinderWithoutProcedure<T, TKey, TClip, TCache, TIndex> : IKeyFinderWithoutProcedure<T>
        where T : unmanaged
        where TClip : IKeyClipper, new()
        where TKey : struct, IKey<T>, IKeyWithCache<TCache>, IKeyInterpolative<T>, IKeyCursor
        where TCache : IStreamCache
        where TIndex : IStreamIndex
    {

        public StreamData<T> Streams => this.core.Streams;

        public float IndexBlockTimeRange => this.core.index.IndexBlockTimeRange;


        public KeyFinderCore<T, TCache, TIndex> core;
        //public StreamDataHolder<T, TCache, TIndex> Data { get; set; }

        public TClip clip;


        public T get<TProcedure>(int istream, StreamingTimer timer, TProcedure procedure = default)
            where TProcedure : IStreamProcedure, new()
        =>
            this.core.get<TKey, TClip, TProcedure>(istream, timer, this.clip, procedure);
        //procedure.GetValue<T, TKey, TClip, TCache, TIndex>(this.Data.Streams, istream, timer, clip, this.Data.Cache, this.Data.Index);
    }

    public struct KeyFinderProcedureAdapter<T, TProcedure, TKeyFinderSrc> : IKeyFinder<T>
        where T : unmanaged
        where TKeyFinderSrc : IKeyFinderWithoutProcedure<T>
        where TProcedure : IStreamProcedure, new()
    {

        public StreamData<T> Streams => this.kf.Streams;

        public float IndexBlockTimeRange => this.kf.IndexBlockTimeRange;


        public TKeyFinderSrc kf;



        public StreamingTimer Timer { get; set; }


        public T get(int istream) =>
            this.kf.get<TProcedure>(istream, this.Timer);
    }





    public static class KeyFinderFactoryExtension
    {


        public static KeyFinderProcedureAdapter<T, TProcedure, TKeyFinderSrc> With<T, TKeyFinderSrc, TProcedure>(
            this TKeyFinderSrc src, StreamingTimer timer, TProcedure procedure = default)
                where T : unmanaged
                where TKeyFinderSrc : IKeyFinderWithoutProcedure<T>
                where TProcedure : IStreamProcedure, new()
        =>
            new KeyFinderProcedureAdapter<T, TProcedure, TKeyFinderSrc>
            {
                kf = src,
                Timer = timer,
            };

        public static KeyFinderCore<T, DummyStreamCache, DummyStreamIndex> ToKeyFinder<T>(this StreamData<T> streams)
            where T : unmanaged
        =>
            new KeyFinderCore<T, DummyStreamCache, DummyStreamIndex>
            {
                Streams = streams,
            };

        public static KeyFinderCore<T, DummyStreamCache, StreamIndex> ToKeyFinder<T>(this StreamData<T> streams, StreamIndex index)
            where T : unmanaged
        =>
            new KeyFinderCore<T, DummyStreamCache, StreamIndex>
            {
                Streams = streams,
                index = index,
            };

        public static KeyFinderCore<T, TCache, DummyStreamIndex> ToKeyFinder<T, TCache>(this StreamData<T> streams, TCache cache)
            where T : unmanaged
            where TCache : IStreamCache
        =>
            new KeyFinderCore<T, TCache, DummyStreamIndex>
            {
                Streams = streams,
                cache = cache,
            };

        public static KeyFinderCore<T, TCache, StreamIndex> ToKeyFinder<T, TCache>(this StreamData<T> streams, TCache cache, StreamIndex index)
            where T : unmanaged
            where TCache : IStreamCache
        =>
            new KeyFinderCore<T, TCache, StreamIndex>
            {
                Streams = streams,
                cache = cache,
                index = index,
            };
    }


}

