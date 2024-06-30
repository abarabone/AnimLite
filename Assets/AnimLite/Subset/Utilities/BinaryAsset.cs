using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;
using System.IO;

namespace AnimLite.Vmd
{

    public class BinaryAsset : ScriptableObject
    {
        [HideInInspector]
        public byte[] bytes;
    }
#if UNITY_EDITOR
    [ScriptedImporter(version: 3, ext: "vmd")]
    public class ImportScript : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var path = ctx.assetPath;

            var content = File.ReadAllBytes(path);
            var asset = ScriptableObject.CreateInstance<BinaryAsset>();
            asset.bytes = content;

            ctx.AddObjectToAsset(identifier: "MainAsset", obj: asset);
        }
    }
#endif
}

