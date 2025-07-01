using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using Unity.VisualScripting;
using UnityEngine.Playables;

namespace AnimLite.DancePlayable
{
    using AnimLite.Utility;
    using AnimLite.Loader;
    using AnimLite.Vrm;
    using AnimLite.Vmd;
    using static AnimLite.DancePlayable.DanceGraphy;
    //using static UnityEditor.Progress;
    //using System.IO.Compression;
    //using System.Security.Cryptography;
    //using UnityEditor.VersionControl;

    public class DanceSetPlayerFromJson : MonoBehaviour
    {
        public Transform LookAtTarget;
        public AudioSource AudioSource;

        public PrototypeCacheManager Cache;


        [FilePath]
        public PathUnit[] JsonFiles;


        DanceGraphy graphy;

        public PlayableGraph? Graph => this.graphy?.graph;
        //public float TotalTime => this.graphy?.timekeeper.TotalTime ?? 0.0f;// 暫定
        public DanceTimeKeeper TimeKeeper => this.graphy?.timekeeper;



        public SemaphoreSlim DanceSemapho { get; } = new(1);

        CancellationTokenSource cts;


        public struct OnLoadStart { }
        public struct OnLoaded { public DanceSceneJson ds; }
        public struct OnPlayEnd { }

        public void Play()
        {
            this.graphy.graph.Play();
        }
        public void Stop()
        {
            this.graphy.graph.Stop();
        }

        private async Awaitable OnEnable()
        {
            try
            {
                "load start".ShowDebugLog();
                using (await this.DanceSemapho.WaitAsyncDisposable(default))
                {
                    AsyncMessaging<OnLoadStart>.Post();
                    this.cts = CancellationTokenSource.CreateLinkedTokenSource(this.destroyCancellationToken);
                    var ct = this.cts.Token;
                    

                    using var x = await this.JsonFiles.LoadDanceSceneAsync(ct);
                    var order = await x.dancescene.BuildDanceGraphyOrderAsync(this.Cache?.Holder, x.archive, this.AudioSource, ct);

                    await Awaitable.MainThreadAsync();
                    this.graphy = DanceGraphy.CreateGraphy(order);

                    adjustModel_(order);
                    changeVisibility_(order, true);

                    this.graphy.graph.Play();


                    AsyncMessaging<OnLoaded>.Post(new OnLoaded { ds = x.dancescene });
                }
                "load end".ShowDebugLog();


                await this.graphy.timekeeper.WaitForEndAsync(this.destroyCancellationToken);
                AsyncMessaging<OnPlayEnd>.Post();

            }
            catch (OperationCanceledException e)
            {
                e.Message.ShowDebugLog();
                await this.graphy.DisposeNullableAsync();
                this.graphy = null;

                AsyncMessaging<OnLoadStart>.Throw(e);
                AsyncMessaging<OnLoaded>.Throw(e);
                AsyncMessaging<OnPlayEnd>.Throw(e);
            }
            catch (Exception e)
            {
                e.ShowDebugError();
                await this.graphy.DisposeNullableAsync();
                this.graphy = null;

                AsyncMessaging<OnLoadStart>.Throw(e);
                AsyncMessaging<OnLoaded>.Throw(e);
                AsyncMessaging<OnPlayEnd>.Throw(e);
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

                AsyncMessaging<OnLoadStart>.Cancel();
                AsyncMessaging<OnLoaded>.Cancel();
                AsyncMessaging<OnPlayEnd>.Cancel();
            });
        }
    }

}