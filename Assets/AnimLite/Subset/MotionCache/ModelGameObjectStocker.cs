using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using System.Linq;
using System.Collections.Concurrent;
using System.IO.Compression;

namespace AnimLite.Vmd
{
    using AnimLite.Vrm;
    using AnimLite.Utility;
    using AnimLite.DancePlayable;
    using AnimLite.Utility.Linq;


    [Serializable]
    public class ModelGameObjectStocker// : MonoBehaviour
    {

        ConcurrentDictionary<PathUnit, AsyncLazy<ModelStockerHolder>> stocker = new();

        public int MaxStockModelLength;


        /// <summary>
        /// すでにロード済みのモデルであれば、その複製を返す。
        /// 初回であればモデルをロードして返す。
        /// </summary>
        public async Task<GameObject> GetOrLoadAsync(PathUnit path, IArchive archive, CancellationToken ct)
        {
            var holder = await this.stocker.GetOrAddLazyAaync(path, async () =>
            {
                return new ModelStockerHolder
                {
                    Template = await archive.LoadModelExAsync(path, ct),
                    LastAccessFrame = await TaskUtility.OnMainThreadAsync(() => Time.frameCount),
                };
            });

            $"load holder {holder.GameObjects?.Count} : {path.Value}".ShowDebugLog();
            if (holder.GameObjects.Count == 1) return holder.Template;


            await Awaitable.MainThreadAsync();
            var model = GameObject.Instantiate(holder.Template);
            holder.GameObjects.Add(model);
            holder.LastAccessFrame = Time.frameCount;
            return model;
        }

        /// <summary>
        /// ストックされたゲームオブジェクト（すでに破棄されたものを除く）について、最低１つだけ残してあとは破棄する。
        /// また MaxStockModelLength を超えた場合、古いものから破棄する。
        /// 空になった ModelStockerHolder は、辞書から取り除く。
        /// また、シーン上で既に Destroy() されているものに関しては、ストックから除外されズレが解消される。
        /// それによりストックされるオブジェクトがなくなっても、また新規ロード対象になるだけ。
        /// ただし、Destroy() が反映されるタイミングには注意すること。（おそらく Destory() の次のフレームから）
        /// </summary>
        public async ValueTask TrimGameObjectsAsync()
        {
            var stocks = await getStocksAsync_();

            destroyLeaveOneAndTrimList_(stocks);
            destroyOverMaxAndTrimList_(stocks);
            trimHolderOfNoObject_(stocks);
            
            return;


            ValueTask<(PathUnit, ModelStockerHolder)[]> getStocksAsync_() =>
                this.stocker
                    .ToAsyncEnumerable()
                    .SelectAwait(async x => (path: x.Key, holder: await x.Value))
                    .ToArrayAsync();

            void destroyLeaveOneAndTrimList_((PathUnit path, ModelStockerHolder holder)[] stocks)
            {
                foreach (var holder in stocks.Select(x => x.holder))
                {
                    var prevList = holder.GameObjects;
                    holder.GameObjects = null;

                    var models = prevList
                        .Where(model => !model.IsUnityNull())
                        .ToArray();

                    prevList.Clear();// 必要ないかも
                    if (models.Length == 0) $"model length 0 : {prevList.FirstOrDefault()?.name}".ShowDebugLog();
                    if (models.Length == 0) continue;

                    models
                        .Skip(1)
                        .Do(x => $"destroy {x.name}".ShowDebugLog())
                        .ForEach(model => model.Destroy());

                    holder.GameObjects = models
                        .Take(1)
                        .Do(x => $"deactive {x.name}".ShowDebugLog())
                        .Do(model =>
                        {
                            model.SetActive(false);

                            var anim = model.GetComponent<Animator>().AsUnityNull();
                            anim?.UnbindAllStreamHandles();
                            anim?.ResetPose();
                        })
                        .ToList();
                }
            }

            void destroyOverMaxAndTrimList_((PathUnit path, ModelStockerHolder holder)[] stocks)
            {
                var qStock = stocks
                    .Where(x => x.holder.GameObjects != null)
                    .OrderBy(x => x.holder.LastAccessFrame)
                    .Skip(this.MaxStockModelLength);
                foreach (var stock in qStock)
                {
                    stock.holder.GameObjects[0].Destroy();
                    stock.holder.GameObjects = null;
                    $"trim max : {stock.path.Value}".ShowDebugLog();
                }
            }

            void trimHolderOfNoObject_((PathUnit path, ModelStockerHolder holder)[] stocks)
            {
                var qStock = stocks
                    .Where(x => x.holder.GameObjects == null);
                foreach (var stock in qStock)
                {
                    var res = this.stocker.TryRemove(stock.path, out var _);
                    $"trim list null : {stock.path.Value} {res}".ShowDebugLog();
                }
            }

        }
    }

    public class ModelStockerHolder
    {

        public GameObject Template
        {
            get => this.GameObjects.FirstOrDefault();
            set => this.GameObjects.Add(value);
        }

        public List<GameObject> GameObjects { get; set; } = new List<GameObject>();

        public int LastAccessFrame;

    }

}
