using Unity.Mathematics;
//using System.Diagnostics;
//using UnityEngine.WSA;

namespace AnimLite
{



    public struct Direct : IKeyClipper
    {

    }


    public struct Clamp : IKeyClipper
    {

        public int ClipKeyIndex(int ikey, int length, float lengthR) => math.clamp(ikey, 0, length - 1);


        //public float ClipCurrentTime(float currentTime, float totalTime, float totalTimeR) => math.clamp(currentTime, 0, totalTime);


        public bool IsNextKey<TCursor>(TCursor cursor, float currentTime)
            where TCursor : IKeyCursor
        {
            var limittime = math.select(cursor.TimeTo, float.PositiveInfinity, cursor.TimeTo == cursor.TimeFrom);

            return currentTime >= limittime;
        }


        // -------
        public float AdjustKeyTime(float time) => time;

        public void SetTimer(StreamingTimer timer) { }
        // -------
    }


    public struct Loop : IKeyClipper
    {

        int freq;// 全長よりもストリーム長が短かった場合に、先頭キーの時間を次ループとして補正するために必要

        float offsetTime;
        float totalTime;

        public void SetTimer(StreamingTimer timer)
        {
            this.freq = 0;
            this.offsetTime = timer.OffsetTime;
            this.totalTime = timer.TotalTime;
        }


        public int ClipKeyIndex(int ikey, int length, float lengthR)
        {
            this.freq = (int)math.floor(ikey * lengthR);

            return ikey - length * this.freq;
        }

        //public float ClipCurrentTime(float currentTime, float totalTime, float totalTimeR)// => currentTime % totalTime;
        //{
        //    return currentTime;// - totalTime * math.floor(currentTime * totalTimeR);
        //}

        public float AdjustKeyTime(float time)
        {
            return time + math.sign(this.freq) * this.totalTime + this.offsetTime;
        }

        //bool IKeyClipper.isOver<TProcedure>(TProcedure cursor, StreamingTimer timer)
        //{
        //    var limittime = cursor.TimeTo + math.select(0.0f, timer.TotalTime, cursor.TimeTo < cursor.TimeFrom);

        //    return timer.CurrentTime >= limittime;
        //}
        //public bool isOver<TProcedure>(TProcedure locator, StreamingTimer timer)
        //    where TProcedure : IKeyLocator
        //{
        //    var limittime = math.select(locator.TimeTo, float.PositiveInfinity, locator.TimeTo == locator.TimeFrom);

        //    var offset = timer.TotalTime * math.floor(timer.CurrentTime * timer.TotalTimeR);

        //    return timer.CurrentTime >= limittime + offset;
        //}
    }

    //public struct Loop : ILoopKeyClipper
    //{

    //    public int ClipIndex(int ikey, int length) => ikey % length;

    //    public float ClipCurrentTime(float currentTime, float totalTime) => currentTime % totalTime;

    //    //public bool isOver<TProcedure>(TProcedure cursor, StreamingTimer timer)
    //    //    where TProcedure : IKeyCursor
    //    //{
    //    //    var limittime = cursor.TimeTo + math.select(0.0f, timer.TotalTime, cursor.TimeTo < cursor.TimeFrom);

    //    //    return timer.CurrentTime >= limittime;
    //    //}
    //}

    //public struct LoopLit : ILoopKeyClipper
    //{
    //    public int ClipIndex(int ikey, int length)
    //    {
    //        while (ikey > length)
    //        {
    //            ikey -= length;
    //        }

    //        return ikey;
    //    }

    //    public float ClipCurrentTime(float currentTime, float totalTime)
    //    {
    //        do
    //        {
    //            currentTime -= math.select(0, totalTime, currentTime >= totalTime);
    //            currentTime += math.select(0, totalTime, currentTime < 0);
    //        }
    //        while (currentTime < 0 | totalTime <= currentTime);

    //        return currentTime;
    //    }

    //    //public bool isOver<TProcedure>(TProcedure cursor, StreamingTimer timer)
    //    //    where TProcedure : IKeyCursor
    //    //{
    //    //    var limittime = cursor.TimeTo + math.select(0.0f, timer.TotalTime, cursor.TimeTo < cursor.TimeFrom);

    //    //    return timer.CurrentTime >= limittime;
    //    //}
    //}






    public enum KeyClipType
    {
        none,
        direct,
        clamp,
        loop,
    }

    //public struct XClip : IKeyClipper, ILoopKeyClipper
    //{

    //    public KeyClipType mode;


    //    public int ClipIndex(int ikey, int length)
    //    =>
    //        this.mode switch
    //        {
    //            KeyClipType.direct => ((IKeyClipper)this).ClipIndex(ikey, length),
    //            KeyClipType.clamp => new Clamp().ClipIndex(ikey, length),
    //            KeyClipType.loop => new Loop().ClipIndex(ikey, length),
    //            KeyClipType.looplit => new LoopLit().ClipIndex(ikey, length),
    //            _ => default,
    //        };


    //    public float ClipCurrentTime(float currentTime, float totalTime)
    //    =>
    //        this.mode switch
    //        {
    //            KeyClipType.direct => ((IKeyClipper)this).ClipCurrentTime(currentTime, totalTime),
    //            KeyClipType.clamp => ((IKeyClipper)this).ClipCurrentTime(currentTime, totalTime),
    //            KeyClipType.loop => new Loop().ClipCurrentTime(currentTime, totalTime),
    //            KeyClipType.looplit => new LoopLit().ClipCurrentTime(currentTime, totalTime),
    //            _ => default,
    //        };

    //    public bool isOver<TProcedure>(TProcedure cursor, StreamingTimer timer)
    //        where TProcedure : IKeyLocator
    //        =>
    //            this.mode switch
    //            {
    //                KeyClipType.direct => ((IKeyClipper)this).isOver(cursor, timer),
    //                KeyClipType.clamp => new Clamp().isOver(cursor, timer),
    //                KeyClipType.loop => ((ILoopKeyClipper)this).isOver(cursor, timer),
    //                KeyClipType.looplit => ((ILoopKeyClipper)this).isOver(cursor, timer),
    //                _ => default,
    //            };
    //    //{
    //    //    var a = this.isOver(cursor, timer);
    //    //    var b = new Clamp().
    //    //}

    //    static public XClip Direct => new XClip { mode = KeyClipType.direct };
    //    static public XClip Clamp => new XClip { mode = KeyClipType.clamp };
    //    static public XClip Loop => new XClip { mode = KeyClipType.loop };
    //    static public XClip LoopLit => new XClip { mode = KeyClipType.looplit };
    //}

}
