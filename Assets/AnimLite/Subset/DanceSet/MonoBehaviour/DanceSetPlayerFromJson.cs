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
        [FilePath]
        public PathUnit JsonFileOverwrite;


        DanceGraphy2 graphy;

        public PlayableGraph? Graph => this.graphy?.graph;


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
                    using var archive0 = await this.JsonFile.OpenWhenZipAsync(ct);
                    using var archive1 = await this.JsonFileOverwrite.OpenWhenZipAsync(archive0, ct);
                    var ds0 = await archive0.LoadJsonAsync<DanceSetJson>(this.JsonFile, ct);
                    var ds1 = await archive1.LoadJsonAsync<DanceSetJson>(this.JsonFileOverwrite, ds0, ct);
                    var order = await ds1.BuildDanceGraphyOrderAsync(this.Cache, archive1, this.AudioSource, ct);
                    
                    await Awaitable.MainThreadAsync();
                    this.graphy = DanceGraphy2.CreateGraphy(order);

                    this.graphy.graph.Play();
                    this.DanceSceneCaption?.NortifyPlaying(ds1);

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