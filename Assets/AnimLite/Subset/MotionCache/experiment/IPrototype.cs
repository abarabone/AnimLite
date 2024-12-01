using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.VisualScripting;
using System.IO;
using System.Collections.Concurrent;
using UnityEngine.Networking;
using UniVRM10;
using UnityEngine.AddressableAssets;
using System.Net.Http;
using System.IO.Compression;
using UnityEngine.Animations;

namespace AnimLite.Utility
{

    public interface IPrototype<T> : IAsyncDisposable
        where T : UnityEngine.Object
    {
        PrototypeReleaseMode Mode { get; }

        ValueTask<Instance<T>> InstantiateAsync();
        ValueTask ReleaseWithDestroyAsync(T instance);

        //ValueTask<Instance<T>> InstantiateOnMainThreadAsync();
        //ValueTask ReleaseOnMainThreadAsync(T instance, PrototypeReleaseMode mode = PrototypeReleaseMode.ReleaseWhenZero);
    }

    public enum PrototypeReleaseMode
    {
        NoRelease,      // �Q�ƃJ�E���g�� 0 �ɂȂ��Ă�������Ȃ�
        AutoRelease,    // �Q�ƃJ�E���g�� 0 �ɂȂ�����������
    }





    // �L���b�V���������ꍇ
    // �E�ЂȌ^��ێ����A���� url �Ȃ�C���X�^���X���쐬
    // �E�ЂȌ^�͖����I�ɔj������܂Ŏc��

    // ����Ȃ��ꍇ
    // �E�ЂȌ^�ƃC���X�^���X���P�΂P�ŕێ����A�j������
    // �E�C���X�^���X�����ێ����Ĕj������΁A�����łЂȌ^���j�������

    // ���f��
    // �E�C���X�^���X���Ƃɕ���
    // audio clip
    // �E�������Ȃ�
    // �Ewww load �� destroy�Aresource �� release
    // animaton clip
    // �E�������Ȃ�
    // �Eresource �̂�
    // anim stream
    // �E�X�g���[���f�[�^�Ƀr���h���ĕێ�
    // �E�ꕔ���[�N�������āA�󂢃R�s�[���쐬


    // �E�������Ƀ}�l�[�W���[�ɓo�^���Ă����A�j�����邩�ۂ��̔��莞�Ƀ��[�v�� .AsUnityNull() ����
    // �@�E���R�� Destroy() �ł���
    // �@�E�s�x�� release �`�F�b�N�͖���
    // �@�Eunityobject �łȂ���΂ł��Ȃ�

    // �E�j���p�I�u�W�F�N�g�Ń��b�v����
    //   �E�Ƃ�܂킵������

    // �Egameobject �ɔj���p�̃X�N���v�g���d����
    //   �E�������̂͂ǂ��Ȃ̂�
    // �@�Eclip �ɂ͖����E



}
