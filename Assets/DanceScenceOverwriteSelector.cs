using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace AnimLite.Samples
{
    using AnimLite.DancePlayable;
    using AnimLite.Utility;

    public class DanceScenceOverwriteSwitch : MonoBehaviour
    {

        public DanceSetPlayerFromJson DanceScenePlayer;
        public int TargetJsonLevel;

        [FilePath]
        public PathUnit[] JsonOverwriteList;

        public int index;

        public KeyCode ChangeToPrevKey = KeyCode.LeftArrow;
        public KeyCode ChangeToNextKey = KeyCode.RightArrow;

        public bool KeepTimeOnChange;


        private async Task Update()
        {

            if (Input.GetKeyDown(this.ChangeToNextKey))
            {
                clip_(+1);
                await play_();
            }
            if (Input.GetKeyDown(this.ChangeToPrevKey))
            {
                clip_(-1);
                await play_();
            }

            return;


            async Task play_()
            {
                var gr = this.DanceScenePlayer.Graph;

                var isKeepTime =
                    this.KeepTimeOnChange
                    &&
                    gr is not null
                    &&
                    gr?.GetRootPlayableCount() > 0
                    ;
                if (isKeepTime)
                {
                    await changeToNext_WithKeepTimeAsync_();
                    return;
                }

                changeToNext_();
                
                return;


                void changeToNext_()
                {
                    this.DanceScenePlayer.SetEnable(false);
                    this.DanceScenePlayer.JsonFiles[this.TargetJsonLevel] = this.JsonOverwriteList[index];
                    this.DanceScenePlayer.SetEnable(true);
                }

                async ValueTask changeToNext_WithKeepTimeAsync_()
                {
                    var rp = this.DanceScenePlayer.Graph.Value.GetRootPlayable(0);
                    var currentTime = (float)rp.GetTime();

                    changeToNext_();

                    using var _ = await this.DanceScenePlayer.DanceSemapho.WaitAsyncDisposable(default);
                    this.DanceScenePlayer.Graph.Value.Evaluate(currentTime);

                }
            }

            void clip_(int addvalue)
            {
                this.index = (this.index + addvalue + this.JsonOverwriteList.Length) % this.JsonOverwriteList.Length;
            }
        }
    }
}