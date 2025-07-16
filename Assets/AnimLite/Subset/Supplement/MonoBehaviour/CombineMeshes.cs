using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using AnimLite.Utility.Linq;
using NUnit.Framework;
using Unity.Mathematics;

using AnimLite.Geometry;
using AnimLite.Vrm;
using AnimLite.Utility;

public class CombineMeshes : MonoBehaviour
{

    public MeshCombineMode mode;

    public Material meshMaterial;
    public Material skinMaterial;
    public Material blendShapeSkinMaterial;

    public string[] MeshTargetList;
    public string[] MaterialTargetList;

    private void OnEnable()
    {
        var ctl = new CombineTargetList
        {
            Mesh = this.MeshTargetList,
            Material = this.MaterialTargetList,
        };

        switch (this.mode)
        {
            case MeshCombineMode.IntoSingleMesh:
                this.gameObject.CombineMeshes_IntoSingleMesh(meshMaterial, skinMaterial, blendShapeSkinMaterial, ctl);
                break;
            case MeshCombineMode.ByMaterial:
                this.gameObject.CombineMeshes_ByMaterial(ctl);
                break;
            case MeshCombineMode.ByMaterialAndAtlasTextures:
                this.gameObject.CombineMeshes_ByMaterialAndAtlasTextures(ctl);
                break;
            case MeshCombineMode.None:
                break;
        }
        
        this.transform.ApplyBlendShapeToVrmExpression("new skin blend");
    }

}
