using Unity.Mathematics;

namespace AnimLite
{
    /// <summary>
    /// 
    /// </summary>
    public struct Key2Cursor : IKeyCursor
    {
        public int IndexTo;

        public float TimeFrom { get; set; }
        public float TimeTo { get; set; }

        public float FromToTimeRate;
    }

    /// <summary>
    /// 
    /// </summary>
    public struct Key2Value<T>
        where T : unmanaged
    {
        public T From;
        public T To;
    }



    public struct Key2NearestShift : IKey2<float>
    {

        public Key2Cursor cursor { get; set; }
        public Key2Value<float> value { get; set; }

        public float Interpolate(float time)
        {
            var from = this.cursor.TimeFrom;
            var to = this.cursor.TimeTo;
            var ratio = this.cursor.FromToTimeRate;

            var v1 = this.value.From;
            var v2 = this.value.To;
            return Interpolation.NearestShift(v1, v2, time, from, to, ratio);
        }

        public float AdjustNext(float v0, float v1)
        {
            return v1;
        }

        // -------------------------------------------------------
        public float TimeFrom => this.cursor.TimeFrom;
        public float TimeTo => this.cursor.TimeTo;

        public void LoadFromCache(Key2StreamCache<float> p, int istream) =>
            p.loadFromCache(istream, out this);

        public void StoreToCache(Key2StreamCache<float> p, int istream) =>
            p.storeToCache(istream, this);


        public void MakeKeyAbsolute<TClip>(StreamPairManipulator<float> s, int ikey, TClip clip)
            where TClip : IKeyClipper
        =>
            s.makeKeyAbsolute(ikey, out this, clip);

        public void MakeKeyNext<TClip>(StreamPairManipulator<float> s, TClip clip)
            where TClip : IKeyClipper
        =>
            s.makeKeyNext(ref this, clip);
        // -------------------------------------------------------
    }

    //public struct Key2LerpPos : IKey2<float4, Key2LerpPos>
    //{

    //    public Key2Cursor cursor { get; set; }
    //    public Key2Value<float4> value { get; set; }

    //    public float4 Interpolate(float time)
    //    {
    //        var _t = (time - this.cursor.TimeFrom) * this.cursor.FromToTimeRate;
    //        var t = math.clamp(_t, 0, 1);

    //        var v1 = this.value.From;
    //        var v2 = this.value.To;
    //        return math.lerp(v1, v2, t);
    //    }

    //    public float4 AdjustNext(float4 v0, float4 v1)
    //    {
    //        return v1;
    //    }
    //}
    //public struct Key2LerpRot : IKey2<quaternion, Key2LerpRot>
    //{

    //    public Key2Cursor cursor { get; set; }
    //    public Key2Value<quaternion> value { get; set; }

    //    public quaternion Interpolate(float time)
    //    {
    //        var _t = (time - this.cursor.TimeFrom) * this.cursor.FromToTimeRate;
    //        var t = math.clamp(_t, 0, 1);

    //        var v1 = this.value.From.value;
    //        var v2 = this.value.To.value;
    //        return math.lerp(v1, v2, t);
    //    }

    //    public quaternion AdjustNext(quaternion q0, quaternion q1)
    //    {
    //        return Interpolation.adjust_quaterion(q0, q1);
    //    }
    //}
    public struct Key2Lerp : IKey2<float>
    {

        public Key2Cursor cursor { get; set; }
        public Key2Value<float> value { get; set; }

        public float Interpolate(float time)
        {
            var _t = (time - this.cursor.TimeFrom) * this.cursor.FromToTimeRate;
            var t = math.clamp(_t, 0, 1);

            var v1 = this.value.From;
            var v2 = this.value.To;
            return math.lerp(v1, v2, t);
        }

        public float AdjustNext(float f0, float f1)
        {
            return f1;
        }

        // -------------------------------------------------------
        public float TimeFrom => this.cursor.TimeFrom;
        public float TimeTo => this.cursor.TimeTo;

        public void LoadFromCache(Key2StreamCache<float> p, int istream) =>
            p.loadFromCache(istream, out this);

        public void StoreToCache(Key2StreamCache<float> p, int istream) =>
            p.storeToCache(istream, this);


        public void MakeKeyAbsolute<TClip>(StreamPairManipulator<float> s, int ikey, TClip clip)
            where TClip : IKeyClipper
        =>
            s.makeKeyAbsolute(ikey, out this, clip);

        public void MakeKeyNext<TClip>(StreamPairManipulator<float> s, TClip clip)
            where TClip : IKeyClipper
        =>
            s.makeKeyNext(ref this, clip);
        // -------------------------------------------------------
    }

    public interface IKey2<T> : IKey<T>, IKeyWithCache<Key2StreamCache<T>>, IKeyInterpolative<T>, IKeyCursor
        where T : unmanaged
    {
        Key2Cursor cursor { get; set; }
        Key2Value<T> value { get; set; }
    }


    static class Key2Extension
    {

        public static void loadFromCache<T, TKey>(this Key2StreamCache<T> cache, int istream, out TKey key)
            where T : unmanaged
            where TKey : IKey2<T>, new()
        {
            key = new();
            key.cursor = cache.CursorCaches[istream];
            key.value = cache.ValueCaches[istream];
        }
        public static void storeToCache<T, TKey>(this Key2StreamCache<T> cache, int istream, TKey key)
            where T : unmanaged
            where TKey : IKey2<T>
        {
            cache.CursorCaches[istream] = key.cursor;
            cache.ValueCaches[istream] = key.value;
        }


        public static void makeKeyAbsolute<T, TKey, TClip>(this StreamPairManipulator<T> s, int ikey, out TKey key, TClip clip)
            where T : unmanaged
            where TKey : IKey2<T>, new()
            where TClip : IKeyClipper
        {
            key = new();

            var keysrc0 = s.GetKey(ikey + 0, clip);
            var keysrc1 = s.GetKey(ikey + 1, clip);
            //var i0 = ikey;// c.ClipIndex(ikey + 0, s.Times.Length);
            //var i1 = clip.ClipKeyIndex(ikey + 1, s.Times.Length, s.lengthR);

            var cursor = new Key2Cursor
            {
                IndexTo = keysrc1.ikey,//i1,

                TimeFrom = keysrc0.time,//s.Times[i0],
                TimeTo = keysrc1.time,//s.Times[i1],
            };
            var d = cursor.TimeTo - cursor.TimeFrom;
            cursor.FromToTimeRate = 1.0f / math.select(d, 1.0f, d == 0.0f);

            var value = new Key2Value<T>
            {
                From = keysrc0.key,//s.Values[i0],
                To = keysrc1.key,//s.Values[i1],
            };
            value = key.adjustAll_(value);

            key.cursor = cursor;
            key.value = value;
        }
        static Key2Value<T> adjustAll_<T, TKey>(this TKey key, Key2Value<T> value)
            where T : unmanaged
            where TKey : IKey2<T>
        {
            value.To = key.AdjustNext(value.From, value.To);
            return value;
        }

        public static void makeKeyNext<T, TKey, TClip>(this StreamPairManipulator<T> s, ref TKey key, TClip clip)
            where T : unmanaged
            where TKey : IKey2<T>, new()
            where TClip : IKeyClipper
        {
            var keysrc = s.GetKey(key.cursor.IndexTo + 1, clip);

            var prevCursor = key.cursor;
            var newCursor = new Key2Cursor
            {
                IndexTo = keysrc.ikey,

                TimeFrom = prevCursor.TimeTo,
                TimeTo = keysrc.time,
            };
            var d = newCursor.TimeTo - newCursor.TimeFrom;
            //newcursor.FromToTimeRate = 1.0f / math.select(d, 1.0f, d == 0.0f);
            newCursor.FromToTimeRate = math.select(1.0f / d, 0.0f, prevCursor.IndexTo == newCursor.IndexTo);// Nan 出ると思うがだいじょぶだろ
            //if (d == 0)
            //    Debug.Log($"{(Vmd.MmdBodyBones)istream} {newcursor.TimeFrom} {newcursor.TimeTo} {newcursor.FromToTimeRate} {d}");

            var prevValue = key.value;
            var newValue = new Key2Value<T>
            {
                From = prevValue.To,
                To = keysrc.key,
            };
            newValue.To = key.AdjustNext(newValue.From, newValue.To);

            key.cursor = newCursor;
            key.value = newValue;
        }
    }


}
