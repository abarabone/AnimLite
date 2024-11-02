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

    public interface IPrototype<T> : IDisposable//, IAsyncDisposable
        where T : UnityEngine.Object
    {
        PrototypeReleaseMode Mode { get; }

        Instance<T> Instantiate();
        void ReleaseWithDestroy(T instance);

        //ValueTask<Instance<T>> InstantiateOnMainThreadAsync();
        //ValueTask ReleaseOnMainThreadAsync(T instance, PrototypeReleaseMode mode = PrototypeReleaseMode.ReleaseWhenZero);
    }




    // キャッシュをつかう場合
    // ・ひな型を保持しつつ、同じ url ならインスタンスを作成
    // ・ひな型は明示的に破棄するまで残る

    // つかわない場合
    // ・ひな型とインスタンスを１対１で保持し、破棄する
    // ・インスタンスだけ保持して破棄すれば、自動でひな型も破棄される

    // モデル
    // ・インスタンスごとに複製
    // audio clip
    // ・複製しない
    // ・www load は destroy、resource は release
    // animaton clip
    // ・複製しない
    // ・resource のみ
    // anim stream
    // ・ストリームデータにビルドして保持
    // ・一部ワーク生成して、浅いコピーを作成


    // ・複製時にマネージャーに登録していき、破棄するか否かの判定時にループで .AsUnityNull() する
    // 　・自由に Destroy() できる
    // 　・都度の release チェックは無理
    // 　・unityobject でなければできない

    // ・破棄用オブジェクトでラップする
    //   ・とりまわしが悪い

    // ・gameobject に破棄用のスクリプトを仕込む
    //   ・手を入れるのはどうなのか
    // 　・clip には無理・

    public enum PrototypeReleaseMode
    {
        NoRelease,
        AutoRelease,
    }


    public class Instance<T> : IDisposable//, IAsyncDisposable
        where T : UnityEngine.Object
    {
        public Instance(T instance, IPrototype<T> prototype)
        {
            this.Value = instance;
            this.Prototype = prototype;
        }

        public T Value { private set; get; }

        public IPrototype<T> Prototype { set; private get; }


        public void Dispose() =>
            this.Prototype.ReleaseWithDestroy(this.Value);


        public static implicit operator T(Instance<T> src) => src.Value;
    }

    public static class InstanceExtension
    {

        public static bool IsUnityNull<T>(this Instance<T> obj)
            where T : UnityEngine.Object
        {
            return obj?.Value.IsUnityNull() ?? true;
        }

        public static T AsUnityNull<T>(this Instance<T> obj)
            where T : UnityEngine.Object
        {
            return obj?.Value.AsUnityNull();
        }

        public static T AsUnityNull<T>(this Instance<T> obj, Func<T, bool?> criteria)
            where T : UnityEngine.Object
        {
            var _obj = obj?.Value.AsUnityNull();
            return criteria(_obj) ?? false
                ? _obj
                : null;
        }
    }







    public static class ModelPrototypeExtension
    {


        public static async ValueTask<Instance<GameObject>> LoadModelInstanceAsync(
            this IArchive archive, PathUnit path, PrototypeReleaseMode mode, CancellationToken ct)
        {
            var prototype = await archive.LoadModelPrototypeAsync(path, mode, ct);

            await Awaitable.MainThreadAsync();
            return prototype.Instantiate();
        }

        public static async ValueTask<IPrototype<GameObject>> LoadModelPrototypeAsync(
            this IArchive archive, PathUnit path, PrototypeReleaseMode mode, CancellationToken ct)
        {
            var model = await archive.LoadModelExAsync(path, ct);
            if (model.IsUnityNull()) return null;

            return path.IsResource() switch
            {
                true =>
                    new Resource<GameObject>(model, mode),
                false =>
                    new ModelOrigin(model, mode),
            };
        }


        public static async ValueTask<IPrototype<AnimationClip>> LoadAnimationClipAsync(
            this ResourceName name, PrototypeReleaseMode mode, CancellationToken ct)
        {
            var clip = await name.loadAnimationClipFromResourceAsync(ct);
            if (clip.IsUnityNull()) return null;

            return new Resource<AnimationClip>(clip, mode);
        }


    }








    /// <summary>
    /// ・prototype にリソースを保持
    /// </summary>
    public class Resource<T> : IPrototype<T>
        where T : UnityEngine.Object
    {
        public Resource(T prototype)
        {
            this.prototype = prototype;
        }
        public Resource(T prototype, PrototypeReleaseMode mode)
        {
            this.prototype = prototype;
            this.Mode = mode;
        }


        public PrototypeReleaseMode Mode { get; } = PrototypeReleaseMode.AutoRelease;

        T prototype = null;

        int refCount = 0;


        public Instance<T> Instantiate()
        {
            var instance = UnityEngine.Object.Instantiate(this.prototype);

            Interlocked.Increment(ref this.refCount);
            return new Instance<T>(instance, this);
        }

        public void ReleaseWithDestroy(T instance)
        {
            instance.Destroy();

            var inow = Interlocked.Decrement(ref this.refCount);

            switch (this.Mode)
            {
                case PrototypeReleaseMode.AutoRelease:
                    if (inow > 0) break;
                    this.Dispose();
                    break;

                case PrototypeReleaseMode.NoRelease:
                    break;
            }
        }

        public void Dispose()
        {
            if (this.prototype == null) return;

            this.prototype.Release();
            this.prototype = null;

            "Dispose ModelResource".ShowDebugLog();
        }
    }




    /// <summary>
    /// 取得
    /// ・最初にインスタンスを１つロードし、非アクティブ化する
    /// ・そのインスタンスを本体とし、prototype とする
    /// ・prototype が空いていればインスタンスとして使用する
    /// ・prototype が使用中なら複製を渡す
    /// 破棄
    /// ・複製なら destory
    /// ・prototype なら非アクティブ化
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


        public Instance<GameObject> Instantiate()
        {
            var inow = Interlocked.Increment(ref this.refCount);

            var prevProtoUsed = Interlocked.CompareExchange(ref this.prototypeUsed, 1, 0);

            var go = prevProtoUsed == 0
                ? this.prototype
                : GameObject.Instantiate(this.prototype);

            return new Instance<GameObject>(go, this);
        }

        public void ReleaseWithDestroy(GameObject go)
        {
            var inow = Interlocked.Decrement(ref this.refCount);

            if (!go.IsUnityNull())
            {
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
                    this.Dispose();
                    break;

                case PrototypeReleaseMode.NoRelease:
                    break;
            }
        }

        public void Dispose()
        {
            if (this.prototype == null) return;

            this.prototype.Destroy();
            this.prototype = null;

            "Dispose ModelOrigin".ShowDebugLog();
        }
    }


    /// <summary>
    /// 取得
    /// ・最初にインスタンスを１つロードする
    /// ・１つしかない場合は prototype を渡す
    /// ・２つ目からは複製を渡す
    /// ・prototype は list としてインスタンスをすべて保持
    /// ・prototype の破棄されていないインスタンスを渡す/複製する
    /// 破棄
    /// ・インスタンスが常に１つ残るようにする
    /// ・残りが１つなら destroy しない
    /// </summary>
    public class ModelLastOne : IPrototype<GameObject>
    {
        public ModelLastOne(GameObject prototype)
        {
            this.instances.Add(prototype);
        }
        public ModelLastOne(GameObject prototype, PrototypeReleaseMode mode)
        {
            this.instances.Add(prototype);
            this.Mode = mode;
        }


        public PrototypeReleaseMode Mode { get; } = PrototypeReleaseMode.AutoRelease;

        int refCount = 0;

        ConcurrentBag<GameObject> instances = new();


        public Instance<GameObject> Instantiate()
        {
            var inow = Interlocked.Increment(ref this.refCount);

            var go = inow > 1
                ? instantiate_(getPrototype_())
                : getPrototype_();

            //go.SetActive(true);

            return new Instance<GameObject>(go, this);


            GameObject instantiate_(GameObject prototype)
            {
                var go = GameObject.Instantiate(prototype);

                this.instances.Add(go);

                return go;
            }

            GameObject getPrototype_()
            {
                foreach (var go in this.instances)
                {
                    if (!go.IsUnityNull()) return go;
                }
                return null;
            }
        }

        public void ReleaseWithDestroy(GameObject go)
        {
            var inow = Interlocked.Decrement(ref this.refCount);

            if (inow > 0)
            {
                go.Destroy();
                return;
            }

            switch (this.Mode)
            {
                case PrototypeReleaseMode.AutoRelease:
                    //go.Destroy();
                    this.Dispose();
                    break;

                case PrototypeReleaseMode.NoRelease:
                    {
                        if (go.IsUnityNull()) break;
                        go.SetActive(false);

                        var anim = go.GetComponent<Animator>().AsUnityNull();
                        anim?.UnbindAllStreamHandles();
                        anim?.ResetPose();
                    }
                    break;
            }
        }

        public void Dispose()
        {
            if (this.instances == null) return;

            this.instances.ForEach(x =>
            {
                x.AsUnityNull()?.Destroy();
            });
            this.instances = null;

            "Dispose ModelLastOne".ShowDebugLog();
        }
    }




}
