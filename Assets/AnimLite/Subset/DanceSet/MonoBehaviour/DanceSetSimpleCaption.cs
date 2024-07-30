using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Animations;
using Unity.VisualScripting;
using UnityEngine.Playables;
using System;
using TMPro;

namespace AnimLite.DancePlayable
{
    using AnimLite.Utility;
    //using AnimLite.Vrm;
    //using AnimLite.Vmd;

    public class DanceSetSimpleCaption : MonoBehaviour
    {

        [SerializeField]
        //[SerializeReference]
        public DanceSetPlayerBase DanceSetPlayer;

        public Canvas Canvas;
        public CanvasGroup LoadingGroup;
        public CanvasGroup InfoGroup;
        public TMP_Text AudioInfo;
        public TMP_Text ModelInfo;

        public float DisplayedTimeSec = 6.0f;

        CancellationTokenSource cts;


        private void Awake()
        {
            this.Canvas.gameObject.SetActive(false);
        }

        private async Awaitable OnEnable()
        {
            this.Canvas.gameObject.SetActive(true);

            try
            {
                this.cts = CancellationTokenSource.CreateLinkedTokenSource(this.destroyCancellationToken);
                var ct = this.cts.Token;

                showInfomatonCaption_(false);

                var ds = await this.DanceSetPlayer.WaitForPlayingAsync;
                ct.ThrowIfCancellationRequested();

                showInfomatonCaption_(true);

                setAudioCaption_(ds);
                setModelCaptions_(ds);
                adjustModelCaptionPosition_(ds);

                await Awaitable.WaitForSecondsAsync(this.DisplayedTimeSec, ct);
            }
            catch (OperationCanceledException)
            {

            }
            finally
            {
                this.cts.Dispose();
                this.cts = null;
            }

            this.Canvas.gameObject.SetActive(false);
            return;


            void showInfomatonCaption_(bool isVisible)
            {
                this.LoadingGroup.gameObject.SetActive(!isVisible);
                this.InfoGroup.gameObject.SetActive(isVisible);
            }

            void setAudioCaption_(DanceSetDefineData ds)
            {
                var title = ds.AudioInformation.Caption;
                var author = ds.AudioInformation.Author != ""
                    ? $"楽曲：{ds.AudioInformation.Author}"// {ds.AudioInformation.Url}"
                    : "";

                this.AudioInfo.text = $"{title}\r\n<indent=1em><size=10%>\r\n<size=50%>{author}";
            }

            void setModelCaptions_(DanceSetDefineData ds)
            {
                this.ModelInfo.text = string.Join("\r\n<size=30%>\r\n",
                    ds.Motions
                        .Select(x =>
                        {
                            var caption = "<size=100%><indent=0>" +
                                string.Join("　/　<size=80%>", x.ModelInformation.Caption, x.AnimationInformation.Caption);

                            var model = x.ModelInformation.Author != ""
                                ? $"造形：{x.ModelInformation.Author}"// {x.ModelInformation.Url}"
                                : "";

                            var anim = x.AnimationInformation.Author != ""
                                ? $"振付：{x.AnimationInformation.Author}"// {x.AnimationInformation.Url}"
                                : "";

                            var author = string.Join("\r\n", new[]
                            {
                                    model, anim,
                            }
                            .Where(x => x != "")
                            .Select(x => $"<size=80%><indent=1em>{x}"));

                            return $"{caption}\r\n{author}";
                        })
                );
            }

            void adjustModelCaptionPosition_(DanceSetDefineData ds)
            {
                var rtf = this.ModelInfo.rectTransform;
                var pw = this.ModelInfo.preferredWidth;
                var rw = rtf.rect.width;
                var dx = pw - rw;
                rtf.offsetMin = new Vector2(rtf.offsetMin.x - dx, rtf.offsetMin.y);

                var _scale = 1.0f - Mathf.Max(ds.Motions.Length - 2, 0) * 0.1f;
                var scale = Mathf.Max(_scale, 0.3f);
                rtf.localScale = new Vector3(scale, scale, 1.0f);
            }
        }

        private void OnDisable()
        {
            this.cts?.Cancel();
        }
    }

}