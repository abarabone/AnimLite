namespace AnimLite
{


    public interface IKeyClipper
    {
        public int ClipKeyIndex(int ikey, int length, float lengthR) => ikey;

        public float AdjustKeyTime(float time) => time;

        public void SetTimer(StreamingTimer timer) { }

        //public float ClipCurrentTime(float currentTime, float totalTime, float totalTimeR) => currentTime;

        public bool IsNextKey<TProcedure>(TProcedure cursor, float currentTime)
            where TProcedure : IKeyCursor
        =>
            currentTime >= cursor.TimeTo;
    }
    //public interface IKeyClipper
    //{
    //    int ClipIndex(int ikey, int length);

    //    float ClipCurrentTime(float currentTime, float totalTime);


    //    bool isOver<TProcedure>(TProcedure cursor, StreamingTimer timer) where TProcedure : IKeyCursor;
    //}

    //public interface ILoopKeyClipper : IKeyClipper
    //{
    //    bool IKeyClipper.isOver<TProcedure>(TProcedure cursor, StreamingTimer timer)
    //    {
    //        var limittime = cursor.TimeTo + math.select(0.0f, timer.TotalTime, cursor.TimeTo < cursor.TimeFrom);

    //        return timer.CurrentTime >= limittime;
    //    }
    //}

    public static class clipExtension
    {
        //public static float AdjustKeyTime<TClip>(this TClip c, float time)
        //    where TClip : IKeyClipper
        //=>
        //    c.AdjustKeyTime(time);

        //public static float ClipCurrentTime<TClip>(this TClip c, float time, StreamingTimer timer)
        //    where TClip : IKeyClipper
        //=>
        //    c.ClipCurrentTime(time, timer.TotalTime, timer.TotalTimeR);
    }

}
