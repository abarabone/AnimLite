using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnimLite
{
    using AnimLite.Utility;

    public class OptionSettings : MonoBehaviour
    {

        public bool IsAccessWithinParentPathOnly = true;

        public bool IsSeaquentialLoadingInZip = false;


        public void Awake()
        {
            PathUnit.IsAccessWithinParentPathOnly = this.IsAccessWithinParentPathOnly;

            SceneLoadUtilitiy.IsSeaquentialLoadingInZip = this.IsSeaquentialLoadingInZip;
        }
    }
}
