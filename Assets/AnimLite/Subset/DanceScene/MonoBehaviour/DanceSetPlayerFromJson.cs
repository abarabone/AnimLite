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
    using AnimLite.Loader;
    using AnimLite.Vrm;
    using AnimLite.Vmd;
    //using static UnityEditor.Progress;
    using System.Collections.Generic;
    using static AnimLite.DancePlayable.DanceGraphy;
    using System.IO.Compression;
    using System.Security.Cryptography;

    public class DanceSetPlayerFromJson : MonoBehaviour
    {
        public Transform LookAtTarget;
        public AudioSource AudioSource;

        public PrototypeCacheManager Cache;



        [FilePath]
        public PathUnit[] JsonFiles;


        DanceGraphy graphy;

        public PlayableGraph? Graph => this.graphy?.graph;
        public float? TotalTime => this.graphy?.TotalTime;// 暫定


        [SerializeField]
        public DanceSceneCaptionBase DanceSceneCaption;



        public SemaphoreSlim DanceSemapho { get; } = new (1);

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
                    using var x = await this.JsonFiles.LoadDanceSceneAsync(ct);
                    var order = await x.dancescene.BuildDanceGraphyOrderAsync(this.Cache?.Holder, x.archive, this.AudioSource, ct);

                    await Awaitable.MainThreadAsync();
                    this.graphy = DanceGraphy.CreateGraphy(order);

                    adjustModel_(order);
                    changeVisibility_(order, true);

                    this.graphy.graph.Play();
                    this.DanceSceneCaption?.NortifyPlaying(x.dancescene);
                }
                "load end".ShowDebugLog();
            }
            catch (OperationCanceledException e)
            {
                e.Message.ShowDebugLog();
                await this.graphy.DisposeNullableAsync();
                this.graphy = null;
            }
            catch (Exception e)
            {
                e.ShowDebugError();
                await this.graphy.DisposeNullableAsync();
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
                    ?.ForEach(x => x.Model?.Value?.SetActive(isVisible));
                order.Motions
                    ?.ForEach(x => x.Model?.Value?.SetActive(isVisible));
            }

            void adjustModel_(Order order)
            {
                order.Motions
                    .ForEach(x =>
                    {
                        x.Model?.Value?.GetComponent<UniVRM10.Vrm10Instance>()?.AdjustLootAt(Camera.main.transform);
                        //x.FaceRenderer?.AdjustBbox(x.Model?.Value?.GetComponent<Animator>());
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
                    //await (this.Cache?.HideAndDestroyModelAsync() ?? default);

                    //await Awaitable.MainThreadAsync();
                    await this.graphy.DisposeNullableAsync();
                    this.graphy = null;
                }
                "disable end".ShowDebugLog();

                this.DanceSceneCaption.AsUnityNull()?.SetEnable(false);
            });
        }
    }

}