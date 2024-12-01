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
        NoRelease,      // 参照カウントが 0 になっても解放しない
        AutoRelease,    // 参照カウントが 0 になったら解放する
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



}
