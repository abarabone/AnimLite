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
    using static UnityEditor.Progress;
    using System.Collections.Generic;
    using static AnimLite.DancePlayable.DanceGraphy2;

    public class DanceSetPlayerFromJson : DanceSetPlayerBase
    {
        public Transform LookAtTarget;
        public AudioSource AudioSource;

        public VmdStreamDataCache Cache;

        [FilePath]
        public PathUnit JsonFile;


        DanceGraphy2 graphy;

        public PlayableGraph Graph => this.graphy.graph;


        public override Awaitable<DanceSetDefineData> WaitForPlayingAsync => this._waitForPlaying.Awaitable;

        AwaitableCompletionSource<DanceSetDefineData> _waitForPlaying = new();


        private async void Start()
        {
            var ct = this.destroyCancellationToken;
            //this._waitForPlaying.Reset();

            try
            {
                await Task.Run(async () =>
                {
                    var path = this.JsonFile;//.ToFullPath();
                    using var archive = await path.OpenZipAsync(ct);

                    var json = await path.ReadJsonExAsync<DanceSetJson>(archive, ct);
                    var ds = json.ToData();
                    var order = await ds.BuildDanceOrderAsync(archive, this.Cache, this.AudioSource, ct);
                    await ds.OrverrideInformationIfBlankAsync(order);

                    await Awaitable.MainThreadAsync();
                    this.graphy = DanceGraphy2.CreateGraphy(order);

                    this.graphy.graph.Play();
                    this._waitForPlaying.SetResult(ds);

                    adjustModel_(order);
                    changeVisibility_(order, true);
                });
            }
            catch (OperationCanceledException e)
            {
                this._waitForPlaying.SetCanceled();
                e.Message.ShowDebugLog();
            }
            catch (Exception e)
            {
                this._waitForPlaying.SetException(e);
                Debug.LogError(e);
            }

            return;


            void changeVisibility_(Order order, bool isVisible)
            {
                order.BackGrouds
                    ?.ForEach(x => x.Model.SetActive(isVisible));
                order.Motions
                    ?.ForEach(x => x.Model.SetActive(isVisible));
            }

            void adjustModel_(Order order)
            {
                order.Motions
                    .ForEach(x =>
                    {
                        x.Model.GetComponent<UniVRM10.Vrm10Instance>().AdjustLootAt(Camera.main.transform);
                        x.FaceRenderer.AdjustBbox(x.Model.GetComponent<Animator>());
                    });
            }
        }

        private void OnDestroy()
        {
            this.graphy?.Dispose();
        }
    }

}