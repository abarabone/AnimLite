# AnimLite
- unity で .vmd を再生するライブラリ
- 現状は humanoid 向け
- 表情はとりあえず VRM 1.0 の expression に依存
- burst, playable にも対応した
- 名前空間 AnimLite.Vrm 関係は、UniVRM 1.x が必要です
- 非同期での .vmd 読み込み
- .vmd, model, web loading のキャッシュ機能
- 補助機能として、.json で音楽、モデル、アニメーション、配置、を設定 ＆ file/web からロードする機能

# そういえば書き忘れてたけど
- clone したら、Assets/AnimLite/Resources_moved にある下記 .txt ファイルを、Addressable に登録して simply addressable names にしてください
  - default_body_adjust.txt
  - default_facemap.txt
- facemap と body adjust に何も指定しないときにデフォルトで参照するために必要です
- Asset/Resources_moved フォルダを reimport しないと、中の vrm がロードできないようです（ univrm の自動変換をオフにしているため）
- あと足接地は foot IK target というレイヤー名に属するものと判定するようになってます
- 使い方のドキュメント的なものが未整備なので、clone しても意味不明とは思う…

# 新機能・修正
2025.7.17
- .json 読込：メッシュ結合の際、結合対象をリストで指定できるようにした
  - 服やアクセサリーを着脱したり等の用途に使用できると思う
- .json の Model.Options で設定する
  - 対象としたいメッシュを含むゲームオブジェクト名を配列形式で指定できる
  - そこで使用されているマテリアル名も、同上のようにして対象としたいものを指定できる
  - 何も指定しなければすべて対象となる
  - ワイルドカードを使用可能
  - ! で始まる場合は、逆に対象としないようになる
  - \ でエスケープできる
- 変更
  - メッシュ単一化時のマテリアル名指定で、.json オプション名を変更
    - MeshMaterialName -> MeshMaterialOnSingleMesh など
- 修正
  - モデルキャッシュをオフにしてもエラーが出ないようにした

2025.7.2
- .json 読込：メッシュ結合に対応
  - 詳しくは issue に書いた
  - 下記３モード
    - 単一メッシュ単一テクスチャ化
    - マテリアルごとにサブメッシュ化
    - サブメッシュ化しつつ単一アトラス化
  - .json の Model.Options で設定する
    - 「.json について」を参照のこと
  - サンプルは Sample6 overrid json のぶいけっとにゃん３体（Asset/ds/dance_scene.json）
  - 修正
    - job モード（UseStreamHandleAnimationJob : false）の時、playable の初期位置を指定してなかったので、時々ずれたりしてた orz
      → .SetTime(0) 的なことをやって時間 0 から始まるようにした

2025.6.2
- .json 読込：Motions のキー名が _ から始まるベースキーが、いつのまにか読み込まれなくなってたので修正
- .json 読込：Motions のキー名でのワイルドカードで、先頭と終端に概念に対応していなかったので修正
- .json 読込：x, y, z 値のある値で、populate が利かなかったのを修正  
  [ x, y, z ] は [ null, 0 ] など、null や省略が可能（ null はベースの価が残る）
- .json 読込：Options で populate が利かなかったのを修正
- .Json 読込：Motions.Model の Scale およびロードされたモデルの Transform.LocalScale が移動などに正しく反映されないのを修正  
  プレハブやリソースとしてロードしたモデルの場合は、そのモデルの初期値に乗算される（ position は加算）
- 今回から修正事項は Issues に書くようにした

2025.5.13
- 足接地の補間を調整（フワフワ感が強くなってしまったかも…）
- 足首接地の角度を補間するようにした
- .json で vector3 のところをスカラーや [0.0, 0.0, 0.0] 形式でも書けるようにした（スカラーは x, y, z すべてに適用）
- 足接地の判定をレイキャストからスフィアキャストにした
- 修正
  - 並列ロード時に BackGround が１つもない時、最初の Motion が BackGround 扱いになってたのを修正
  - Motions.{keyname}.Model だけの .json をエラーで読み込めなかったのを修正
  - 足首ＩＫの値が、前方シークの時にしか正しく取得できなかったのを修正、ランダムシークでも読めるようになった

2025.4.24
- .json の Animation.Options 以下にある BodyScaleFromHuman/MoveScaleFromHuman/FootScaleFromHuman を、ベクトル値で指定できるようにした。
  - ベクトルは { "x":0.0, "y":0.0, "z":0.0 } または [0.0, 0.0, 0.0] という形式で指定できる
  - 今まで通り、スカラー値で指定も可能
- foot ik の接地で、段差でカクカクするのを補間で緩和した
  - そのおかげで足の高さ移動が 0.1 秒くらい遅れるので若干フワフワする場合があるかも（キャラクタの下に地面があるときのみ）
  - 足首の角度補間は leg only with ground の時対応が難しいことが分かったので、根本的に job 構成を変える必要があって未実装

2025.4.17
- foot ik の修正いろいろ
  - 段差とかでかくかくするの以外はだいたい解消できた気がするけどまだわからぬ
- なんか変な上書きしちゃったかも git よくわかんね

2025.4.15
- foot ik mode を混在させたとき、job の依存関係エラーが出ていたので解消した（と思う）
  - ik anchor への書き込みで、ik と fk+ground は依存関係なしで行けると思うんだけど、属性使っても回避できない
  - まだ変な挙動あるので直したい（ foot only の時とか）

2025.4.10
- 足ＩＫを接地に対応させた
  - animation job だと job 中で raycast が使えないので、job system で raycast command を使うモードを experimental として実装した
  - sample7 で animation job と job system での実行比較、sample8 で接地を実行
  - sample8 では .json に Mosions.Animation.Options.UseStreamHandleAnimationJob : false を記述すると使えることを示した（デフォルトでは true ）
  - 現状、従来のアニメーションモード（ transform, animation job playable ）では、接地は使えない
  - job system は ecs みたいに job を小さい単位で組んだんだけど、複雑になってしまった
    - animation job のようにモデル１体ごとに単一の job のほうがパフォーマンスでてるか？
      - モデル１体の時でも並列度は高くなってるが、トータル負荷は増えてる気がする（モデル増えたら逆転するか？）
      - 一連の job は、.json 単位で作ってる
  - 接地とみなされた時とそうでない時でいったりきたりして、足（foot）の角度がチラチラするので調整必要か…
- DanceSetPlayerFromJson でアニメーション終了を通知するようにした
  - OnPlayEnd を await すれば待機できる（ DanceSetSimpleCaption で使ってる） 
  
2025.1.26
- Motions.Animation.Options の BodyScaleFromHuman を、Body/Foot/Move に分けた
  - これにより、腰の位置をモデルの大きさに合わせたまま、移動量や歩幅だけ調節したりできるようになった

2025.1.17
- DanceSetPlayerFromJson のロード開始/完了を、待機可能な簡易公共イベントで流すようにした（AsyncMessaging\<T\>.Post()）
  - caption などはそれを await で待てる（await AsyncMessaging\<T\>.ReciveAsync()）
  - static 関数なので、参照をセットしたりなどはしないで、疎結合的にやり取りできるしくみ。T の型が同じ相手同士でやり取りされる

2025.1.14
- 今までT→Aポーズ固定だった、ポーズ補正を一般化した
  - .txt ファイルに humanoid 部位とローカル回転補正値を記述できる
  - .json に BodyAdjustFilePath として .txt ファイルのパスを指定する
- .json の Options を il2cpp, android, ios 向けの時だけ dynamic 型ではなく object 型になるようにプラグマを入れた
  - ていうかそもそも .net standard 2.1 向けだと dynamic 使えないっぽいので、USE_DYNAMIC が true じゃないと object になるようにしたけど悩み中
  - ExpandoObject だと quest3 でエラーでたので、Json.Net の JObject に変更したら動いた

2024.12.31
- .json の 各パートに Options として任意の設定をかけるようにした
- それに伴い、Motions にある Options を廃止

2024.12.19
- ファイルやリソースからロードしたものを IPrototype<T> というもので統一した。Instance<T> というのでインスタンスとして使用する。参照カウントで管理。
- キャッシュ機構を統一した。モーションとモデルで別々の仕組みだったのを、IPrototype<T> を使って参照カウントを取るようにした。
- いろいろ整理した。古いものを消した。そしてコミットとプッシュを暴発してしまった。結果、サンプルの 2, 4, は動かないと思う（DanceSet を使っているサンプル）
- あと sample 5 のパスで .. を使っているものがうまく動いてないような気がする、確認する前にプッシュしてしまった… orz

2024.12.6
- zip sequential mode と IArchive のフォールバックの整合性がとれてなかったので修正
  - これにともない、全体の並列/逐次フラグとして DanceSceneLoader.UseSeaquentialLoading を追加し、zip の並列とは分けた
  - zip 逐次ロードでは、全体を並列ロードする時はセマフォで制御し、そうでないなら普通に逐次ロードされる
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
- ~~.json でベクトル値を [0.0, 0.0, 0.0] でも記述できるようにしようと思う（現状は Animation.Options の scale のみ対応してる）~~
- 現状、.vmd のキャッシュはオンメモリだけど、一時ファイルにした方がよくない？メモリ占有し続けるのってどうなの
- ~~move/body/ik の補正スケールは水平と垂直で別にしたほうがいいかも？（ベクトルで指定するなど）~~
- 各種動作フラグとかは、現状の static 変数じゃなくて、関数に引数で渡す方式にしたほうがいいかな…
- IStreamProcedure が Absolute のときカクつく気がするので調査する
- そういえばカメラがないとふつうのビューワーだとダメか。ＶＲしか考えてなかったが…
- ~~キャラごとにボーンのオフセット値を設定できるようにしたいかも（Ｔポーズ→Ａポーズもこれでやってもいい）~~
  - 回転制限か比率をつけるのもいいかなぁ、特に肩がモデルによって違うからな…
- ~~足ＩＫとつま先ＩＫ（向き）のオンオフを分けて .json に記載できるといいかも~~ <- 対応した
- ~~http 時の .zip で クエリストリングの ? 以降と ZipArchive エントリの指定に対応させたい（が、http での .zip はあまり実用性ない気もする…）~~
  → xxx/xxx.zip?xxx=xxx&xxx=xxx/xxx/xxx.xxx の形式で記載できるようにした
- ~~.zip の並列スイッチは、ZipArchive のエントリの時に機能しないことに気づいたので、対応したい~~
  → 対応した、別のファイルとして扱って並列ロード可能とした
- ~~同じパスのモデルをキャッシュする際、同じモデルを複数体同時に表示すると最後のモデルしか表示されない~~
- DanceSetSimpleCaption において、キャラが多いとテロップが意味不明になるので、複数回に分けるとかスクロールするとか対処したい
- ~~WebGL でサンプル作ってどっかに置きたい~~　<- webgl だとスレッド使えないらしいのでダメかも
- DanceSet を DanceScene にリネームしたいかも
- ~~現状 DanceSet の機構が新旧２重になっているので統合したい（エディタ上でオーサリングするタイプと .json しか考慮してないタイプ）~~
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
- そこそこ使えるようになってきた気がするので、そろそろ外面的な関数だけでも整理しようか…

# なやみちゅう
- zip paralell open single file 読み込みの時 android(quest3) で落ちるようになった、file mapping のとこで落ちてる様子
- 非同期と並列うまく扱えてない感　特に unity の Awaitable
- 非同期とバックグラウンドスレッドを分けた関数にしようかな  
  ~~- xxxAsync() と xxxBg() とか？~~  
  -> 基本的に async は非同期であって並列化ではないようにした  
     （並列化は Task.Run() や Awaitable.MainThreadAsync() などでやる）

# その他
- unity 6 ~~~6 prevew~~~ + ~~VRM1.20~~ VRM0.127.0
- テストコードは書いてない（よくわからない）、サンプルシーンが動けばとりあえずいいかみたいな
  -> ぼちぼち書いていくようにしますたが、なんかこのプロジェクトだとビルド通らなくなるので外した
- とりあえず ~~quest2~~ quest3（買った！！）で遊びながら修正していきたい
  → ほしい機能がそろってきたので quest3 ライフが向上. deep link でブラウザから起動できるようにしたいがわからぬ...

# .json について
- Sample5 load from json と Sample6 override json シーンにサンプルがある
- ＶＲＭモデルが踊るのをもっと気軽に見たいので、なんかこういうフォーマットが世間に１つあるといいなと思う
  - よく考えたら vrm live viewer のフォーマットとかがそれに当たるんじゃないのか（ mmd 界隈には詳しくないので...）
- zip を読めるようにしたし「固めてアップロードしたので好きなビューワで見て」みたいな世界がくるといいなーとか思うし
  -> うまいこと暗号化できればいいんだけど、なんかアイデアないかね
- 楽曲、モデル、アニメーション、の作者情報をビューワーが積極的に表示するようにすれば、作者さんもシーンとして閲覧可能な形態で配布しよう的な気持ちになるんじゃないかなと（モーション単体とかじゃなく）
- DanceSetSimpleCaption はとりあえずシンプルな作者情報を表示するテロップ
```
{
    "Audio": {
        "AudioFilePath": "",
        "Volume": 0.0,
        "DelayTime": 0.0,
        "Options": { ... }                        // 任意のデータ
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
        "Scale": 0.0,
        "Options": {
          "MeshCombineMode": "None",              // 選択肢は "IntoSingleMesh" | "ByMaterial" | "ByMaterialAndAtlasTextures"
          "MeshMaterialOnSingleMesh": "",         // IntoSingleMesh の時に、マテリアル名を指定。ワイルドカードに対応。シェーダー名でもよい
          "SkinMaterialOnSingleMesh": "",         // 同上で、スキンメッシュのマテリアルを指定する。省略時は MeshMaterialOnSingleMesh と同じマテリアルが適用される
          "BlendShapeMaterialOnSingleMesh": "",   // 同上で、ブレンドシェイプ付スキンメッシュのマテリアルを指定する。省略時は同上
          "TargetMeshList": [],                   // 結合対象とするメッシュを含むゲームオブジェクト名を配列で指定。ワイルドカードに対応。先頭に ! をつけると逆に対象としない指定。エスケープは \ で。省略するとすべてのメッシュが結合対象となる
          "TargetMaterialList": []                // 結合対象とするマテリアル名を配列で指定。ワイルドカード、省略時、等は TargetMeshList と同じ。
        }
      },
      "Options": { ... }                          // 任意のデータ
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
                "Scale": 0.0,
                "Options": {
                  "MeshCombineMode": "None",      // 選択肢は "IntoSingleMesh" | "ByMaterial" | "ByMaterialAndAtlasTextures"
                  "MeshMaterialName": "",         // IntoSingleMesh の時に、マテリアル名を指定。ワイルドカードに対応。シェーダー名でもよい
                  "SkinMaterialName": "",         // 同上で、スキンメッシュのマテリアルを指定する。省略時は MeshMaterialName と同じマテリアルが適用される
                  "SkinBlendMaterialName": ""     // 同上で、ブレンドシェイプ付スキンメッシュのマテリアルを指定する。省略時は同上
                }
            },
            "Animation": {                        // キャラクターのアニメーション
                "AnimationFilePath": "",          // ["", ..., ""], とすれば複数 .vmd のマージ読込となる
                "FaceMappingFilePath": "",        // .vrm と .vmd の表情名対応表へのパス。"" ならデフォルトの対応表が使用される
                "BodyAdjustFilePath": "",         // アニメーションのポーズ補正表へのパス。"" ならデフォルトの補正表（T -> A ポーズ変換用）が使用される
                "DelayTime": 0.0,
                "Options": {                      // Vmd では下記を記載可能だが、任意のデータを記述できる
                    "BodyScaleFromHuman": 0.0,    // 身体サイズの補正比率。0.0 なら自動的に計算される（ベクトルで軸ごとの指定も可能）
                    "FootScaleFromHuman": 0.0,    // 足ＩＫ位置の補正比率。0.0 なら自動的に計算される（ベクトルで軸ごとの指定も可能）
                    "MoveScaleFromHuman": 0.0,    // キャラクター移動の補正比率。0.0 なら自動的に計算される（ベクトルで軸ごとの指定も可能）
                    "FootIkMode": "auto",         // .vmd のフットＩＫをどうするか。auto|on|off|leg_only|foot_only から選ぶ。auto は .vmd の足ＩＫにキーがあるかないかで自動判別する
                                                  // _with_ground をつけると接地ＩＫを使う（ auto_with_ground とか leg_only_with_ground とか書く）
                    "UseStreamHandleAnimationJob": true  // 従来の animation job モードを使用する（接地を試すには false ）
                },
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
    },
    "Options": { ... }                            // 任意のデータ
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
    -> ちなみに１つ下位の .json で _* が定義されているとしたら、１つ上位で _* { } と空の定義を書けばベースとして扱うことができる  
    （通常の定義は下位からオーバーライドでき、そのオーバーライドをベースとして同一 .json 内部で参照すればＯＫ）
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
- 各ブロックに Options として、任意の内容を記述可能。想定としては、アプリケーションによって個別に解釈する用途
  - 現状では、Animation ブロックで vmd 用に FootIkMode などで使用している
  - .json に書いてあるプロパティのみ取得される
  - .Options は dynamic アクセスが可能。内部には ExpandoObject が格納されている（初期値は null だがアクセスした時点で生成される）
  - .OptionsAs<デシリアライズ先クラス>() でデフォルト値を考慮した型変換ができ、ちゃんとした型で受け取れる（動作としては populate のような感じ）
- Motion.Animation.Options の BodyScaleFromHuman について
  - BodyScaleFromHuman
    - アニメーションにおける身体サイズの補正値。値 * 0.8 * 0.1 がアニメーションの移動ベクトルに乗算される。
      - 値が 0.0 なら自動計算され、Animator.humanScale * 0.8 * 0.1 となる（humanScale は標準股下 1m からの比率）
      - \* 0.8 は、unity humanoid の股下標準(1m)と mmd 標準ミクとの股下の比率
      - \* 0.1 は、mmd の位置単位から unity(1m) への補正値。経験的に割り出したので、根拠のある値ではない
      - 値を 1.0 にすると、vmd 本来のモデルと近い値となる（ように試行錯誤で調節したつもり）
  - FootScaleFromHuman は足ＩＫの補正値。Body と同様の計算で、歩幅が調節される
  - MoveScaleFromHuman はキャラクター移動量の補正値。計算は Body と同様
  - Body を 0.0 にして Move を 1.0 にすれば、体の上下移動をキャラクターの身体サイズに合わせつつ、移動量を元のモデルと同じにできる
    - Foot を Move と異なる値にすると滑ってみえる
    - 小さいキャラで 1.0 にすると大股になって頑張ってる感じがしてかわいい
  - 今考えると FromHuman って意味わかんないな、FromVmd とかのがいいのでは？ FromHumanoid ならまだしも…
  - 各 scale 値は、スカラー値指定とベクトル値指定が可能
    - ベクトル値は {"x":0.0, "y":0.0, "z":0.0} または [0.0, 0.0, 0.0] のいずれかの記述ができる

# ポーズ補正ファイルについて
- テキストファイルで、HumanoidBodyBones Enum 値に対応した部位ごとに、ローカル回転を記述する
  - .json の BodyAdjustFilePath を "" にするとデフォルトの補正ファイルが使用されるが、下記のような T -> A ポーズ補正となっている。
```
LeftUpperArm
  lrot z+30
RightUpperArm
  lrot z-30
```
- 現状はローカル回転しか記述できない
  - ローカル回転は、lrot | local rotation | LocalRotation の後に、x|y|z{実数} という形式で記述する
- 回転は x|y|z 軸を {実数} 度という意味合いを持ち、* でクォータニオンの掛け算のように記載できる
- ところで https://www.nicovideo.jp/watch/sm42270202 からＤＬさせてもらえる「ドクヘビ」という .vmd データは、どうも肩が 30 度くらい前に傾いている
  - これを補正するには、下記のような補正ファイルを用意すればよい
``` 
LeftShoulder
  lrot y-30
RightShoulder
  lrot y+30
LeftUpperArm
  lrot y+30 * z+30 
RightUpperArm
  lrot y-30 * z-30
```
- ちなみに下記のように処理している。あんまり自信はない…
```
ボーン回転補正 = 親ボーンの回転補正 * 補正ファイルから得た回転 * アバターから得た初期姿勢回転
localRotation = inv(親のボーン回転補正) * .vmd から得た回転 * ボーン回転補正
```
- ワールド回転でやったほうが無駄ないかもねぇ

# その他
- unity 6 preview から addressables での Resources フォルダが使えなくなったようす…（ひどい）
  -> Resources_moved に移動されてしまう

