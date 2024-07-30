# AnimLite
- unity で .vmd を再生するライブラリ
- 現状は humanoid 向け
- 表情はとりあえず VRM 1.0 の expression に依存
- burst, playable にも対応した
- 名前空間 AnimLite.Vrm 関係は、UniVRM 1.x が必要です
- 裏スレッドでの .vmd 読み込み
- .vmd のキャッシュ機構、データ共有機構
- .json で音楽、モデル、アニメーション、配置、を設定する機能
- local, web から file, zip をロード、また resource(addressable) もロードできるようにした　<- new

# いずれ
- 表面的な最適化もやらねば
  - burst 関数の引数に ref/in とか [NoArias] とか
  - aggressive inlining つけたり
- 汎用のアニメーションライブラリにしたい
  - 複数アニメーションの所持、切り替えに対応させる
- ecs でも使えるようにしたい
- body motion が burst job なのに、face の playable script がメインスレッドなうえに遅い、なんとかならないか…
  - 表情は univrm ではない再生方法もあるといいかも
- .vmd 変換を高速化したい

# なやみちゅう
- 非同期と並列うまく扱えてない感　特に unity の Awaitable
- 非同期とバックグラウンドスレッドを分けた関数にしようかな  
  ~~- xxxAsync() と xxxBg() とか？~~  
  -> 基本的に async は非同期であって並列化ではないようにした  
     （並列化は Task.Run() や Awaitable.MainThreadAsync() などでやる）

# その他
- unity 2023.1.19f1, VRM1.20
- テストコードは書いてない（よくわからない）、サンプルシーンが動けばとりあえずいいかみたいな
- とりあえず ~~quest2~~ quest3（買った！！）で遊びながら修正していきたい

# .json について
- Sample5 load from json シーンにサンプルがある
- ＶＲＭモデルが踊るのをもっと気軽に見たいので、なんかこういうフォーマットが世間に１つあるといいなと思う
- zip を読めるようにしたし「固めてアップロードしたので好きなビューワで見て」みたいな世界がくるといいなーとか思うし
- 楽曲、モデル、アニメーション、の作者情報をビューワーが積極的に表示するようにすれば、作者さんも公開しようという気持ちになるんじゃないかなと願いつつ
```
{
    "Audio": {
        "AudioFilePath": "",
        "Volume": 0.0,
        "DelayTime": 0.0
    },
    "DefaultAnimation": {                         // デフォルトのアニメーション（現在は機能していない）
        "AnimationFilePath": "",
        "FaceMappingFilePath": "",
        "DelayTime": 0.0
    },
    "AudioInformation": {                         // 音楽の情報
        "Caption": "",                            // 曲名のキャプションとして表示される
        "Author": "",                             // 音楽の作者として表示される
        "Url": "",
        "Description": ""
    },
    "AnimationInformation": {                     // デフォルトアニメーションの情報（現在は機能していない）
        "Caption": "",                            // アニメーション名として表示される
        "Author": "",                             // アニメーションの作者として表示される
        "Url": "",
        "Description": ""
    },
    "Motions": [                                  // キャラクターのモデル、アニメーション、を配列で定義
        {
            "Model": {
                "ModelFilePath": "",
                "UsePositionAndDirection": true,  // true なら、position, euler angles をキャラクーの位置に反映する
                "Position": {
                    "x": 0.0,
                    "y": 0.0,
                    "z": 0.0
                },
                "EulerAngles": {
                    "x": 0.0,
                    "y": 0.0,
                    "z": 0.0
                }
            },
            "Animation": {                        // キャラクターのアニメーション
                "AnimationFilePath": "",
                "FaceMappingFilePath": "",        // .vrm と .vmd の表情名対応表へのパス。"" ならデフォルトの対応表が使用される
                "DelayTime": 0.0
            },
            "Options": {
                "BodyScaleFromHuman": 0.0,        // unity humanoid の標準からのスケール。0.0 なら自動的に計算される
                "FootIkMode": "auto"              // .vmd のフットＩＫをどうするか。auto|on|off から選ぶ。auto は .vmd の足ＩＫにキーがあるかないかで自動判別する
            },
            "ModelInformation": {                 // キャラクターモデルの情報
                "Caption": "",                    // キャラクター名として表示される
                "Author": "",                     // 作者情報として表示される
                "Url": "",
                "Description": ""
            },
            "AnimationInformation": {             // キャラクターアニメーションの情報
                "Caption": "",                    // アニメーション名として表示される
                "Author": "",                     // アニメーションの作者として表示される
                "Url": "",
                "Description": ""
            }
        }
    ]
}
```
- パスの形式
  - 相対パス指定： ds/step1.vmd
  - 絶対パス指定： c:/xxxx/ds/step1.vmd
  - web から： https://github.com/abarabone/AnimLite/raw/master/Asset/ds/step1.vmd
  - zip を指定： https://github.com/abarabone/AnimLite/raw/master/ds-sjis.zip
  - zip 内のパスを指定： https://github.com/abarabone/AnimLite/raw/master/ds-sjis.zip/ds/step1.vmd
  - drop box： https://www.dropbox.com/xxxx/step1.vmd?rlkey=kfga3v1soo6sple638gk326qt&st=hrqrzch6&dl=1 ← 末尾を dl=1 にすればよいみたい
  - unity のリソースは末尾に as resource をつける： step1 as resource
- 現状、zip だとマルチスレッドロードが利かない（非同期ではあるが）のでなんとかしたい
- いちおう utf8 と sjis の zip に対応しているつもり（ win の送るで作った zip はなんと shift-jis で内部パスが保存される恐ろしい仕様らしい）
- 相対パスは、下記のように絶対パスに変換される
  - FullPathMode.PersistentDataPath => Application.persistentDataPath + /ds/step1.vmd
  - FullPathMode.DataPath => Application.dataPath + /ds/step1.vmd
  - セキュリティ的にアレなので、上記位置より深いフォルダにアクセスできないモードも作る予定
- json なのでいろいろ省略しても読める
  - ただしまだエラー処理とかテストとかしてないので、いろいろエラーも出ると思う
  - 各パスとキャラ位置さえ指定しておけば、あとは初期値（または省略）でもなんとなくOkだと思う
