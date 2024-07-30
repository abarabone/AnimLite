using AnimLite.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnimLite.DancePlayable
{
    public class DanceSetPlayerBase : MonoBehaviour
    {
        public virtual Awaitable<DanceSetDefineData> WaitForPlayingAsync { get; }
    }
}
