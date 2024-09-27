# AnimLite
- unity で .vmd を再生するライブラリ
- 現状は humanoid 向け
- 表情はとりあえず VRM 1.0 の expression に依存
- burst, playable にも対応した
- 名前空間 AnimLite.Vrm 関係は、UniVRM 1.x が必要です
- 裏スレッドでの .vmd 読み込み
- .vmd, model, web loading のキャッシュ機構、データ共有機構
- .json で音楽、モデル、アニメーション、配置、を設定する機能

# 新機能・修正
2024.9.19
- .json 読込をオーバーライドできるようにした（Sample6）
  - オーバーライド分の .json に記述のある項目だけ上書きされ、あとはベースの値が残る
  - キャラだけ変えたり、モーションだけ変えたりできる
  - キャラや背景の定義を、配列からキーバリューペアにした。同じ名前を付けた内容についてオーバーライドされる
- 表情を catmul 補間にもどしてみた
  - 目がちゃんと閉じないときがあるなぁ
    
それ以前の修正
- local, web から file, zip をロード、また resource(addressable) もロードできるようにした
- .glb を読めるようにした(BackGrounds)
- シーン上の game object インスタンスのキャッシュ（同じパスのモデルを再ロードしなくてもよいように）
- ロードの並列度を高めた（同じ .json に出てくる音楽、モデル、アニメーション、表情定義、のどれもが並列になるようにした）
- ZipArchive のロード時、ファイルストリームを使いまわさずに別個にファイルをオープンし、並列ロードできるスイッチを追加した
- ZipArchive の時に .glb が読めていなかったのを修正
- ZipArchive の vmd ロードで MemorryStream にコピーしていたのを解消
- .vmd キャッシュの参照カウントがロック対応されてなかったので、interlocked を使うようにした
- .vrm から作成したモデルゲームオブジェクトを、シーン上でキャッシュしておく機能を追加（キャッシュ最大数の目安を指定できる）
  - モデルロードはふつうに速いからあんまり意味ない気がする、メモリ無駄になるだけで余計な機能かなぁ
- データロードに１つでも失敗するとエラーで中断していたが、続行するようにした
- http での .zip ロードで、クエリストリングの ? 以降がある場合、機能していなかったので修正
  ~~- ただし、https://.../ds.zip?dl=1/dance_scene.json のような記述はまだ無理（というか書き方どうしようか悩み中）~~
- http のクエリストリング + .zip entry の記述を可能にした
  - xxx/xxx.zip?xxx=xxx&xxx=xxx/xxx/xxx.xxx 形式
- http ロードしたファイルをキャッシュするようにした。
  - キャッシュファイルは Application.CashDataPath/loadcach フォルダ
  - 任意のタイミングでクリアできるが、必ずプログラム開始時にクリアされる

# やりたい・検討中
- キャラごとにボーンのオフセット値を設定できるようにしたいかも（Ｔポーズ→Ａポーズもこれでやってもいい）
  - 回転制限か比率をつけるのもいいかなぁ、特に肩がモデルによって違うからな…
  - しゃがみこんだ時すごい潜るのとか改善できないかな
- 足ＩＫとつま先ＩＫ（向き）のオンオフを分けて .json に記載できるといいかも
- ~~http 時の .zip で クエリストリングの ? 以降と ZipArchive エントリの指定に対応させたい（が、http での .zip はあまり実用性ない気もする…）~~
  → xxx/xxx.zip?xxx=xxx&xxx=xxx/xxx/xxx.xxx の形式で記載できるようにした
- .zip の並列スイッチは、ZipArchive のエントリの時に機能しないことに気づいたので、対応したい
  → 対応した、別のファイルとして扱って並列ロード可能とした
- 同じパスのモデルをキャッシュする際、同じモデルを複数体同時に表示すると最後のモデルしか表示されない
- DanceSetSimpleCaption において、キャラが多いとテロップが意味不明になるので、複数回に分けるとかスクロールするとか対処したい
- ~~WebGL でサンプル作ってどっかに置きたい~~　<- webgl だとスレッド使えないらしいのでダメかも
- DanceSet を DanceScene にリネームしたいかも
- 現状 DanceSet の機構が新旧２重になっているので統合したい（エディタ上でオーサリングするタイプと .json しか考慮してないタイプ）
- .vmd の肩、腕ねじり、手首ねじり、を再考したい
- 体と表情が別 .vmd になっている場合に対応したいので、複数 .vmd をマージするようにしたいかも
- アニメーションとモデルがキャッシュされるので、音楽もキャッシュされるべき
- 顔と体の bounding box が変なので修正したい

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
- unity ~~2023.1.19f1~~ 6 prevew にしちゃった… + VRM1.20
- テストコードは書いてない（よくわからない）、サンプルシーンが動けばとりあえずいいかみたいな
- とりあえず ~~quest2~~ quest3（買った！！）で遊びながら修正していきたい
  → けっこう機能が充実してきたので quest3 ライフがよきよき. deep link でブラウザから起動できるようにしたいがわからぬ

# .json について
- Sample5 load from json シーンにサンプルがある
- ＶＲＭモデルが踊るのをもっと気軽に見たいので、なんかこういうフォーマットが世間に１つあるといいなと思う
  - よく考えたら vrm live viewer のフォーマットとかがこれに当たるんじゃないのか
- zip を読めるようにしたし「固めてアップロードしたので好きなビューワで見て」みたいな世界がくるといいなーとか思うし
- 楽曲、モデル、アニメーション、の作者情報をビューワーが積極的に表示するようにすれば、作者さんも公開しようという気持ちになるんじゃないかなと願いつつ
- DanceSetSimpleCaption はとりあえずシンプルな作者情報を表示するテロップ
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
    "BackGrounds": {                              // 背景用モデルをキーバリューペアで定義（ .glb を想定）
      "key-name": {
        "ModelFilePath": "",
        "Position": {
          "x": 0.0,
          "y": 0.0,
          "z": 0.0
        },
        "EulerAngles": {
          "x": 0.0,
          "y": 0.0,
          "z": 0.0
        },
        "Scale": 0.0
      }
    },
    "Motions": {                                  // キャラクターのモデル、アニメーション、をキーバリューペアで定義
        "key-name": {
            "Model": {
                "ModelFilePath": "",
                "Position": {
                    "x": 0.0,
                    "y": 0.0,
                    "z": 0.0
                },
                "EulerAngles": {
                    "x": 0.0,
                    "y": 0.0,
                    "z": 0.0
                },
                "Scale": 0.0
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
    }
}
```
- パスの形式
  - 相対パス指定： ds/step1.vmd
  - 絶対パス指定： c:/xxxx/ds/step1.vmd
  - web から： https://github.com/abarabone/AnimLite/raw/master/Asset/ds/step1.vmd
  - zip を指定： https://github.com/abarabone/AnimLite/raw/master/ds-sjis.zip
  - zip 内のパスを指定： https://github.com/abarabone/AnimLite/raw/master/ds-sjis.zip/ds/step1.vmd
  - drop box： https://www.dropbox.com/xxxx/step1.vmd?rlkey=kfga3v1soo6sple638gk326qt\&st=hrqrzch6\&dl=1 ← 末尾を dl=1 にすればよいみたい
    - クエリストリング中の & は、quest3(android?) だと \& のようにエスケープしないとダメだったので注意
    - one drive とか google のマイドライブとかだとパスに拡張子が含まれないので、content の type とか見る方法にしないとダメかも…
    - https://www.dropbox.com/xxxx/step1.vmd?rlkey=kfga3v1soo6sple638gk326qt\&st=hrqrzch6\&dl=1/ds/step2.vmd のような記述（クエリストリングの後に zip 内のパス指定）もOK
  - unity のリソースは末尾に as resource をつける： step1 as resource
- 単体 zip に固めたデータは、同じ ZipArchive を使いまわすのでマルチスレッドロードが利かない（非同期ではあるが）
  - ただし SceneLoadUtilitiy.IsSeaquentialLoadingInZip が false の時には、同じ zip を複数開いて並列にロードする
  - デフォルトは false
  - ~~マルチスレッドロードは機能していないことに気づいた。ZipArchive のエントリが相対パスの場合、ローカルを見に行ってしまう…~~ ← 修正対応した（つもり）
- いちおう utf8 と sjis の zip に対応しているつもり（ win の送るで作った zip はなんと shift-jis で内部パスが保存される仕様らしい）
- 相対パスは、下記のように絶対パスに変換してロードする
  - FullPathMode.PersistentDataPath の時は Application.persistentDataPath + /ds/step1.vmd
  - FullPathMode.DataPath の時は Application.dataPath + /ds/step1.vmd
  - デフォルトは dataPath（ android の実機の時だけ persistentDataPath ）
- PathUnit.IsAccessWithinParentPathOnly が true なら、ローカルファイルに関しては PathUnit.ParentPath 以下にあるファイルにしかアクセスできないようにした
  - デフォルトは true
  - アクセスすると IOException がスローされる（ null が返されるとかのほうがいいだろうか）
- json なのでいろいろ省略しても読める
  - ただしまだエラー処理とかテストとかしてないので、いろいろエラーも出ると思う
  - 各種データのパスとキャラ位置さえ指定しておけば、あとは省略（初期値となる）でもなんとなくOkだと思う
  - .json を別の .json でオーバーライドできるが、省略した部分はオーバーライドされる前の .json の値が残る

# その他
- vrm 自動インポートを切っていると、サンプルシーンでプレハブがきれてしまうのに気づきました
- ![image](https://github.com/user-attachments/assets/6270ef93-c64a-4bdc-8307-ac8bdcb78838)
  - チェックをオンにするか、Resources の中の vrm を reimport すると動くようです
  - vrm 0 は自動でプレハブを新規作成するようで、その時データとなるフォルダ？っぽいのが大量にできてしまう。それが嫌なのでオフにしてます
  - vrm ファイル自体がモデルのプレハブとして機能するようなので、新規作成されたプレハブとデータのフォルダは消してしまっても動きます
- unity 6 preview から addressables での Resources フォルダが使えなくなったようす…（ひどい）
  → Resources_moved に移動されてしまう

