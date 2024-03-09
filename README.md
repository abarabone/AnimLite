# AnimLite
- unity で .vmd を再生するライブラリ
- 現状は humanoid 向け
- 表情はとりあえず VRM 1.0 の expression に依存
- burst, playable にも対応した

# いずれ
- 表面的な最適化もやらねば
  - burst 関数の引数に ref/in とか [NoArias] とか
  - aggressive inlining つけたり
- 汎用のアニメーションライブラリにしたい
  - 複数アニメーションに対応させる
- ecs でも使えるようにしたい
