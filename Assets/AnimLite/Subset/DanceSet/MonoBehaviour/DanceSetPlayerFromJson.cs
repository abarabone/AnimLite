using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Animations;
using Unity.VisualScripting;
using UnityEngine.Playables;
using System;
using System.Threading.Tasks;

namespace AnimLite.DancePlayable
{
    using AnimLite.Utility;
    using AnimLite.Vrm;
    using AnimLite.Vmd;
    //using static UnityEditor.Progress;
    using System.Collections.Generic;
    using static AnimLite.DancePlayable.DanceGraphy2;
    using System.IO.Compression;

    public class DanceSetPlayerFromJson : MonoBehaviour
    {
        public Transform LookAtTarget;
        public AudioSource AudioSource;

        public VmdStreamDataCache Cache;

        [FilePath]
        public PathUnit JsonFile;


        DanceGraphy2 graphy;

        public PlayableGraph Graph => this.graphy.graph;


        [SerializeField]
        public DanceSceneCaptionBase DanceSceneCaption;



        public SemaphoreSlim DanceSemapho { get; } = new SemaphoreSlim(1, 1);

        CancellationTokenSource cts;




        private async Awaitable OnEnable()
        {
            this.DanceSceneCaption?.SetEnable(true);
            this.cts = CancellationTokenSource.CreateLinkedTokenSource(this.destroyCancellationToken);
            var ct = this.cts.Token;

            try
            {
                "load start".ShowDebugLog();
                using (await this.DanceSemapho.WaitAsyncDisposable(default))
                {
                    //var path = "motions/神社橋配布用test.glb".ToPath();
                    //var model = await path.LoadModelExAsync(ct);
                    //await Awaitable.MainThreadAsync();
                    //model.SetActive(true);

                    var jsonpath = this.JsonFile;
                    using var archive = await jsonpath.OpenZipAsync(ct);
                    var ds = await jsonpath.LoadDanceSceneAsync(archive, ct);
                    var order = await ds.BuildDanceGraphyOrderAsync(this.Cache, archive, this.AudioSource, ct);

                    //var order = await ds.BuildDanceOrderAsync(this.Cache, this.AudioSource, ct);
                    //order.Motions.First().Model.SetActive(true);

                    await Awaitable.MainThreadAsync();
                    this.graphy = DanceGraphy2.CreateGraphy(order);

                    this.graphy.graph.Play();
                    this.DanceSceneCaption?.NortifyPlaying(ds);

                    adjustModel_(order);
                    changeVisibility_(order, true);
                }
                "load end".ShowDebugLog();
            }
            catch (OperationCanceledException e)
            {
                e.Message.ShowDebugLog();
                this.graphy?.Dispose();
                this.graphy = null;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                this.graphy?.Dispose();
                this.graphy = null;
            }
            finally
            {
                this.cts.Dispose();
                this.cts = null;
                "canceller disposed".ShowDebugLog();
            }

            return;


            void changeVisibility_(Order order, bool isVisible)
            {
                order.BackGrouds
                    ?.ForEach(x => x.Model?.SetActive(isVisible));
                order.Motions
                    ?.ForEach(x => x.Model?.SetActive(isVisible));
            }

            void adjustModel_(Order order)
            {
                order.Motions
                    .ForEach(x =>
                    {
                        x.Model?.GetComponent<UniVRM10.Vrm10Instance>()?.AdjustLootAt(Camera.main.transform);
                        x.FaceRenderer?.AdjustBbox(x.Model.GetComponent<Animator>());
                    });
            }
        }

        private async Awaitable OnDisable()
        {
            await Err.LoggingAsync(async () =>
            {
                this.cts?.Cancel();

                "disable start".ShowDebugLog();
                using (await this.DanceSemapho.WaitAsyncDisposable(default))// ゲームオブジェクトが破棄されても、解放はやり切ってほしいので Token は default
                {
                    await (this.Cache?.HideAndDestroyModelAsync() ?? default);

                    this.graphy?.Dispose();
                    this.graphy = null;
                }
                "disable end".ShowDebugLog();

                this.DanceSceneCaption?.SetEnable(false);
            });
        }
    }

}