namespace AnimLite
{


    public interface IKey<T>
        where T : unmanaged
    {

        void MakeKeyAbsolute<TClip>(StreamPairManipulator<T> s, int ikey, TClip c = default)
            where TClip : IKeyClipper
            ;

        void MakeKeyNext<TClip>(StreamPairManipulator<T> s, TClip c = default)
            where TClip : IKeyClipper
            ;

    }

    public interface IKeyCursor
    {
        float TimeFrom { get; }
        float TimeTo { get; }
    }

    public interface IKeyInterpolative<T>
        where T : unmanaged
    {

        T Interpolate(float time);


        /// <summary>
        /// 次に値を取得するときに調整する
        /// </summary>
        T AdjustNext(T p0, T p1) => p1;
    }

    public interface IKeyWithCache<TCache>
        where TCache : IStreamCache
    {

        void LoadFromCache(TCache p, int istream);

        void StoreToCache(TCache p, int istream);

        //void LoadFromCache<TC>(TCache p, int istream) where TC : IStreamCache;

        //void StoreToCache<TC>(TCache p, int istream) where TC : IStreamCache;

    }



    // インターフェースのデフォルト実装で Burst が効かないっぽいので、残念だが没

    //public interface IKey<T, TCache, TKey> : IKeyCursor
    //    where T:unmanaged
    //    where TCache : IStreamCache<T>
    //    where TKey : struct, IKey<T, TCache, TKey>
    //{

    //    TKey LoadFromCache(TCache p, int istream);
    //    void StoreToCache(TCache p, int istream);


    //    TKey makeCacheAbsolute<TClip>(StreamPairManipulator<T> s, int ikey, TClip c = default)
    //        where TClip : IKeyClipper
    //        ;
    //    TKey makeCacheNext<TClip>(StreamPairManipulator<T> s, TClip c = default)
    //        where TClip : IKeyClipper
    //        ;



    //    T Interpolate(float time);


    //    /// <summary>
    //    /// 次に値を取得するときに調整する
    //    /// </summary>
    //    T AdjustNext(T p0, T p1) => p1;
    //}

}
