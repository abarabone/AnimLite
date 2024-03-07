# AnimLite
- unity で .vmd を動かすライブラリ
- 現状は humanoid 向け
- 表情はとりあえず VRM 1.0 で制御
- burst, playable にも対応した

# いずれ
- 表面的な最適化
  - burst 関数の引数に ref/in とか [NoArias] とか
  - aggressive inlining つけたり
- 汎用のアニメーションライブラリにしたい
  - 複数アニメーションに対応させる
- ecs でも使えるようにしたい

シンプルにしようとしたけど構造体しばりとジェネリクスで思うようにいかなかった感
