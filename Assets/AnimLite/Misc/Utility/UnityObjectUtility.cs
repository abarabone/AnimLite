using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using Unity.VisualScripting;

namespace AnimLite.Utility
{
    public static class ObjectUtilityExtension
    {

        public static void SetEnable(this MonoBehaviour c, bool isEnabled)
        {
            if (c is null) return;
            
            c.enabled = isEnabled;
        }

        public static T AsUnityNull<T>(this T obj, Func<T, bool?> criteria)
            where T : UnityEngine.Object
        {
            var _obj = obj.AsUnityNull();
            return criteria(_obj) ?? false
                ? _obj
                : null;
        }



        public static async ValueTask DestroyOnMainThreadAsync(this UnityEngine.Object obj)
        {
            await Awaitable.MainThreadAsync();
            obj.Destroy();
        }

        public static async ValueTask DestroyOnMainThreadAsync(this GameObject obj)
        {
            await Awaitable.MainThreadAsync();
            obj.Destroy();
        }


        //public static void Destroy(this UnityEngine.Object obj) =>
        //    UnityEngine.Object.Destroy(obj);
        public static void Destroy(this UnityEngine.Object obj)
        {
        #if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(obj);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        #else
            UnityEngine.Object.Destroy(obj);
        #endif
        }

        //public static void Destroy(this GameObject obj) =>
        //    GameObject.Destroy(obj);
        public static void Destroy(this GameObject obj)
        {
        #if UNITY_EDITOR
            if (Application.isPlaying)
            {
                GameObject.Destroy(obj);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        #else
            GameObject.Destroy(obj);
        #endif
        }


        public static SkinnedMeshRenderer FindFaceRendererIfNothing(this GameObject model, SkinnedMeshRenderer r) =>
            r.IsUnityNull()
                ? model.FindFaceRenderer()
                : r
            ;

        public static SkinnedMeshRenderer FindFaceRenderer(this GameObject model) =>
            model.GetComponentsInChildren<SkinnedMeshRenderer>()
                .FirstOrDefault(r => r.sharedMesh.bindposeCount > 0);
        //{
        //    var tf = anim.transform;

        //    for (var i = 0; i<tf.childCount; i++)
        //    {
        //        var tfc = tf.GetChild(i);

        //        var r = tfc.GetComponent<SkinnedMeshRenderer>();
        //        if (r == null) continue;

        //        if (r.sharedMesh.blendShapeCount > 0) return r;
        //    }

        //    return null;
        //}


        /// <summary>
        /// 最適化で消えることを期待するがダメかも
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ShowDebugLog(this string text)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(text);
#endif
            return text;
        }

    }
}
