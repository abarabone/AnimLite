using Unity.Mathematics;

namespace AnimLite
{
    /// <summary>
    /// 
    /// </summary>
    public struct Key4Cursor// : IKeyCursor
    {
        public int IndexTo;
        public int IndexNext;

        public float TimeFrom;// { get; set; }
        public float TimeTo;// { get; set; }
        public float TimeNext;

        public float FromToTimeRate;
    }

    /// <summary>
    /// 
    /// </summary>
    public struct Key4Value<T>
        where T : unmanaged
    {
        public T Prev;
        public T From;
        public T To;
        public T Next;
    }



    //public struct Key4DualLerpPos : IKey4<float4>
    //{

    //    public Key4Cursor cursor { get; set; }
    //    public Key4Value<float4> value { get; set; }

    //    public float4 Interpolate(float time)
    //    {
    //        var _t = (time - this.cursor.TimeFrom) * this.cursor.FromToTimeRate;
    //        var t = math.clamp(_t, 0, 1);

    //        var v0 = this.value.Prev;
    //        var v1 = this.value.From;
    //        var v2 = this.value.To;
    //        var v3 = this.value.Next;
    //        return Interpolation.DualLerp(v0, v1, v2, v3, t);
    //    }

    //    public float4 AdjustNext(float4 v0, float4 v1)
    //    {
    //        return v1;
    //    }
    //}
    //public struct Key4DualLerpRot : IKey4<quaternion>
    //{

    //    public Key4Cursor cursor { get; set; }
    //    public Key4Value<quaternion> value { get; set; }

    //    public quaternion Interpolate(float time)
    //    {
    //        var _t = (time - this.cursor.TimeFrom) * this.cursor.FromToTimeRate;
    //        var t = math.clamp(_t, 0, 1);

    //        var v0 = this.value.Prev.value;
    //        var v1 = this.value.From.value;
    //        var v2 = this.value.To.value;
    //        var v3 = this.value.Next.value;
    //        return Interpolation.DualLerp(v0, v1, v2, v3, t);
    //        //return math.normalize(Interpolation.DualLeap(v0, v1, v2, v3, t).As_quaternion());
    //    }

    //    public quaternion AdjustNext(quaternion q0, quaternion q1)
    //    {
    //        return Interpolation.adjust_quaterion(q0, q1);
    //    }
    //}

    public struct Key4Catmul : IKey4<float>
    {

        public Key4Cursor cursor { get; set; }
        public Key4Value<float> value { get; set; }

        public float Interpolate(float time)
        {
            var _t = (time - this.cursor.TimeFrom) * this.cursor.FromToTimeRate;
            var t = math.clamp(_t, 0, 1);

            var v0 = this.value.Prev;
            var v1 = this.value.From;
            var v2 = this.value.To;
            var v3 = this.value.Next;
            return Interpolation.CatmullRom(v0, v1, v2, v3, t);
        }

        public float AdjustNext(float v0, float v1)
        {
            return v1;
        }
        // -------------------------------------------------------
        public float TimeFrom => this.cursor.TimeFrom;
        public float TimeTo => this.cursor.TimeTo;

        public void LoadFromCache(Key4StreamCache<float> p, int istream) =>
            p.loadFromCache(istream, out this);

        public void StoreToCache(Key4StreamCache<float> p, int istream) =>
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

    public struct Key4CatmulPos : IKey4<float4>
    {

        public Key4Cursor cursor { get; set; }
        public Key4Value<float4> value { get; set; }

        public float4 Interpolate(float time)
        {
            var _t = (time - this.cursor.TimeFrom) * this.cursor.FromToTimeRate;
            var t = math.clamp(_t, 0, 1);

            var v0 = this.value.Prev;
            var v1 = this.value.From;
            var v2 = this.value.To;
            var v3 = this.value.Next;
            return Interpolation.CatmullRom(v0, v1, v2, v3, t);
        }

        public float4 AdjustNext(float4 v0, float4 v1)
        {
            return v1;
        }
        // -------------------------------------------------------
        public float TimeFrom => this.cursor.TimeFrom;
        public float TimeTo => this.cursor.TimeTo;

        public void LoadFromCache(Key4StreamCache<float4> p, int istream) =>
            p.loadFromCache(istream, out this);

        public void StoreToCache(Key4StreamCache<float4> p, int istream) =>
            p.storeToCache(istream, this);


        public void MakeKeyAbsolute<TClip>(StreamPairManipulator<float4> s, int ikey, TClip clip)
            where TClip : IKeyClipper
        =>
            s.makeKeyAbsolute(ikey, out this, clip);

        public void MakeKeyNext<TClip>(StreamPairManipulator<float4> s, TClip clip)
            where TClip : IKeyClipper
        =>
            s.makeKeyNext(ref this, clip);
        // -------------------------------------------------------
    }
    public struct Key4CatmulRot : IKey4<quaternion>
    {

        public Key4Cursor cursor { get; set; }
        public Key4Value<quaternion> value { get; set; }

        public quaternion Interpolate(float time)
        {
            var _t = (time - this.cursor.TimeFrom) * this.cursor.FromToTimeRate;
            var t = math.clamp(_t, 0, 1);

            var v0 = this.value.Prev.value;
            var v1 = this.value.From.value;
            var v2 = this.value.To.value;
            var v3 = this.value.Next.value;
            return Interpolation.CatmullRom(v0, v1, v2, v3, t);
            //return math.normalize(Interpolation.CatmullRom(v0, v1, v2, v3, t).As_quaternion());
        }

        public quaternion AdjustNext(quaternion q0, quaternion q1)
        {
            return Interpolation.adjust_quaterion(q0, q1);
        }


        // -------------------------------------------------------
        public float TimeFrom => this.cursor.TimeFrom;
        public float TimeTo => this.cursor.TimeTo;

        public void LoadFromCache(Key4StreamCache<quaternion> p, int istream) =>
            p.loadFromCache(istream, out this);

        public void StoreToCache(Key4StreamCache<quaternion> p, int istream) =>
            p.storeToCache(istream, this);


        public void MakeKeyAbsolute<TClip>(StreamPairManipulator<quaternion> s, int ikey, TClip clip)
            where TClip : IKeyClipper
        =>
            s.makeKeyAbsolute(ikey, out this, clip);

        public void MakeKeyNext<TClip>(StreamPairManipulator<quaternion> s, TClip clip)
            where TClip : IKeyClipper
        =>
            s.makeKeyNext(ref this, clip);
        // -------------------------------------------------------
    }


    public interface IKey4<T> : IKey<T>, IKeyWithCache<Key4StreamCache<T>>, IKeyInterpolative<T>, IKeyCursor
        where T : unmanaged
    {
        Key4Cursor cursor { get; set; }
        Key4Value<T> value { get; set; }
    }



    static class Key4Extension
    {

        public static void loadFromCache<T, TKey>(this Key4StreamCache<T> cache, int istream, out TKey key)
            where T : unmanaged
            where TKey : IKey4<T>, new()
        {
            key = new();
            key.cursor = cache.CursorCaches[istream];
            key.value = cache.ValueCaches[istream];
        }
        public static void storeToCache<T, TKey>(this Key4StreamCache<T> cache, int istream, TKey key)
            where T : unmanaged
            where TKey : IKey4<T>
        {
            cache.CursorCaches[istream] = key.cursor;
            cache.ValueCaches[istream] = key.value;
        }


        public static void makeKeyAbsolute<T, TKey, TClip>(this StreamPairManipulator<T> s, int ikey, out TKey key, TClip clip)
            where T : unmanaged
            where TKey : IKey4<T>, new()
            where TClip : IKeyClipper
        {
            key = new();

            var keysrc_ = s.GetKey(ikey - 1, clip);
            var keysrc0 = s.GetKey(ikey + 0, clip);
            var keysrc1 = s.GetKey(ikey + 1, clip);
            var keysrc2 = s.GetKey(ikey + 2, clip);

            var cursor = new Key4Cursor
            {
                IndexTo = keysrc1.ikey,// i1,
                IndexNext = keysrc2.ikey,//i2,

                TimeFrom = keysrc0.time,//c0.AdjustKeyTime(s.Times[i0]),
                TimeTo = keysrc1.time,//c1.AdjustKeyTime(s.Times[i1]),
                TimeNext = keysrc2.time,//c2.AdjustKeyTime(s.Times[i2]),
            };
            var d = cursor.TimeTo - cursor.TimeFrom;
            cursor.FromToTimeRate = math.select(1.0f / d, 0.0f, d == 0.0f);// Nan 出ると思うがだいじょぶだろ
            //cursor.FromToTimeRate = 1.0f / math.select(d, 1.0f, d == 0.0f);

            var value = new Key4Value<T>
            {
                Prev = keysrc_.key,//s.Values[i_],
                From = keysrc0.key,//s.Values[i0],
                To = keysrc1.key,//s.Values[i1],
                Next = keysrc2.key,//s.Values[i2],
            };
            value = key.adjustAll_(value);

            key.cursor = cursor;
            key.value = value;
        }
        static Key4Value<T> adjustAll_<T, TKey>(this TKey key, Key4Value<T> value)
            where T : unmanaged
            where TKey : IKey4<T>
        {
            value.From = key.AdjustNext(value.Prev, value.From);
            value.To = key.AdjustNext(value.From, value.To);
            value.Next = key.AdjustNext(value.To, value.Next);
            return value;
        }

        public static void makeKeyNext<T, TKey, TClip>(this StreamPairManipulator<T> s, ref TKey key, TClip clip)
            where T : unmanaged
            where TKey : IKey4<T>, new()
            where TClip : IKeyClipper
        {
            var keysrc = s.GetKey(key.cursor.IndexNext + 1, clip);

            var prevCursor = key.cursor;
            var newCursor = new Key4Cursor
            {
                IndexTo = prevCursor.IndexNext,
                IndexNext = keysrc.ikey,

                TimeFrom = prevCursor.TimeTo,
                TimeTo = prevCursor.TimeNext,
                TimeNext = keysrc.time,
            };
            var d = newCursor.TimeTo - newCursor.TimeFrom;
            newCursor.FromToTimeRate = math.select(1.0f / d, 0.0f, prevCursor.IndexTo == newCursor.IndexTo);// Nan 出ると思うがだいじょぶだろ

            var prevValue = key.value;
            var newValue = new Key4Value<T>
            {
                Prev = prevValue.From,
                From = prevValue.To,
                To = prevValue.Next,
                Next = keysrc.key,
            };
            newValue.Next = key.AdjustNext(newValue.To, newValue.Next);

            key.cursor = newCursor;
            key.value = newValue;
        }
    }



}
