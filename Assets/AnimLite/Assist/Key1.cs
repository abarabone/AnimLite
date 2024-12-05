namespace AnimLite
{
    /// <summary>
    /// 
    /// </summary>
    public struct Key1Cursor// : IKeyCursor
    {
        public int CurrentIndex;

        public float TimeFrom { get; set; }
        //public float TimeFrom => float.NegativeInfinity;// でいいのかな？ ← ダメ、ラスト値で無限ループになる
        public float TimeTo { get; set; }
    }



    //    public struct Key1<T> : IKey1<T, Key1<T>>
    //        where T : unmanaged
    //    {

    //        public Key1Cursor cursor { get; set; }
    //        public T value { get; set; }

    //        public T Interpolate(float time) => this.value;

    //        public T AdjustNext(T t0, T t1) => t1;
    //    }

    //    public interface IKey1<T, TKey> : IKey<T, Key1StreamCache<T>, TKey>
    //        where T : unmanaged
    //        where TKey : struct, IKey1<T, TKey>
    //    {
    //        Key1Cursor cursor { get; set; }
    //        T value { get; set; }

    //        float IKeyCursor.TimeFrom => this.cursor.TimeFrom;
    //        float IKeyCursor.TimeTo => this.cursor.TimeTo;

    //        TKey IKey<T, Key1StreamCache<T>, TKey>.LoadFromCache(Key1StreamCache<T> p, int istream)
    //        {
    //            this.cursor = p.CursorCaches[istream];
    //            this.value = p.ValueCaches[istream];
    //            return (TKey)this;
    //        }
    //        void IKey<T, Key1StreamCache<T>, TKey>.StoreToCache(Key1StreamCache<T> p, int istream)
    //        {
    //            p.CursorCaches[istream] = this.cursor;
    //            p.ValueCaches[istream] = this.value;
    //        }


    //        TKey IKey<T, Key1StreamCache<T>, TKey>.makeCacheAbsolute<TClip>(StreamPairManipulator<T> s, int ikey, TClip clip)
    //        {
    //            var keysrc = s.GetKey(ikey, clip);
    //            var nexttime = s.GetTime(ikey + 1, clip);

    //            var cursor = new Key1Cursor
    //            {
    //                CurrentIndex = keysrc.ikey,

    //                TimeFrom = keysrc.time,
    //                TimeTo = nexttime.time,
    //            };

    //            var value = keysrc.key;

    //            this.cursor = cursor;
    //            this.value = value;
    //            return (TKey)this;
    //        }

    //        TKey IKey<T, Key1StreamCache<T>, TKey>.makeCacheNext<TClip>(StreamPairManipulator<T> s, TClip clip)
    //        {
    //            //return this.makeCacheAbsolute(s, this.cursor.CurrentIndex + 1, clip);
    //            var keysrc = s.GetValueAndNextTime(this.cursor.CurrentIndex + 1, clip);

    //            var cursor = new Key1Cursor
    //            {
    //                CurrentIndex = keysrc.ikey,

    //                TimeFrom = this.cursor.TimeTo,
    //                TimeTo = keysrc.nexttime,
    //            };

    //            var value = keysrc.key;

    //            this.cursor = cursor;
    //            this.value = value;
    //            return (TKey)this;
    //        }
    //    }






}
