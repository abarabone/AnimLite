
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
        /// ���łɃ��[�h�ς݂̃��f���ł���΁A���̕�����Ԃ��B
        /// ����ł���΃��f�������[�h���ĕԂ��B
        /// </summary>
        public async Task<GameObject> GetOrLoadAsync(PathUnit path, ZipArchive archive, CancellationToken ct)
        {
            var holder = await this.stocker.GetOrAddLazyAaync(path, async () =>
            {
                return new ModelStockerHolder
                {
                    Template = await path.LoadModelExAsync(archive, ct),
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
        /// �X�g�b�N���ꂽ�Q�[���I�u�W�F�N�g�i���łɔj�����ꂽ���̂������j�ɂ��āA�Œ�P�����c���Ă��Ƃ͔j������B
        /// �܂� MaxStockModelLength �𒴂����ꍇ�A�Â����̂���j������B
        /// ��ɂȂ��� ModelStockerHolder �́A���������菜���B
        /// �܂��A�V�[����Ŋ��� Destroy() ����Ă�����̂Ɋւ��ẮA�X�g�b�N���珜�O����Y�������������B
        /// ����ɂ��X�g�b�N�����I�u�W�F�N�g���Ȃ��Ȃ��Ă��A�܂��V�K���[�h�ΏۂɂȂ邾���B
        /// �������ADestroy() �����f�����^�C�~���O�ɂ͒��ӂ��邱�ƁB�i�����炭 Destory() �̎��̃t���[������j
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

                    prevList.Clear();// �K�v�Ȃ�����
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
