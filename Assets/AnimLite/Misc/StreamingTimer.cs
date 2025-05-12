using System.Linq;
using Unity.Mathematics;

namespace AnimLite
{


    /// <summary>
    /// 
    /// </summary>
    public struct StreamingTimer
    {

        //public float _curret_time_inner;
        public float CurrentTime;

        public float TotalTime;

        //public float TotalTimeR;

        public float OffsetTime;


        public StreamingTimer(float totalTime)
        {
            //this._curret_time_inner = 0.0f;
            this.CurrentTime = 0.0f;
            
            this.TotalTime = totalTime;
            //this.TotalTimeR = 1f / totalTime;
            this.OffsetTime = 0.0f;
        }
        //public StreamingTimer(float totalTime, float delayTime)
        //{
        //    this._curret_time_inner = -delayTime;

        //    var t = math.floor(this._curret_time_inner / totalTime);
        //    this.OffsetTime = totalTime * t;
        //    this.CurrentTime = math.max(this._curret_time_inner - t, 0);

        //    this.TotalTime = totalTime;
        //    //this.TotalTimeR = 1f / totalTime;
        //}


        /// <summary>
        /// 現在の時刻を更新する。
        /// ただし、0 以上にクリッピングされる。
        /// </summary>
        public void ProceedTime(float deltaTime)
        {
            //this.CurrentTime =
            //    new TClip().ClipCurrentTime(this.CurrentTime + deltaTime, this.TotalTime, this.TotalTimeR);

            //this.UpdateTime(this._curret_time_inner + deltaTime);
            this.UpdateTime(this.CurrentTime + deltaTime);
        }

        /// <summary>
        /// 現在の時刻を更新する。
        /// ただし、0 以上にクリッピングされる。
        /// </summary>
        public void UpdateTime(float currentTime)
        {
            //this.CurrentTime =
            //    new TClip().ClipCurrentTime(currentTime, this.TotalTime, this.TotalTimeR);

            //this._curret_time_inner = currentTime;
            //this.CurrentTime = math.max(this._curret_time_inner, 0);
            this.CurrentTime = math.max(currentTime, 0);

            // 尺を超えたとき、尺をオフセットに加算していき、ループ時の計算補助に使う。
            this.OffsetTime += math.select(0, this.TotalTime, currentTime >= this.OffsetTime + this.TotalTime);
        }


        ///// <summary>
        ///// 
        ///// </summary>
        //public struct StreamingTimer : IDisposable
        //{

        //    NativeArray<float> content;


        //    public float CurrentTime
        //    {
        //        get => this.content[0];
        //        set => this.content[0] = value;
        //    }

        //    public float TotalTime;



        //    public StreamingTimer(float totalTime)
        //    {
        //        this.content = new NativeArray<float>(1, Allocator.Persistent);
        //        this.TotalTime = totalTime;
        //    }
        //    public void Dispose()
        //    {
        //        this.content.Dispose();
        //    }


        //    public void ProceedTime<TClip>(float deltaTime) where TClip : IKeyClipper, new()
        //    {
        //        this.CurrentTime =
        //            new TClip().ClipCurrentTime(this.CurrentTime + deltaTime, this.TotalTime);
        //    }

        //}
    }


    public static class TimerUtility
    {

        public static float GetLastKeyTime<T>(this StreamData<T> s) where T : unmanaged =>
            Enumerable.Range(0, s.Sections.Length)
                .Select(i => s.GetStream(i))
                .Max(x => x.Times[x.Times.Length - 1])
                ;

    }
}
