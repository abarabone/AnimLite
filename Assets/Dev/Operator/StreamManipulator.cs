using Unity.Collections;

namespace AnimLite
{
    public struct StreamPairManipulator<T>
        where T : unmanaged
    {

        public NativeSlice<float> Times;

        public NativeSlice<T> Values;


        public float lengthR;



        ///// <summary>
        ///// 
        ///// </summary>
        //public (int ikey, float time, T key) GetNextKey<TClip>(float time)
        //    where TClip : IKeyClipper<TClip>, new()
        //{
        //    var s = streams.GetKeyStreamPair(istream);
        //    var i = cursor.CurrentIndex + 1;
        //    for (; i + 1 < s.times.Length; i++)
        //    {
        //        var t = s.times[i + 1];

        //        if (time < t)
        //        {
        //            var key = (i, t, s.values[i]);
        //            return cache.StoreKeyCache(istream, key);
        //        }
        //    }
        //    {
        //        var key = (i, float.PositiveInfinity, s.values[i]);
        //        return cache.StoreKeyCache(istream, key);
        //    }
        //}



        /// <summary>
        /// 
        /// </summary>
        public (int ikey, float time, T key) GetKey<TClip>(int ikey, TClip c = default)
            where TClip : IKeyClipper
        {
            var i = c.ClipKeyIndex(ikey, this.Times.Length, this.lengthR);

            return (i, c.AdjustKeyTime(this.Times[i]), this.Values[i]);
        }

        /// <summary>
        /// 
        /// </summary>
        public (int ikey, float nexttime, T key) GetValueAndNextTime<TClip>(int ikey, TClip c = default)// ”pŽ~—\’è
            where TClip : IKeyClipper
        {
            var next = this.GetTime<TClip>(ikey + 1, c);
            var current = this.GetValue<TClip>(ikey, c);

            return (current.ikey, next.time, current.value);
        }

        /// <summary>
        /// 
        /// </summary>
        public (int ikey, float time) GetTime<TClip>(int ikey, TClip c = default)
            where TClip : IKeyClipper
        {
            var i = c.ClipKeyIndex(ikey, this.Times.Length, this.lengthR);

            return (i, c.AdjustKeyTime(this.Times[i]));
        }

        /// <summary>
        /// 
        /// </summary>
        public (int ikey, T value) GetValue<TClip>(int ikey, TClip c = default)
            where TClip : IKeyClipper
        {
            var i = c.ClipKeyIndex(ikey, this.Values.Length, this.lengthR);

            return (i, this.Values[i]);
        }



        ///// <summary>
        ///// 
        ///// </summary>
        //public (int ikey, float time, T value) GetKeyDirect<T>(
        //    this StreamPackage<T> src, int istream, float time)
        //    where T : unmanaged
        //{
        //    var times = src.GetTimeStream(istream);

        //    var index = src.Index;
        //    var i = (times, index).GetKeyIndexDirect(istream, time);
        //    //Debug.Log($"{streamid} {section.start}:{section.length} {i} {src.KeyLinears.Keys[i]}");

        //    return src.GetKey<Direct>(istream, i);
        //    //return src.GetKey<Clamp>(istream, i);
        //}

    }


    public static class StreamManipulatorExtension
    {

        public static StreamPairManipulator<T> GetStream<T>(this StreamData<T> streams, int istream)
            where T : unmanaged
        {

            var section = streams.Sections.Sections[istream];

            var times = streams.KeyStreams.FrameTimes.Slice(section.start, section.length);
            var values = streams.KeyStreams.Values.Slice(section.start, section.length);


            return new StreamPairManipulator<T>
            {
                Times = times,
                Values = values,
                lengthR = section.lengthR,
            };
        }

    }
}