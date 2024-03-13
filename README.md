# AnimLite
- unity で .vmd を再生するライブラリ
- 現状は humanoid 向け
- 表情はとりあえず VRM 1.0 の expression に依存
- burst, playable にも対応した
- 名前空間 AnimLite.Vrm 関係は、UniVRM 1.x が必要です

# いずれ
- 表面的な最適化もやらねば
  - burst 関数の引数に ref/in とか [NoArias] とか
  - aggressive inlining つけたり
- 汎用のアニメーションライブラリにしたい
  - 複数アニメーションに対応させる
- ecs でも使えるようにしたい
- body motion が burst job なのに、face の playable script がメインスレッドなうえに遅い、なんとかならないか…
- 現状、.vmd のパースはモデルが初期状態の T-pose であることを仮定しているため、２度読みするとその時点のポーズを初期状態としてとらえてしまい、ポーズがおかしくなる。
  どうにかしたいが…
  - T-pose に戻す？（方法わからず、力技で上書きするしかない？）← これを採用した anim.ResetPose() を追加した
  - ２度読み時は bone 構造（TransformMappings オブジェクト）を更新しないようにする？
