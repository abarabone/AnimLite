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


}
