using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnimLite
{
    using AnimLite.Utility;

    public class OptionSettings : MonoBehaviour
    {

        public bool IsAccessWithinParentPathOnly;


        public void Awake()
        {
            PathUnit.IsAccessWithinParentPathOnly = this.IsAccessWithinParentPathOnly;
        }
    }
}
