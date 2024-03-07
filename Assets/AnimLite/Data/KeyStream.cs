using System;
using Unity.Collections;

namespace AnimLite
{
    /// <summary>
    /// Burst �Ή����l���� NativeArray ���g�p�����L�[�t���[���f�[�^�\���́B
    /// �L�[�t���[���̓X�g���[���Ƃ��ă{�[�����Ƃɂ܂Ƃ܂��Ă��邪�A
    /// ���ׂĂP�̔z��ɋl�ߍ���ł���B�i��]�A�ʒu�A�\��ʁj
    /// ���o�����߂ɂ́A�����\���́iVmdStreamIndex�j���K�v�ƂȂ�B
    /// </summary>


    /// <summary>
    /// ��]�A�ʒu�A�\��A�̊e�L�[�f�[�^���l�߂邽�߂̍\���́B
    /// �L�[�͎��ԂƂP�΂P�őΉ�����B
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
    /// �X�g���[�����Ƃ̋�Ԃ�񋓂���B
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
