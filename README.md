# AnimLite
- unity で .vmd を再生するライブラリ
- 現状は humanoid 向け
- 表情はとりあえず VRM 1.0 の expression に依存
- burst, playable にも対応した
- 名前空間 AnimLite.Vrm 関係は、UniVRM 1.x が必要です
- 非同期での .vmd 読み込み
- .vmd, model, web loading のキャッシュ機能
- 補助機能として、.json で音楽、モデル、アニメーション、配置、を設定 ＆ file/web からロードする機能

# 新機能・修正
2024.12.6
- zip sequential mode と IArchive のフォールバックの整合性がとれてなかったので修正
  - 並列ロードの時はセマフォで制御し、そうでないなら普通にロードされる
- ソースの s-jis を utf-8 bom にした、今更過ぎる…
- GameObject.InstantiateAsync を使うと vrm の spring bone で問題がでるようなのでやめた
  
2024.12.1
- .json に記載した相対パスでは、.json のあるフォルダを起点にするようにした。そのほうが自然かなと…
  - 今までは一律で PathUnit.ParentPath が起点だった
  - これに合わせて、zip 管理用の IArchive を拡張してフォルダ関連を管理するクラスにした
- zip の並列ロード時、１つだけオープンするモードを追加
  - 今までは並列単位で zip ファイルをオープンしていた（ ZipArchive と Stream クラスの都合上）
  - ファイルマッピングを使用することでなんとかなりそうだったので組み込んでみた。…が、quest3 で落ちたので調査中…
- 一部部位で quaternion の回転順序逆になってたかも、しゅうせいした orz　肩とか

2024.11.3
- .json の BackGrounds と Motions のキー名でワイルドカードを使えるようにした

2024.10.20
- vrm 0.127.0 にした
- graph.Evalute() でシークすると音ズレしていたが、ストリーミング読込のせいだとわかったのでオフにした
- vrm や humanoid を as resource でロードしたとき、（インスタンスが存在していても）プレハブを消すとメッシュ等も消えてしまうのが分かったので、リソース管理の仕組みを新しくした
  - これにともない humanoid キャッシュの仕組みも変更した
  - ゲームオブジェクトをプールしていたのをやめて、ひな型を１つだけ保存するようにした
  - オーディオやアニメーションも統一したい
- animation clip も as resource としてロードできるようにした
- sample6 の json オーバーライドを、２つ限定から配列指定できるようにした
  - これでキャラクタと背景を別々に差し替えたりできるようになった

2024.9.28
- AudioPlayable で Graph.Evalute() が効かなかったのを修正
  - Sample6 で , . キーを押して再生位置を前後移動するようにした

2024.9.27
- 複数の .vmd を読めるようにした
  - .json で "AnimationFilePath" に 配列も書ける  
      "AnimationFilePath": ["body.vmd", "face.vmd"]
  - 動作としては、複数の .vmd 辞書を１つの辞書にマージするかんじ。同じキーは後読みで上書き

2024.9.19
- .json 読込をオーバーライドできるようにした（Sample6）
  - オーバーライド分の .json に記述のある項目だけ上書きされ、あとはベースの値が残る
  - キャラだけ変えたり、モーションだけ変えたりできる
  - キャラや背景の定義を、配列からキーバリューペアにした。同じ名前を付けた内容についてオーバーライドされる
- 表情を catmul 補間にもどしてみた
  - 目がちゃんと閉じないときがあるなぁ
    
それ以前の修正
- local/web から file, zip をロード、また addressable resource もロードできるようにした
- .glb を読めるようにした(BackGrounds として)
- シーン上の game object インスタンスのキャッシュ（同じパスのモデルを再ロードしなくてもよいように）
- ロードの並列度を高めた（同じ .json に出てくる音楽、モデル、アニメーション、表情定義、のどれもが並列になるようにした）
- ZipArchive のロード時、ファイルストリームを使いまわさずに別個にファイルをオープンし、並列ロードできるスイッチを追加した
- ZipArchive の時に .glb が読めていなかったのを修正
- ZipArchive の vmd ロードで MemorryStream にコピーしていたのを解消
- .vmd キャッシュの参照カウントがロック対応されてなかったので、interlocked を使うようにした
- ~~.vrm から作成したモデルゲームオブジェクトを、シーン上でキャッシュしておく機能を追加（キャッシュ最大数の目安を指定できる）~~
  - ~~モデルロードはふつうに速いからあんまり意味ない気がする、メモリ無駄になるだけで余計な機能かなぁ~~
- データロードに１つでも失敗するとエラーで中断していたが、続行するようにした
  - 失敗したデータには default が入る
- http での .zip ロードで、クエリストリングの ? 以降がある場合、機能していなかったので修正
  ~~- ただし、https://.../ds.zip?dl=1/dance_scene.json のような記述はまだ無理（というか書き方どうしようか悩み中）~~
- http のクエリストリング + .zip entry の記述を可能にした
  - xxx/xxx.zip?xxx=xxx&xxx=xxx/xxx/xxx.xxx 形式
- http ロードしたファイルをキャッシュするようにした。
  - キャッシュファイルは Application.CashDataPath/loadcach フォルダ
  - 任意のタイミングでクリアできるが、必ずプログラム開始時にクリアされる

# やりたい・検討中
- IStreamProcedure が Absolute のときカクつく気がするので調査する
- そういえばカメラがないとふつうのビューワーだとダメか。ＶＲしか考えてなかったが…
- キャラごとにボーンのオフセット値を設定できるようにしたいかも（Ｔポーズ→Ａポーズもこれでやってもいい）
  - 回転制限か比率をつけるのもいいかなぁ、特に肩がモデルによって違うからな…
- ~~足ＩＫとつま先ＩＫ（向き）のオンオフを分けて .json に記載できるといいかも~~ <- 対応した
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
- ~~アニメーションとモデルがキャッシュされるので、音楽もキャッシュされるべき~~　すぐ読めるしいらんか
- 顔と体の bounding box が変なので修正したい
- zip 相対パス指定時の archive fallback が若干雑なとこあるのでちゃんとする

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
- zip paralell open single file 読み込みの時 android(quest3) で落ちるようになった、file mapping のとこで落ちてる様子
- 非同期と並列うまく扱えてない感　特に unity の Awaitable
- 非同期とバックグラウンドスレッドを分けた関数にしようかな  
  ~~- xxxAsync() と xxxBg() とか？~~  
  -> 基本的に async は非同期であって並列化ではないようにした  
     （並列化は Task.Run() や Awaitable.MainThreadAsync() などでやる）

# その他
- unity ~~2023.1.19f1~~ 6 prevew + ~~VRM1.20~~ VRM0.127.0
- テストコードは書いてない（よくわからない）、サンプルシーンが動けばとりあえずいいかみたいな
  -> ぼちぼち書いていくようにしますた
- とりあえず ~~quest2~~ quest3（買った！！）で遊びながら修正していきたい
  → ほしい機能がそろってきたので quest3 ライフが向上. deep link でブラウザから起動できるようにしたいがわからぬ...

# .json について
- Sample5 load from json と Sample6 override json シーンにサンプルがある
- ＶＲＭモデルが踊るのをもっと気軽に見たいので、なんかこういうフォーマットが世間に１つあるといいなと思う
  - よく考えたら vrm live viewer のフォーマットとかがそれに当たるんじゃないのか（ mmd 界隈には詳しくないので...）
- zip を読めるようにしたし「固めてアップロードしたので好きなビューワで見て」みたいな世界がくるといいなーとか思うし
- 楽曲、モデル、アニメーション、の作者情報をビューワーが積極的に表示するようにすれば、作者さんもシーンとして閲覧可能な形態で配布しよう的な気持ちになるんじゃないかなと（モーション単体とかじゃなく）
- DanceSetSimpleCaption はとりあえずシンプルな作者情報を表示するテロップ
```
{
    "Audio": {
        "AudioFilePath": "",
        "Volume": 0.0,
        "DelayTime": 0.0
    },
    //"DefaultAnimation": {                         // デフォルトのアニメーション <- 廃止（ワイルドカードで代用）
    //    "AnimationFilePath": "",
    //    "FaceMappingFilePath": "",
    //    "DelayTime": 0.0
    //},
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
                "AnimationFilePath": "",          // ["", ..., ""], とすれば複数 .vmd のマージ読込となる
                "FaceMappingFilePath": "",        // .vrm と .vmd の表情名対応表へのパス。"" ならデフォルトの対応表が使用される
                "DelayTime": 0.0
            },
            "Options": {
                "BodyScaleFromHuman": 0.0,        // unity humanoid の標準からのスケール。0.0 なら自動的に計算される
                "FootIkMode": "auto"              // .vmd のフットＩＫをどうするか。auto|on|off|leg_only|foot_only から選ぶ。auto は .vmd の足ＩＫにキーがあるかないかで自動判別する
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
  - 相対パス指定： step1.vmd（ . や .. も使える）
  - 絶対パス指定： c:/xxxx/ds/step1.vmd
  - web から： https://github.com/abarabone/AnimLite/raw/master/Asset/ds/step1.vmd
  - zip を指定： https://github.com/abarabone/AnimLite/raw/master/ds-sjis.zip
  - zip 内のパスを指定： https://github.com/abarabone/AnimLite/raw/master/ds-sjis.zip/ds/step1.vmd
  - drop box： https://www.dropbox.com/xxxx/step1.vmd?rlkey=kfga3v1soo6sple638gk326qt\&st=hrqrzch6\&dl=1 ← 末尾を dl=1 にすればよいみたい
    - クエリストリング中の & は、quest3(android?) だと \& のようにエスケープしないとダメだったので注意
    - one drive とか google のマイドライブとかだとパスに拡張子が含まれないので、content の type とか見る方法にしないとダメかも…
    - https://www.dropbox.com/xxxx/step1.vmd?rlkey=kfga3v1soo6sple638gk326qt\&st=hrqrzch6\&dl=1/ds/step2.vmd のような記述（クエリストリングの後に zip 内のパス指定）もOK
  - unity のリソースは末尾に as resource をつける： step1 as resource
  - AnimationFilePath に限り、単体でも配列でも記述できる  
    "AnimationFilePath": "body.vmd"  
    "AnimationFilePath": ["body.vmd", "face.vmd"]
- 単体 zip に固めたデータは、
  - ３種類のロードモードがあり、DanceSceneLoader.ZipLoaderMode で指定する（全体用の設定なのでロードの度に変えたりはできない）
    - Sequential              ... １つずつロード
    - ParallelOpenSingleFile  ... 並列してロードするが、ファイルは１つだけ開く（ファイルマッピングを使用、android だと不安定かも、調査中）
    - ParallelOpenMultiFiles  ... 並列してロードし、ファイルはそれぞれ開く（そのぶんメモリを消費する）
  - デフォルトは ParallelOpenMultiFiles
- いちおう utf8 と sjis の zip に対応しているつもり（ win の送るで作った zip はなんと shift-jis で内部パスが保存される仕様らしい）
- 相対パスは、下記のように絶対パスに変換してロードする
  - .json のあるフォルダを起点として検索
    - .json を上書き読みこみしている場合は、ベースとなった .json のあるフォルダも検索対象となる
  - 上記でみつからない場合、または .json を使用していない場合
    - FullPathMode.PersistentDataPath の時は Application.persistentDataPath + /ds/step1.vmd
    - FullPathMode.DataPath の時は Application.dataPath + /ds/step1.vmd
    - デフォルトは dataPath（ android の実機の時だけ persistentDataPath ）
- PathUnit.IsAccessWithinParentPathOnly が true なら、ローカルファイルに関しては PathUnit.ParentPath 以下にあるファイルにしかアクセスできないようにした
  - デフォルトは true
  - アクセスすると IOException をスローする（ null が返されるとかのほうがいいだろうか）
- json なのでいろいろ省略しても読める
  - ただしまだエラー処理とかテストとかしてないので、いろいろエラーも出ると思う
  - 各種データのパスとキャラ位置さえ指定しておけば、あとは省略（初期値となる）でもなんとなくOkだと思う
  - .json を別の .json でオーバーライドできるが、省略した部分はオーバーライドされる前の .json の値が残る
- Motions/BackGrounds のキー名でワイルドカード（ *, ?, # ）を使い、エントリの値を上書きできる
  - マッチするキーのエントリの値を上書きする
    - ちなみに # は数字１文字とマッチするが、# 自体ともマッチするようにした
  - ワイルドカードを使ったキーのエントリ自体は、表示対象ではなくなる
  - ワイルドカードでの上書きは、.json に登場した順に適用する
  - .json をオーバーライドした場合、下位 .json のエントリも上書きの対象となる
    ```
    "Motions": {
      "mob##": {                               // ワイルドカードを使ったキー名
        "Animation": {
          "AnimationFilePath": "step,vmd"
        }
      },
      "mob01": {                               // mob## がマッチするので、上書きされる対象となる
        "Model": {
           "ModelFilePath": "character1.vrm",  // ただし mob## では ModelFilePath の設定がないので、character1.vrm は生きる
        },
        "Animation": {
          "AnimationFilePath": "walk,vmd"      // mob## に AnimationFilePath の設定があるので、step.vmd で上書きされる
        }
      }
    }
    ```
- Motions/BackGrounds のキー名が '_' で始まるものは、表示対象ではなくなる
  - ただしキー名がマッチする他のエントリのベースとなることができる
  - ベースキーにはワイルドカードも使用できる
  - ベースも他のベースを継承できる
  - ベース継承は、より前に記述されたベースからのみ可能
  - 下位 .json からはベース継承できない（可能とするか迷ったが、意図しない継承が起きそうなのでやめた）
    ```
    "Motions": {
      "_center-*": {                           // ワイルドカードを使ったベースエントリ
        "Animation": {
          "AnimationFilePath": "step,vmd",
          "DelayTime": 0.4
        }
      },
      "center-ch": {                           // _center-* がマッチするので、ベースとして継承できる
        "Model": {
           "ModelFilePath": "character1.vrm"   // character1.vrm は普通に適用
        },
        "Animation": {
                                               // AnimationFilePath は center-ch に記述がないので、_center-* の step.vmd が継承できる
          "DelayTime": 0.0                     // DelayTime は center-ch で記述されているので 0.0 が適用
        }
      }
    }
    ```
- ワイルドカードでの上書き/ベース継承を実装したため、デフォルトアニメーションは廃止とした

# その他
- unity 6 preview から addressables での Resources フォルダが使えなくなったようす…（ひどい）
  -> Resources_moved に移動されてしまう

