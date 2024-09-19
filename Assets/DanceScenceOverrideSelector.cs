
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnimLite.Samples
{
    using AnimLite.DancePlayable;
    using AnimLite.Utility;

    public class DanceScenceOverrideSwitch : MonoBehaviour
    {

        public DanceSetPlayerFromJson DanceScenePlayer;

        [FilePath]
        public PathUnit[] JsonOverwriteList;

        public int index;


        private void Update()
        {

            if (Input.GetKeyDown(KeyCode.V) || Input.GetKeyDown(KeyCode.Space))
            {
                play_();
            }

            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                clip_(+1);
                play_();
            }
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                clip_(-1);
                play_();
            }

            return;


            void play_()
            {
                this.DanceScenePlayer.SetEnable(false);
                this.DanceScenePlayer.JsonFileOverwrite = this.JsonOverwriteList[index];
                this.DanceScenePlayer.SetEnable(true);
            }

            void clip_(int addvalue)
            {
                this.index = (this.index + addvalue + this.JsonOverwriteList.Length) % this.JsonOverwriteList.Length;
            }
        }
    }
}