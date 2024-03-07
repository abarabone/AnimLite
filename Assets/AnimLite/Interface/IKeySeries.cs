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
        /// ���ɒl���擾����Ƃ��ɒ�������
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



    // �C���^�[�t�F�[�X�̃f�t�H���g������ Burst �������Ȃ����ۂ��̂ŁA�c�O�����v

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
    //    /// ���ɒl���擾����Ƃ��ɒ�������
    //    /// </summary>
    //    T AdjustNext(T p0, T p1) => p1;
    //}

}
