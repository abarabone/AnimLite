using System;
using Unity.Collections;

namespace AnimLite
{
    /// <summary>
    /// Burst 対応を考慮し NativeArray を使用したキーフレームデータ構造体。
    /// キーフレームはストリームとしてボーンごとにまとまっているが、
    /// すべて１つの配列に詰め込んでいる。（回転、位置、表情別）
    /// 取り出すためには、索引構造体（VmdStreamIndex）が必要となる。
    /// </summary>


    /// <summary>
    /// 回転、位置、表情、の各キーデータを詰めるための構造体。
    /// キーは時間と１対１で対応する。
    /// </summary>
    public struct KeyStreamsInOneArray<T> : IDisposable
        where T : unmanaged
    {
        public NativeArray<float> FrameTimes;
        public NativeArray<T> Values;

        public void Dispose()
        {
            this.FrameTimes.Dispose();
            this.Values.Dispose();
        }
    }


    /// <summary>
    /// ストリームごとの区間を列挙する。
    /// </summary>
    public struct KeyStreamSections : IDisposable
    {

        public NativeArray<(int start, float lengthR, int length)> Sections;


        public (int start, float lengthR, int length) this[int i]
        {
            get => this.Sections[i];
            set => this.Sections[i] = value;
        }

        public int Length => this.Sections.Length;


        public void Dispose()
        {
            this.Sections.Dispose();
        }
    }



    public static class KeyStreamExtension
    {

    }

}
