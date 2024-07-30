using UnityEngine;


namespace AnimLite.DancePlayable
{


    /// <summary>
    /// DanceSetHolder の子として DanceMotionDefine をセットしたいときに使う。
    /// モーション再生時、この GameObject の位置がキャラクターの位置となる。
    /// </summary>
    public class DanceHumanDefine : MonoBehaviour
    {

        [SerializeField]
        public DanceMotionDefine Motion;

    }





}
