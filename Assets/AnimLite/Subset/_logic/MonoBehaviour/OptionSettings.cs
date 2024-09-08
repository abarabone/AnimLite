using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnimLite
{
    using AnimLite.Utility;

    [DefaultExecutionOrder(-1)]
    public class OptionSettings : MonoBehaviour
    {

        public bool IsAccessWithinParentPathOnly = PathUnit.IsAccessWithinParentPathOnly;

        public bool IsSeaquentialLoadingInZip = DanceSceneLoader.IsSeaquentialLoadingInZip;

        public bool UseAsyncModeForFileStreamApi = DanceSceneLoader.UseAsyncModeForFileStreamApi;


        public void Awake()
        {
            PathUnit.InitPath();// モノビに依存しないで呼ぶ方法ないの？？

            PathUnit.IsAccessWithinParentPathOnly = this.IsAccessWithinParentPathOnly;

            DanceSceneLoader.IsSeaquentialLoadingInZip = this.IsSeaquentialLoadingInZip;

            DanceSceneLoader.UseAsyncModeForFileStreamApi = this.UseAsyncModeForFileStreamApi;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            $"PathUnit.IsAccessWithinParentPathOnly : {PathUnit.IsAccessWithinParentPathOnly}".ShowDebugLog();
            $"DanceSceneLoader.IsSeaquentialLoadingInZip : {DanceSceneLoader.IsSeaquentialLoadingInZip}".ShowDebugLog();
            $"DanceSceneLoader.UseAsyncModeForFileStreamApi : {DanceSceneLoader.UseAsyncModeForFileStreamApi}".ShowDebugLog();
            $"PathUnit.PathUnit.CacheFolderPath : {PathUnit.CacheFolderPath}".ShowDebugLog();
            $"PathUnit.DataFolderPath : {PathUnit.DataFolderPath}".ShowDebugLog();
            $"PathUnit.PersistentFolderPath : {PathUnit.PersistentFolderPath}".ShowDebugLog();
            $"PathUnit.ParentPath : {PathUnit.ParentPath}".ShowDebugLog();
#endif
        }
    }
}
