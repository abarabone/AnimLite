using System;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;

namespace AnimLite
{
    using AnimLite.Utility;



    public struct DummyStreamIndex : IStreamIndex, IDisposable
    {
        public float IndexBlockTimeRange => 0;

        public int GetKeyIndexInBlock(int istream, float time) => 0;

        public void Dispose() { }
    }



    // <stream index>
    // streams[stream length]
    //   top key indices per blocks[block unit length]

    /// <summary>
    /// �X�g���[���f�[�^�����ԋ�Ԃŋ�؂�A���Ԃňʒu��������������\�z����B
    /// </summary>
    public struct StreamIndex : IStreamIndex, IDisposable
    {

        public NativeArray<int> TopKeyIndicesPerFrameBlock;

        public int StreamLength;
        public float LastFrameTime;
        public int FrameBlockUnitLength;

        public float FrameBlockUnitRange;
        public float FrameBlockUnitRangeReciprocal;

        public void Dispose()
        {
            this.TopKeyIndicesPerFrameBlock.Dispose();
        }




        public float IndexBlockTimeRange => this.FrameBlockUnitRange;// ���O��v�����悤


        /// <summary>
        /// �u���b�N���Ƃ̐擪�L�[�C���f�b�N�X��Ԃ��B
        /// �u���b�N�͍ő厞�Ԃ𒴂��Ȃ��悤�ɂ���B
        /// </summary>
        public int GetKeyIndexInBlock(int istream, float time)
        {
            //var typeOffset = this.FrameBlockUnitLength * this.StreamLength;
            var streamOffset = istream * this.FrameBlockUnitLength;
            var blockOffset = (int)(time * this.FrameBlockUnitRangeReciprocal);

            var blockOffsetLimited = math.min(blockOffset, this.FrameBlockUnitLength);
            return this.TopKeyIndicesPerFrameBlock[streamOffset + blockOffsetLimited];
        }


        /// <summary>
        /// 
        /// </summary>
        public StreamIndex(
            NativeArray<float> frameTimes, KeyStreamSections keySectionsInStream, int blockLength)
        {

            // ���Z�b�g
            this.StreamLength = keySectionsInStream.Sections.Length;
            this.LastFrameTime = getLastFrame_();
            this.FrameBlockUnitLength = blockLength;
            this.FrameBlockUnitRange = this.LastFrameTime / blockLength;
            this.FrameBlockUnitRangeReciprocal = this.FrameBlockUnitRange > 0
                ? (float)(1.0 / this.FrameBlockUnitRange)
                : 0;// range �� 0 �̎��́AblockOffset ����� 0 �ɂȂ�悤�ɂ���

            // �u���b�N�����\�z
            this.TopKeyIndicesPerFrameBlock = buildIndex(
                frameTimes, keySectionsInStream.Sections, this.FrameBlockUnitLength, this.FrameBlockUnitRange);

            return;


            // �e�X�g���[������Ō�̃t���[����񋓂��A���̍ő���擾����B
            float getLastFrame_() => keySectionsInStream.Sections
                //.Do(x => $"{x.start} {x.length}".ShowDebugLog())
                .Where(x => x.length != 0)
                .Where(x => frameTimes.Length > x.start + x.length)//
                .Select(section => frameTimes
                    .Slice(section.start, section.length)[section.length - 1])
                //.Last())
                .DefaultIfEmpty(0)
                .Max();
        }


        /// <summary>
        /// ���ׂẴL�[��񋓂��āA���̎��ԋ�Ԃ��ƂɃC���f�b�N�X�u���b�N���\�z����B
        /// �X�g���[�����Ƃ� FrameBlockUnitLength �̃u���b�N�ɋ�؂���B
        /// �e�X�g���[���̃u���b�N��̐擪�Ɩ����́Amin value / max value �܂Ŋi�[�ł���悤�ɂ���B
        /// </summary>
        static NativeArray<int> buildIndex(
            NativeArray<float> keyFrameTimes,
            NativeArray<(int start, float lengthR, int length)> streamSections,
            int blockLength, float blockRange)
        {
            var id = 0;
            var qBlockFlatten =
                from section in streamSections//.Do(x => Debug.Log($"{(MmdBodyBones)id} {x.start} {x.length}"))
                let iii = id++
                let stream = section.length > 0
                    ? keyFrameTimes.Slice(section.start, section.length)
                    : default
                from iblock in Enumerable.Range(0, blockLength)
                let begin = (iblock + 0) * blockRange
                let end = (iblock + 1) * blockRange
                select section.length > 0
                    ? binarySearch_(iii, stream, (begin, end))
                    : 0
                ;// linq �g������ math.select() �Ƃ��̈Ӗ��Ȃ��̂ŁA�����ꃋ�[�v�ɂ��˂΂Ȃ�܂����c

            return qBlockFlatten.ToNativeArray();
            //return qBlockFlatten.Do(x => Debug.Log(x)).ToNativeArray();


            // �����l�������Ă��Ȃ����ƁB�܂��A�����ł��邱�ƁB
            // �l�����݂��Ȃ��ꍇ�́A���[�̂����ꂩ���Ԃ�B
            int binarySearch_(int iii, NativeSlice<float> times, (float begin, float end) block)
            {
                var t = times.Length >> 1;//Debug.Log($"{(MmdBodyBones)iii} {stream[t]} {t}:{stream.Length} {block.begin}-{block.end}");

                // �Ƃ肠�����Q���،���
                var i = t;
                for (; ; )
                {
                    //Debug.Log($"{i} {stream[i]}");
                    var v = times[i];

                    if (block.begin <= v && v < block.end) break;

                    t = t >> 1;
                    if (t == 0) break;//return i;
                    //Debug.Log($"{i} {v} {block.begin} {math.select(t, -t, v > block.begin)}");
                    i += math.select(t, -t, v > block.begin);

                    if (i < 0 || times.Length <= i)
                    {
                        $"index lost binary search : {i}".ShowDebugLog();
                        return math.clamp(i, 0, times.Length - 1);
                    }
                }

                // �����͕�������̂ŁA���̂����ōł��Ⴂ���̂܂ł��ǂ�B
                for (; i > 0; i--)
                {
                    var v = times[i - 1];

                    if (v < block.begin) break;
                }

                return i;
            }
        }
    }



    public static class StreamIndexExtension
    {

        public static StreamIndex CreateIndex<T>(this StreamData<T> data, int indexBlockLength)
            where T : unmanaged
        =>
            new StreamIndex(data.KeyStreams.FrameTimes, data.Sections, indexBlockLength);


        /// <summary>
        /// 
        /// </summary>
        public static int GetKeyIndex<TIndex>(
            this (NativeSlice<float> times, TIndex index) src, int istream, float time)
                where TIndex : IStreamIndex
        {
            var i = src.index.GetKeyIndexInBlock(istream, time);

            // �u���b�N�擪����i�s�����ւ��ǂ�A�t���[���^�C���𒴂��Ȃ��ő�̃L�[��T��
            for (; i < src.times.Length - 1; i++)
            {
                if (time < src.times[i + 1]) break;
            }

            return i;
        }

    }

}
