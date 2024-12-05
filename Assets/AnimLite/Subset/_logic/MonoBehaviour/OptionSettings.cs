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

        public bool UseAsyncModeForFileStreamApi = DanceSceneLoader.UseAsyncModeForFileStreamApi;

        public bool UseSeaquentialLoading = DanceSceneLoader.UseSeaquentialLoading;

        public DanceSceneLoader.ZipMode ZipLoaderMode = DanceSceneLoader.ZipMode.ParallelOpenMultiFiles;


        public void Awake()
        {
            PathUnit.InitPath();// モノビに依存しないで呼ぶ方法ないの？？

            PathUnit.IsAccessWithinParentPathOnly = this.IsAccessWithinParentPathOnly;

            DanceSceneLoader.UseAsyncModeForFileStreamApi = this.UseAsyncModeForFileStreamApi;

            DanceSceneLoader.UseSeaquentialLoading = this.UseSeaquentialLoading;

            DanceSceneLoader.ZipLoaderMode = this.ZipLoaderMode;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            $"PathUnit.IsAccessWithinParentPathOnly : {PathUnit.IsAccessWithinParentPathOnly}".ShowDebugLog();
            $"DanceSceneLoader.UseAsyncModeForFileStreamApi : {DanceSceneLoader.UseAsyncModeForFileStreamApi}".ShowDebugLog();
            $"DanceSceneLoader.UseSeaquentialLoading : {DanceSceneLoader.UseSeaquentialLoading}".ShowDebugLog();
            $"DanceSceneLoader.ZipLoaderMode : {DanceSceneLoader.ZipLoaderMode}".ShowDebugLog();
            $"PathUnit.PathUnit.CacheFolderPath : {PathUnit.CacheFolderPath}".ShowDebugLog();
            $"PathUnit.DataFolderPath : {PathUnit.DataFolderPath}".ShowDebugLog();
            $"PathUnit.PersistentFolderPath : {PathUnit.PersistentFolderPath}".ShowDebugLog();
            $"PathUnit.ParentPath : {PathUnit.ParentPath}".ShowDebugLog();
#endif
        }
    }
}
