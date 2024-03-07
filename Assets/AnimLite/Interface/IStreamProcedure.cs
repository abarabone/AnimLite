namespace AnimLite
{


    public interface IStreamProcedure
    {
        T GetValue<T, TKey, TClip, TCache, TIndex>(StreamData<T> streams, int istream, StreamingTimer timer, TClip clip, TCache cache, TIndex index)
            where T : unmanaged
            where TClip : IKeyClipper
            where TKey : struct, IKey<T>, IKeyWithCache<TCache>, IKeyInterpolative<T>, IKeyCursor
            where TCache : IStreamCache
            where TIndex : IStreamIndex
        ;
    }


}
