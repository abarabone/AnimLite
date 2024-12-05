namespace AnimLite
{

    //public static class Procedure
    //{
    //    public static Bruteforce Bruteforce =>
    //        default;

    //    public static Absolute Absolute =>
    //        default;

    //    public static Forward<TCache> Forward<TCache>(TCache cache) where TCache : IStreamCache =>
    //        new Forward<TCache> { cache = cache };
    //}


    public struct Bruteforce : IStreamProcedure
    {
        public T GetValue<T, TKey, TClip, TCache, TIndex>(StreamData<T> streams, int istream, StreamingTimer timer, TClip clip, TCache cache, TIndex index)
            where T : unmanaged
            where TClip : IKeyClipper
            where TKey : struct, IKey<T>, IKeyWithCache<TCache>, IKeyInterpolative<T>, IKeyCursor
            where TCache : IStreamCache
            where TIndex : IStreamIndex
        =>
            streams.getbruteforce<T, TCache, TKey, TClip>(istream, cache, timer, clip);
    }

    public struct Absolute : IStreamProcedure
    {
        public T GetValue<T, TKey, TClip, TCache, TIndex>(StreamData<T> streams, int istream, StreamingTimer timer, TClip clip, TCache cache, TIndex index)
            where T : unmanaged
            where TClip : IKeyClipper
            where TKey : struct, IKey<T>, IKeyWithCache<TCache>, IKeyInterpolative<T>, IKeyCursor
            where TCache : IStreamCache
            where TIndex : IStreamIndex
        =>
            streams.getabsolute<T, TCache, TKey, TClip, TIndex>(istream, cache, timer, clip, index);
    }

    public struct Forward : IStreamProcedure
    {
        public T GetValue<T, TKey, TClip, TCache, TIndex>(StreamData<T> streams, int istream, StreamingTimer timer, TClip clip, TCache cache, TIndex index)
            where T : unmanaged
            where TClip : IKeyClipper
            where TKey : struct, IKey<T>, IKeyWithCache<TCache>, IKeyInterpolative<T>, IKeyCursor
            where TCache : IStreamCache
            where TIndex : IStreamIndex
        =>
            streams.getforward<T, TCache, TKey, TClip>(istream, cache, timer, clip);
    }




    //public enum ProcedureType
    //{
    //    none,
    //    absolute,
    //    forward,
    //    bruteforce,
    //}

    //public struct XProcedure : IStreamProcedure
    //{

    //    public ProcedureType mode;


    //    public T GetValue<T, TCache, TKey, TClip>(
    //        TCache cache, StreamPackage<T> streams, int istream,
    //        StreamingTimer timer, TClip clip)

    //        where T : unmanaged
    //        where TCache : IStreamCache
    //        where TClip : IKeyClipper
    //        where TKey : struct, IKey<T, TCache>
    //    =>
    //        this.mode switch
    //        {
    //            ProcedureType.absolute =>
    //                cache.getabsolute<T, TCache, TKey, TClip>(streams, istream, timer, clip),
    //            ProcedureType.forward =>
    //                cache.getforward<T, TCache, TKey, TClip>(streams, istream, timer, clip),
    //            ProcedureType.bruteforce =>
    //                cache.getbruteforce<T, TCache, TKey, TClip>(streams, istream, timer, clip),
    //            _ => default,
    //        };


    //    static public XProcedure Absolute => new XProcedure { mode = ProcedureType.absolute };
    //    static public XProcedure Forward => new XProcedure { mode = ProcedureType.forward };
    //    static public XProcedure Bruteforce => new XProcedure { mode = ProcedureType.bruteforce };
    //}



    static class MoveExtension
    {


        public static T getbruteforce<T, TCache, TKey, TClip>(
            this StreamData<T> streams, int istream, TCache cache, StreamingTimer timer, TClip clip)
                where T : unmanaged
                where TCache : IStreamCache
                where TKey : struct, IKey<T>, IKeyWithCache<TCache>, IKeyInterpolative<T>, IKeyCursor
                where TClip : IKeyClipper
        {
            clip.SetTimer(timer);

            var s = streams.GetStream(istream);

            for (var ikey = s.Times.Length; ikey-- > 0;)
            {
                if (timer.CurrentTime >= s.Times[ikey])
                {
                    var key = new TKey();
                    key.MakeKeyAbsolute(s, ikey, clip);

                    key.StoreToCache(cache, istream);
                    return key.Interpolate(timer.CurrentTime);
                }
            }

            {
                var ikey = s.Values.Length - 1;

                var key = new TKey();
                key.MakeKeyAbsolute(s, ikey, clip);

                key.StoreToCache(cache, istream);
                return key.Interpolate(timer.CurrentTime);
            }
        }

        public static T getabsolute<T, TCache, TKey, TClip, TIndex>(
            this StreamData<T> streams, int istream, TCache cache, StreamingTimer timer, TClip clip, TIndex index)
                where T : unmanaged
                where TCache : IStreamCache
                where TIndex : IStreamIndex
                where TKey : struct, IKey<T>, IKeyWithCache<TCache>, IKeyInterpolative<T>, IKeyCursor
                where TClip : IKeyClipper
        {
            clip.SetTimer(timer);

            var s = streams.GetStream(istream);

            var ikey = (s.Times, index).GetKeyIndex(istream, timer.CurrentTime);

            var key = new TKey();
            key.MakeKeyAbsolute(s, ikey, clip);

            key.StoreToCache(cache, istream);
            return key.Interpolate(timer.CurrentTime);
        }

        public static T getforward<T, TCache, TKey, TClip>(
            this StreamData<T> streams, int istream, TCache cache, StreamingTimer timer, TClip clip)
                where T : unmanaged
                where TCache : IStreamCache
                where TKey : struct, IKey<T>, IKeyWithCache<TCache>, IKeyInterpolative<T>, IKeyCursor
                where TClip : IKeyClipper
        {
            clip.SetTimer(timer);

            var key = new TKey();
            key.LoadFromCache(cache, istream);
            if (!clip.IsNextKey(key, timer.CurrentTime))
            {
                return key.Interpolate(timer.CurrentTime);
            }

            var s = streams.GetStream(istream);

            do
            {
                key.MakeKeyNext(s, clip);
            }
            while (clip.IsNextKey(key, timer.CurrentTime));

            key.StoreToCache(cache, istream);
            return key.Interpolate(timer.CurrentTime);
        }

    }
}
