using System;
using Unity.Collections;

namespace AnimLite
{
    interface IAllocatable
    {
        void Alloc(int length);
    }


    public struct DummyStreamCache : IStreamCache, IDisposable
    {
        public void Dispose() { }
    }


    /// <summary>
    /// 
    /// </summary>
    public struct Key1StreamCache<T> : IDisposable, IStreamCache, IAllocatable
        where T : unmanaged
    {

        public NativeArray<Key1Cursor> CursorCaches;

        public NativeArray<T> ValueCaches;


        public void Dispose()
        {
            this.CursorCaches.Dispose();
            this.ValueCaches.Dispose();
        }

        public void Alloc(int length)
        {
            var alloc = Allocator.Persistent;
            var opt = NativeArrayOptions.UninitializedMemory;

            CursorCaches = new NativeArray<Key1Cursor>(length, alloc, opt);
            ValueCaches = new NativeArray<T>(length, alloc, opt);
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public struct Key2StreamCache<T> : IDisposable, IStreamCache, IAllocatable
        where T : unmanaged
    {

        public NativeArray<Key2Cursor> CursorCaches;

        public NativeArray<Key2Value<T>> ValueCaches;


        public void Dispose()
        {
            this.CursorCaches.Dispose();
            this.ValueCaches.Dispose();
        }

        public void Alloc(int length)
        {
            var alloc = Allocator.Persistent;
            var opt = NativeArrayOptions.UninitializedMemory;

            CursorCaches = new NativeArray<Key2Cursor>(length, alloc, opt);
            ValueCaches = new NativeArray<Key2Value<T>>(length, alloc, opt);
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public struct Key4StreamCache<T> : IDisposable, IStreamCache, IAllocatable
        where T : unmanaged
    {

        public NativeArray<Key4Cursor> CursorCaches;

        public NativeArray<Key4Value<T>> ValueCaches;


        public void Dispose()
        {
            this.CursorCaches.Dispose();
            this.ValueCaches.Dispose();
        }

        public void Alloc(int length)
        {
            var alloc = Allocator.Persistent;
            var opt = NativeArrayOptions.UninitializedMemory;

            CursorCaches = new NativeArray<Key4Cursor>(length, alloc, opt);
            ValueCaches = new NativeArray<Key4Value<T>>(length, alloc, opt);
        }
    }






    static class StreamCacheExtention
    {

        //public static Key1StreamCache<T> CreateKey1Cache<T>(this StreamData<T> streams)
        //    where T : unmanaged
        //{
        //    var length = streams.Sections.Sections.Length;
        //    var alloc = Allocator.Persistent;
        //    var opt = NativeArrayOptions.UninitializedMemory;

        //    return new Key1StreamCache<T>
        //    {
        //        CursorCaches = new NativeArray<Key1Cursor>(length, alloc, opt),
        //        ValueCaches = new NativeArray<T>(length, alloc, opt),
        //    };
        //}

        //public static Key2StreamCache<T> CreateKey2Cache<T>(this StreamData<T> streams)
        //    where T : unmanaged
        //{
        //    var length = streams.Sections.Sections.Length;
        //    var alloc = Allocator.Persistent;
        //    var opt = NativeArrayOptions.UninitializedMemory;

        //    return new Key2StreamCache<T>
        //    {
        //        CursorCaches = new NativeArray<Key2Cursor>(length, alloc, opt),
        //        ValueCaches = new NativeArray<Key2Value<T>>(length, alloc, opt),
        //    };
        //}

        //public static Key4StreamCache<T> CreateKey4Cache<T>(this StreamData<T> streams)
        //    where T : unmanaged
        //{
        //    var length = streams.Sections.Sections.Length;
        //    var alloc = Allocator.Persistent;
        //    var opt = NativeArrayOptions.UninitializedMemory;

        //    return new Key4StreamCache<T>
        //    {
        //        CursorCaches = new NativeArray<Key4Cursor>(length, alloc, opt),
        //        ValueCaches = new NativeArray<Key4Value<T>>(length, alloc, opt),
        //    };
        //}





        public static CacheFactory<T, Key1StreamCache<T>> ToKey1CacheFactory<T>(this StreamData<T> streams)
            where T : unmanaged
        =>
            new CacheFactory<T, Key1StreamCache<T>>
            {
                streams = streams,
            };

        public static CacheFactory<T, Key2StreamCache<T>> ToKey2CacheFactory<T>(this StreamData<T> streams)
            where T : unmanaged
        =>
            new CacheFactory<T, Key2StreamCache<T>>
            {
                streams = streams,
            };

        public static CacheFactory<T, Key4StreamCache<T>> ToKey4CacheFactory<T>(this StreamData<T> streams)
            where T : unmanaged
        =>
            new CacheFactory<T, Key4StreamCache<T>>
            {
                streams = streams,
            };

        public struct CacheFactory<T, TCache>
            where T : unmanaged
            where TCache : IStreamCache, IAllocatable, new()
        {

            public StreamData<T> streams;


            public TCache CreateCacheWithInitialize<TClip, TKey>(StreamingTimer timer)
                where TClip : IKeyClipper, new()
                where TKey : struct, IKey<T>, IKeyWithCache<TCache>
            {
                var cache = new TCache();
                cache.Alloc(this.streams.Sections.Length);

                var clip = new TClip();
                clip.SetTimer(timer);

                var key = new TKey();
                for (var istream = 0; istream < this.streams.Sections.Sections.Length; istream++)
                {
                    var s = streams.GetStream(istream);
                    key.MakeKeyAbsolute(s, 0, clip);
                    key.StoreToCache(cache, istream);
                }

                return cache;
            }
        }
    }




}
