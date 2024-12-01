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

        public DanceSceneLoader.ZipMode ZipLoaderMode = DanceSceneLoader.ZipMode.ParallelOpenMultiFiles;

        public bool UseAsyncModeForFileStreamApi = DanceSceneLoader.UseAsyncModeForFileStreamApi;


        public void Awake()
        {
            PathUnit.InitPath();// ���m�r�Ɉˑ����Ȃ��ŌĂԕ��@�Ȃ��́H�H

            PathUnit.IsAccessWithinParentPathOnly = this.IsAccessWithinParentPathOnly;

            DanceSceneLoader.ZipLoaderMode = this.ZipLoaderMode;

            DanceSceneLoader.UseAsyncModeForFileStreamApi = this.UseAsyncModeForFileStreamApi;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            $"PathUnit.IsAccessWithinParentPathOnly : {PathUnit.IsAccessWithinParentPathOnly}".ShowDebugLog();
            $"DanceSceneLoader.ZipLoaderMode : {DanceSceneLoader.ZipLoaderMode}".ShowDebugLog();
            $"DanceSceneLoader.UseAsyncModeForFileStreamApi : {DanceSceneLoader.UseAsyncModeForFileStreamApi}".ShowDebugLog();
            $"PathUnit.PathUnit.CacheFolderPath : {PathUnit.CacheFolderPath}".ShowDebugLog();
            $"PathUnit.DataFolderPath : {PathUnit.DataFolderPath}".ShowDebugLog();
            $"PathUnit.PersistentFolderPath : {PathUnit.PersistentFolderPath}".ShowDebugLog();
            $"PathUnit.ParentPath : {PathUnit.ParentPath}".ShowDebugLog();
#endif
        }
    }
}
