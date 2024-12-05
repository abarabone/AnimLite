namespace AnimLite
{


    public interface IStreamCache
    {

    }


    //public interface IKeyCacher
    //{
    //    TKey LoadKey<T, TKey>(int istream)
    //        where T : unmanaged
    //        where TKey : struct, IKey<T>, IKeyInterpolative<T>, IKeyCursor
    //    ;

    //    void SaveKey<T, TKey>(int istream, TKey key)
    //        where T : unmanaged
    //        where TKey : struct, IKey<T>, IKeyInterpolative<T>, IKeyCursor
    //    ;
    //}




    //public struct CacheFactory<T, TCache>
    //    where T : unmanaged
    //    where TCache : IStreamCache
    //{

    //    public StreamData<T> streams;

    //    public TCache cache;


    //    public TCache Initialize<TClip, TKey>(StreamingTimer timer)
    //        where TClip : IKeyClipper, new()
    //        where TKey : struct, IKey<T>, IKeyWithCache<TCache>
    //    {
    //        var clip = new TClip();
    //        clip.SetTimer(timer);

    //        var key = new TKey();
    //        for (var istream = 0; istream < this.streams.Sections.Sections.Length; istream++)
    //        {
    //            var s = streams.GetStream(istream);
    //            key.MakeKeyAbsolute<TClip>(s, 0, clip);
    //            key.StoreToCache(this.cache, istream);
    //        }

    //        return this.cache;
    //    }
    //}

}
