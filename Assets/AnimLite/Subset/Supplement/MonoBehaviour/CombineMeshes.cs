using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using AnimLite.Utility.Linq;
using NUnit.Framework;
using Unity.Mathematics;

using AnimLite.Geometry;
using AnimLite.Vrm;
using UniVRM10;

public class CombineMeshes : MonoBehaviour
{

    public MeshCombineMode mode;

    public Material meshMaterial;
    public Material skinMaterial;
    public Material blendShapeSkinMaterial;

    private void OnEnable()
    {

        switch (this.mode)
        {
            case MeshCombineMode.IntoSingleMesh:
                this.gameObject.CombineMeshes_IntoSingleMesh(meshMaterial, skinMaterial, blendShapeSkinMaterial);
                break;
            case MeshCombineMode.ByMaterial:
                this.gameObject.CombineMeshes_ByMaterial();
                break;
            case MeshCombineMode.ByMaterialAndAtlasTextures:
                this.gameObject.CombineMeshes_ByMaterialAndAtlasTextures();
                break;
            case MeshCombineMode.None:
                break;
        }
        
        this.transform.ApplyBlendShapeToVrmExpression("new skin blend");
    }

}
