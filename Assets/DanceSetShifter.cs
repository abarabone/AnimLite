using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnimLite.Samples
{
    using AnimLite.DancePlayable;

    public class DanceSetShifter : MonoBehaviour
    {

        public DanceSetHolder[] holders;

        public int index;


        private void Update()
        {

            if (Input.GetKeyDown(KeyCode.V) || Input.GetKeyDown(KeyCode.Space))
            {
                var go = this.holders[index].gameObject;
                go.SetActive(!go.activeSelf);
            }

            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                clip(+1);
            }
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                clip(-1);
            }

            return;


            void clip(int addvalue)
            {
                var _i = this.index;
                var go_ = this.holders[_i].gameObject;
                go_.SetActive(false);

                this.index = (this.index + addvalue + this.holders.Length) % this.holders.Length;

                var i_ = this.index;
                var go = this.holders[i_].gameObject;
                go.SetActive(true);
            }
        }
    }
}