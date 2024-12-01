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


    /// <summary>
    /// �擾
    /// �E�ŏ��ɃC���X�^���X���P���[�h���A��A�N�e�B�u������
    /// �E���̃C���X�^���X��{�̂Ƃ��Aprototype �Ƃ���
    /// �Eprototype ���󂢂Ă���΃C���X�^���X�Ƃ��Ďg�p����
    /// �Eprototype ���g�p���Ȃ畡����n��
    /// �j��
    /// �E�����Ȃ� destory
    /// �Eprototype �Ȃ��A�N�e�B�u��
    /// </summary>
    public class ModelOrigin : IPrototype<GameObject>
    {
        public ModelOrigin(GameObject prototype)
        {
            this.prototype = prototype;
        }
        public ModelOrigin(GameObject prototype, PrototypeReleaseMode mode)
        {
            this.prototype = prototype;
            this.Mode = mode;
        }


        public PrototypeReleaseMode Mode { get; } = PrototypeReleaseMode.AutoRelease;

        int refCount = 0;
        int prototypeUsed = 0;

        GameObject prototype;


        public async ValueTask<Instance<GameObject>> InstantiateAsync()
        {
            var inow = Interlocked.Increment(ref this.refCount);

            var prevProtoUsed = Interlocked.CompareExchange(ref this.prototypeUsed, 1, 0);

            var go = prevProtoUsed == 0
                ? this.prototype
                : await instantateAsync_();

            return new Instance<GameObject>(go, this);

            async ValueTask<GameObject> instantateAsync_()
            {
                await Awaitable.MainThreadAsync();
                return( await GameObject.InstantiateAsync(this.prototype))[0];
            }
        }

        public async ValueTask ReleaseWithDestroyAsync(GameObject go)
        {
            var inow = Interlocked.Decrement(ref this.refCount);

            if (!go.IsUnityNull())
            {
                await Awaitable.MainThreadAsync();

                if (go == this.prototype)
                {
                    go.SetActive(false);

                    var anim = go.GetComponent<Animator>().AsUnityNull();
                    anim?.UnbindAllStreamHandles();
                    anim?.ResetPose();

                    Interlocked.CompareExchange(ref this.prototypeUsed, 0, 1);
                }
                else
                {
                    go.Destroy();
                }
            }

            if (inow > 0) return;


            switch (this.Mode)
            {
                case PrototypeReleaseMode.AutoRelease:
                    await this.DisposeAsync();
                    break;

                case PrototypeReleaseMode.NoRelease:
                    break;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (this.prototype == null) return;

            await this.prototype.DestroyOnMainThreadAsync();
            this.prototype = null;

            "Dispose async ModelOrigin".ShowDebugLog();
        }
        //public void Dispose()
        //{
        //    if (this.prototype == null) return;

        //    this.prototype.Destroy();
        //    this.prototype = null;

        //    "Dispose ModelOrigin".ShowDebugLog();
        //}
    }


}
