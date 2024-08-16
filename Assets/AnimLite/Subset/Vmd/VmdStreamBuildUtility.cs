using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

namespace AnimLite.Vmd
{
    using AnimLite;
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;


    /// <summary>
    /// �u�l�c����ǂ݉������f�[�^���A�L�[�X�g���[���f�[�^�ɐ��`����B
    /// �L�[�X�g���[���f�[�^�́A�S�X�g���[���i�{�f�B�̑S���ʁj���P�܂Ƃ߂ɂ����z��ƁA
    /// �X�g���[���i���ʁj���Ƃ͈̔͂��i�[�����z��̂Q�̃f�[�^����Ȃ�B
    /// </summary>
    public static class VmdStreamBuildExtension
    {


        /// <summary>
        /// 
        /// </summary>
        public static KeyStreamSections BuildSectionData<TKey>(
            this IEnumerable<IEnumerable<TKey>> streamsList, TKey defaultKey = default)
        {
            var qStreamsList = streamsList
                .Append(defaultKey.WrapEnumerable());

            var streamKeyCounts = qStreamsList//.Do((x, i) => Debug.Log($"{(MmdBodyBones)i} {x.Count()}"))
                .Select(x => x.Count())
                .ToArray();

            var iLastItem = streamKeyCounts.Sum() - 1;
            var qStreamKeySection = Enumerable.Zip(
                    streamKeyCounts.Prepend(0).Scan(0, (pre, cur) => pre + cur),
                    streamKeyCounts,
                    //(start, length) => (start, length)
                    (start, length) => length != 0
                        ? (start, 1f / length, length)
                        : (iLastItem, 1f, 1)
                );

            return new KeyStreamSections
            {
                Sections = qStreamKeySection
                    //.Do(x => Debug.Log($":{x}"))
                    .ToNativeArray(),
                //Sections = qVmdBoneSection.ToNativeArray(),
            };
        }


        /// <summary>
        /// 
        /// </summary>
        public static KeyStreamsInOneArray<T> BuildKeyData<TKey, T>(
            this IEnumerable<IEnumerable<TKey>> streamsList, Func<TKey, T> keySelector, Func<TKey, float> timeSelector, TKey defaultKey = default)
            where T : unmanaged
        {
            var qStreamsList = streamsList
                .Append(defaultKey.WrapEnumerable());


            var qFrameTime =
                from stream in qStreamsList
                from key in stream
                select timeSelector(key)
                ;

            var qKey =
                from stream in qStreamsList
                from key in stream
                select keySelector(key)
                ;

            return new KeyStreamsInOneArray<T>
            {
                FrameTimes = qFrameTime.ToNativeArray(),
                Values = qKey.ToNativeArray(),
            };
        }
    }

}
