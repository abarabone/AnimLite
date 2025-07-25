using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine.Experimental.Rendering;
using UnityEditor;
using Unity.Mathematics.Geometry;
using UnityEngine.Rendering;

namespace AnimLite.Geometry
{
    using AnimLite.Utility.Linq;
    using AnimLite.Utility;

    public enum MeshCombineMode
    {
        None,
        IntoSingleMesh,
        ByMaterial,
        ByMaterialAndAtlasTextures,
    }

    public struct CombineTargetList
    {
        public Wildlist Mesh;
        public Wildlist Material;
    }
    public struct Wildlist
    {
        public Wildcard[] targets_wild { get; private set; }
        public Wildcard[] untargets_wild { get; private set; }

        public IEnumerable<string> targetlist_input
        {
            set
            {
                this.targets_wild = value
                    .Where(x => !x.StartsWith('!'))// || x.StartsWith(@"\!"))// \! はエスケープで ! に置き換わるので考えなくてよい
                    .Select(x => x.ToWildcardEscaped())
                    .ToArray();
                this.untargets_wild = value
                    .Where(x => x.StartsWith('!'))// || x.StartsWith(@"!\!"))// \! はエスケープで ! に置き換わるので考えなくてよい
                    .Select(x => x[1..].ToWildcardEscaped())
                    .ToArray();
            }
        }
        public static implicit operator Wildlist(string[] src) => new Wildlist
        {
            targetlist_input = src,
        };

        public bool IsTarget(string name)
        {
            if (this.untargets_wild is not null)
            {
                var isUntarget = this.untargets_wild.Any(t => name.Like(t));
                if (isUntarget) return false;
            }

            if (this.targets_wild is not null)
            {
                var isTarget = this.targets_wild.IsEmpty() || this.targets_wild.Any(t => name.Like(t));
                return isTarget;
            }

            return true;
        }
    }


    public static class MeshCombiner
    {

        public static GameObject CombineMeshes_IntoSingleMesh(
            this GameObject go, CombineTargetList targetList = default)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            return go.combineMeshes_IntoSingleMesh(null, null, null, default, rs);
        }

        public static GameObject CombineMeshes_IntoSingleMesh(
            this GameObject go,
            string meshMaterialName = null,
            string skinMaterialName = null,
            string blendShapeMaterialName = null,
            CombineTargetList targetList = default)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var qmat = rs.SelectMany(x => x.sharedMaterials);

            var mname = meshMaterialName;
            var sname = skinMaterialName != mname ? skinMaterialName : null;
            var bname = blendShapeMaterialName != mname ? blendShapeMaterialName : null;

            return go.combineMeshes_IntoSingleMesh(
                getMaterial_(mname), getMaterial_(sname), getMaterial_(bname), targetList, rs);


            Material getMaterial_(string name)
            {
                if (name is null) return null;

                var mat = qmat.FirstOrDefault(mat => mat.name.ToLower().Like(name.ToLower()));
                if (mat is not null)
                {
                    return Material.Instantiate(mat);
                }

                return RendererUtility.CreateMaterial(name);
            }
        }

        public static GameObject CombineMeshes_IntoSingleMesh(
            this GameObject go,
            Material meshMaterial = null,
            Material skinMaterial = null,
            Material blendShapeMaterial = null,
            CombineTargetList targetList = default)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            return go.combineMeshes_IntoSingleMesh(
                meshMaterial, skinMaterial, blendShapeMaterial, targetList, rs);
        }

        static GameObject combineMeshes_IntoSingleMesh(
            this GameObject go,
            Material meshMaterial,
            Material skinMaterial,
            Material blendShapeMaterial,
            CombineTargetList targetList,
            IEnumerable<Renderer> rs)
        {
            //var rs = go.GetComponentsInChildren<Renderer>();
            var target_rs = rs.filterRenderer(targetList);
            var target_mats = target_rs.FilterMaterials(targetList).ToArray();
            var (ctex, ctexToUvRectList) = target_mats.PackTexture(TextureUtility.GetColorTexture2D);
            var (ntex, ntexToUvRectList) = target_mats.PackTexture(TextureUtility.GetNormalTexture2D);
            var texToUvRectDict = ctexToUvRectList.Concat(ntexToUvRectList).ToDictionary();

            var meshes = target_rs.OfType<MeshRenderer>();
            var stdmat = meshMaterial.AsUnityNull() ?? default_mat_();
            combine_mesh_("new mesh", meshes, stdmat);

            var qbody = target_rs.OfType<SkinnedMeshRenderer>().Where(x => x.sharedMesh.blendShapeCount == 0);
            var bodymat = skinMaterial.AsUnityNull() ?? stdmat;
            combine_skin_("new skin", qbody, bodymat);

            var qface = target_rs.OfType<SkinnedMeshRenderer>().Where(x => x.sharedMesh.blendShapeCount > 0);
            var facemat = blendShapeMaterial.AsUnityNull() ?? stdmat;
            combine_skin_("new skin blend", qface, facemat).AddBlendShapes(qface, targetList);

            rs.HideRenderer();
            return go;


            static Material default_mat_() => RendererUtility.CreateMaterial("Unlit/Texture");

            Mesh combine_skin_(string name, IEnumerable<SkinnedMeshRenderer> smrs, Material mat)
            {
                if (smrs.IsEmpty()) return null;

                var (bones, bindposes) = smrs.buildBonesAndBindposes();
                var mesh = smrs.CombineMeshesIntoSingleMesh(targetList, texToUvRectDict, bones, bindposes, go);
                mat.SetColorTexture2D(ctex);
                mat.SetNormalTexture2D(ntex);

                mesh.name = name;
                go.CreateSkinRendererObject(mesh, new[] {mat}, bones, smrs);
                return mesh;
            }
            void combine_mesh_(string name, IEnumerable<MeshRenderer> mrs, Material mat)
            {
                if (mrs.IsEmpty()) return;

                var mesh = mrs.CombineMeshesIntoSingleMesh(targetList, texToUvRectDict, go);
                mat.SetColorTexture2D(ctex);
                mat.SetNormalTexture2D(ntex);

                mesh.name = name;
                go.CreateRendererObject(mesh, new[] {mat});
            }
        }


        public static GameObject CombineMeshes_ByMaterial(
            this GameObject go, CombineTargetList targetList = default)
        {
            var rs = go.GetComponentsInChildren<Renderer>();

            var meshes = rs.OfType<MeshRenderer>();
            combine_mesh_("new mesh", meshes);

            var bodies = rs.OfType<SkinnedMeshRenderer>().Where(x => x.sharedMesh.blendShapeCount == 0);
            combine_skin_("new skin", bodies);

            var faces = rs.OfType<SkinnedMeshRenderer>().Where(x => x.sharedMesh.blendShapeCount > 0);
            combine_skin_("new skin blend", faces).AddBlendShapes(faces, targetList);

            rs.HideRenderer();
            return go;


            Mesh combine_skin_(string name, IEnumerable<SkinnedMeshRenderer> smrs)
            {
                if (smrs.IsEmpty()) return null;

                var (bones, bindposes) = smrs.buildBonesAndBindposes();
                var (mesh, mats) = smrs.CombineMeshesByMaterial(targetList, bones, bindposes, go);

                mesh.name = name;
                go.CreateSkinRendererObject(mesh, mats, bones, smrs);
                return mesh;
            }
            void combine_mesh_(string name, IEnumerable<MeshRenderer> mrs)
            {
                if (mrs.IsEmpty()) return;

                var (mesh, mats) = mrs.CombineMeshesByMaterial(targetList, go);
                
                mesh.name = name;
                go.CreateRendererObject(mesh, mats);
            }
        }

        public static GameObject CombineMeshes_ByMaterialAndAtlasTextures(
            this GameObject go, CombineTargetList targetList = default)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var target_rs = rs.filterRenderer(targetList);
            var target_mats = target_rs.FilterMaterials(targetList).ToArray();
            var (ctex, ctexToUvRectList) = target_mats.PackTexture(TextureUtility.GetColorTexture2D);
            var (ntex, ntexToUvRectList) = target_mats.PackTexture(TextureUtility.GetNormalTexture2D);
            var texToUvRectDict = ctexToUvRectList.Concat(ntexToUvRectList).ToDictionary();

            var meshes = target_rs.OfType<MeshRenderer>();
            combine_mesh_("new mesh", meshes);

            var bodies = target_rs.OfType<SkinnedMeshRenderer>().Where(x => x.sharedMesh.blendShapeCount == 0);
            combine_skin_("new skin", bodies);

            var faces = target_rs.OfType<SkinnedMeshRenderer>().Where(x => x.sharedMesh.blendShapeCount > 0);
            combine_skin_("new skin blend", faces).AddBlendShapes(faces, targetList);

            rs.HideRenderer();
            return go;


            Mesh combine_skin_(string name, IEnumerable<SkinnedMeshRenderer> smrs)
            {
                if (smrs.IsEmpty()) return null;

                var (bones, bindposes) = smrs.buildBonesAndBindposes();
                var (mesh, mats) = smrs.CombineMeshesByMaterial(targetList, texToUvRectDict, bones, bindposes, go);
                mats.ForEach(mat => mat.SetColorTexture2D(ctex));
                mats.ForEach(mat => mat.SetNormalTexture2D(ntex));

                mesh.name = name;
                go.CreateSkinRendererObject(mesh, mats, bones, smrs);
                return mesh;
            }
            void combine_mesh_(string name, IEnumerable<MeshRenderer> mrs)
            {
                if (mrs.IsEmpty()) return;

                var (mesh, mats) = mrs.CombineMeshesByMaterial(targetList, texToUvRectDict, go);
                mats.ForEach(mat => mat.SetColorTexture2D(ctex));
                mats.ForEach(mat => mat.SetNormalTexture2D(ntex));

                mesh.name = name;
                go.CreateRendererObject(mesh, mats);
            }
        }

    }


    struct SrcModelData
    {
        public Mesh mesh;
        public Material[] mats;
        public Transform[] bones;
        public Matrix4x4 mt;
    }



    public static class MeshCombineUtility
    {

        public static Mesh CombineMeshesIntoSingleMesh(
            this IEnumerable<Renderer> rs, CombineTargetList targetList,
            Dictionary<Texture2D, Rect> texToUvRectDict,
            GameObject baseobj)
        {
            var mtinvbase = baseobj.transform.worldToLocalMatrix;
            var qmmb =
                from r in rs
                select new SrcModelData
                {
                    mesh = r.GetComponent<MeshFilter>().sharedMesh,
                    mats = r.sharedMaterials,
                    bones = null,
                    mt = mtinvbase * r.localToWorldMatrix,
                };

            return qmmb.combineMeshesIntoSingleMesh(
                targetList,
                getcuvs: mat => mat.GetColorTexture2D()?.tex_to_rect(texToUvRectDict),
                getnuvs: mat => mat.GetNormalTexture2D()?.tex_to_rect(texToUvRectDict),
                getweights: (bones, wi) => 0,
                null,
                baseobj);
        }
        public static Mesh CombineMeshesIntoSingleMesh(
            this IEnumerable<SkinnedMeshRenderer> smrs, CombineTargetList targetList,
            Dictionary<Texture2D, Rect> texToUvRectDict,
            Transform[] bones,
            Matrix4x4[] bindposes,
            GameObject baseobj)
        {
            var mtinvbase = baseobj.transform.worldToLocalMatrix;
            var qmmb =
                from smr in smrs
                select new SrcModelData
                {
                    mesh = smr.sharedMesh,
                    mats = smr.sharedMaterials,
                    bones = smr.bones,
                    mt = mtinvbase * smr.localToWorldMatrix,
                };

            var boneToIndex = bones.buildBoneToIndexDict();

            return qmmb.combineMeshesIntoSingleMesh(
                targetList,
                getcuvs: mat => mat.GetColorTexture2D()?.tex_to_rect(texToUvRectDict),
                getnuvs: mat => mat.GetNormalTexture2D()?.tex_to_rect(texToUvRectDict),
                getweights: (bones, wi) => boneToIndex[bones[wi]],
                bindposes,
                baseobj);
        }


        static Mesh combineMeshesIntoSingleMesh(
            this IEnumerable<SrcModelData> srcmmbs, CombineTargetList targetList,
            Func<Material, Rect?> getcuvs,
            Func<Material, Rect?> getnuvs,
            Func<Transform[], int, int> getweights,
            Matrix4x4[] bindposes,
            GameObject baseobj)
        {
            var mmbs = srcmmbs
                .Where(x => targetList.Mesh.IsTarget(x.mesh.name))
                .Do(x => Debug.Log(x.mesh.name))
                .ToArray();

            var hasWeithts = mmbs.Select(x => x.mesh).HasWeights();
            var hasUv0s = mmbs.Select(x => x.mesh).HasUvs(0);
            var hasUv1s = mmbs.Select(x => x.mesh).HasUvs(1);
            var hasTangents = mmbs.Select(x => x.mesh).HasTangents();
            var hasColors = mmbs.Select(x => x.mesh).HasColors();

            var qvtxofs = mmbs
                .Scan(0, (pre, mmb) => pre + mmb.mesh.vertexCount)
                .Prepend(0);

            var qmesh =
                from x in (mmbs, qvtxofs).Zip()

                let mesh = x.Item1.mesh
                let mats = x.Item1.mats
                let bones = x.Item1.bones
                let mt = x.Item1.mt
                let vtxofs = x.Item2

                let idxs = mesh.triangles
                let idxs_ = filter_index_(mesh, mats, idxs, vtxofs).ToArray()

                let tangents = hasTangents ? mesh.tangents : null

                let cuvs = hasUv0s ? mesh.calc_uvs(0, idxs, i => getcuvs(mats[i])).ToArray() : null

                let nuvs = hasUv1s ? mesh.calc_uvs(1, idxs, i => getnuvs(mats[i])).ToArray() : null

                let weights = hasWeithts ? mesh.calc_weights(wi => getweights(bones, wi)) : null

                let cols = hasColors ? mesh.calc_cols(mats, idxs) : null

                let vtxs = mt.isIdentity
                    ? mesh.vertices
                    : mesh.vertices.Select(v => mt.MultiplyPoint3x4(v)).ToArray()

                select (
                    idxs: idxs_,//.Select(x => x + vtxofs),
                    vtxs,
                    mesh.normals,
                    tangents,
                    cuvs,
                    nuvs,
                    weights,
                    cols
                );


            var newmesh = new Mesh();

            newmesh.vertices = qmesh.SelectMany(x => x.vtxs).ToArray();
            newmesh.normals = qmesh.SelectMany(x => x.normals).ToArray();
            if (hasTangents) newmesh.tangents = qmesh.SelectMany(x => x.tangents).ToArray();
            if (hasUv0s) newmesh.uv = qmesh.SelectMany(x => x.cuvs).ToArray();
            if (hasUv1s) newmesh.uv2 = qmesh.SelectMany(x => x.nuvs).ToArray();
            if (hasWeithts) newmesh.boneWeights = qmesh.SelectMany(x => x.weights).ToArray();
            if (hasColors) newmesh.colors32 = qmesh.SelectMany(x => x.cols).ToArray();

            newmesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            newmesh.triangles = qmesh.SelectMany(x => x.idxs).ToArray();

            newmesh.bindposes = bindposes;

            newmesh.RecalculateBounds();
            return newmesh;


            IEnumerable<int> filter_index_(Mesh mesh, Material[] mats, int[] idxs, int vofs) =>
                from isub in Enumerable.Range(0, mats.Length)
                let mat = mats[isub]
                where targetList.Material.IsTarget(mat.name)

                from idx in mesh.idx_subs(isub, idxs, vofs)

                select idx;
        }

    }
    

    static class MeshSubsCombinerUtility
    {

        public static (Mesh, Material[]) CombineMeshesByMaterial(
            this IEnumerable<Renderer> rs, CombineTargetList targetList,
            GameObject baseobj)
        {
            //var qmmb = rs
            //    .Select(r =>
            //        (mesh: r.GetComponent<MeshFilter>().sharedMesh, mats: r.sharedMaterials, bones: null as Transform[]))
            //    //.Do(x => Debug.Log($"{x.mesh.boneWeights?.Length} {x.mesh.uv2?.Length}"))
            //    ;
            var mtinvbase = baseobj.transform.worldToLocalMatrix;
            var qmmb =
                from r in rs
                select new SrcModelData
                {
                    mesh = r.GetComponent<MeshFilter>().sharedMesh,
                    mats = r.sharedMaterials,
                    bones = null,
                    mt = mtinvbase * r.localToWorldMatrix,
                };

            return qmmb.combineMeshesByMaterial(
                targetList,
                getcuvs: mat => default,
                getnuvs: mat => default,
                getweights: (bones, wi) => 0,
                null,
                baseobj);
        }
        public static (Mesh, Material[]) CombineMeshesByMaterial(
            this IEnumerable<SkinnedMeshRenderer> smrs, CombineTargetList targetList,
            Transform[] bones,
            Matrix4x4[] bindposes,
            GameObject baseobj)
        {
            //var qmmb = smrs
            //    .Select(smr => (mesh: smr.sharedMesh, mats: smr.sharedMaterials, bones: smr.bones))
            //    //.Do(x => Debug.Log($"{x.mesh.boneWeights?.Length} {x.mesh.uv2?.Length}"))
            //    ;
            var mtinvbase = baseobj.transform.worldToLocalMatrix;
            var qmmb =
                from smr in smrs
                select new SrcModelData
                {
                    mesh = smr.sharedMesh,
                    mats = smr.sharedMaterials,
                    bones = smr.bones,
                    mt = mtinvbase * smr.localToWorldMatrix,
                };

            var boneToIndex = bones.buildBoneToIndexDict();

            return qmmb.combineMeshesByMaterial(
                targetList,
                getcuvs: mat => default,
                getnuvs: mat => default,
                getweights: (bones, wi) => boneToIndex[bones[wi]],
                bindposes,
                baseobj);
        }


        public static (Mesh, Material[]) CombineMeshesByMaterial(
            this IEnumerable<Renderer> rs, CombineTargetList targetList,
            Dictionary<Texture2D, Rect> texToUvRectDict,
            GameObject baseobj)
        {
            //var qmmb = rs
            //    .Select(r =>
            //        (mesh: r.GetComponent<MeshFilter>().sharedMesh, mats: r.sharedMaterials, bones: null as Transform[]))
            //    //.Do(x => Debug.Log($"{x.mesh.boneWeights?.Length} {x.mesh.uv2?.Length}"))
            //    ;
            var mtinvbase = baseobj.transform.worldToLocalMatrix;
            var qmmb =
                from r in rs
                select new SrcModelData
                {
                    mesh = r.GetComponent<MeshFilter>().sharedMesh,
                    mats = r.sharedMaterials,
                    bones = null,
                    mt = mtinvbase * r.localToWorldMatrix,
                };

            return qmmb.combineMeshesByMaterial(
                targetList,
                getcuvs: mat => mat.GetColorTexture2D()?.tex_to_rect(texToUvRectDict),
                getnuvs: mat => mat.GetNormalTexture2D()?.tex_to_rect(texToUvRectDict),
                getweights: (bones, wi) => 0,
                null,
                baseobj);
        }
        public static (Mesh, Material[]) CombineMeshesByMaterial(
            this IEnumerable<SkinnedMeshRenderer> smrs, CombineTargetList targetList,
            Dictionary<Texture2D, Rect> texToUvRectDict,
            Transform[] bones,
            Matrix4x4[] bindposes,
            GameObject baseobj)
        {
            //var qmmb = smrs
            //    .Select(smr => (mesh: smr.sharedMesh, mats: smr.sharedMaterials, bones: smr.bones))
            //    //.Do(x => Debug.Log($"{x.mesh.boneWeights?.Length} {x.mesh.uv2?.Length}"))
            //    ;
            var mtinvbase = baseobj.transform.worldToLocalMatrix;
            var qmmb =
                from smr in smrs
                select new SrcModelData
                {
                    mesh = smr.sharedMesh,
                    mats = smr.sharedMaterials,
                    bones = smr.bones,
                    mt = mtinvbase * smr.localToWorldMatrix,
                };

            var boneToIndex = bones.buildBoneToIndexDict();

            return qmmb.combineMeshesByMaterial(
                targetList,
                getcuvs: mat => mat.GetColorTexture2D()?.tex_to_rect(texToUvRectDict),
                getnuvs: mat => mat.GetNormalTexture2D()?.tex_to_rect(texToUvRectDict),
                getweights: (bones, wi) => boneToIndex[bones[wi]],
                bindposes,
                baseobj);
        }


        static (Mesh, Material[]) combineMeshesByMaterial(
            this IEnumerable<SrcModelData> srcmmbs, CombineTargetList targetList,
            Func<Material, Rect?> getcuvs,
            Func<Material, Rect?> getnuvs,
            Func<Transform[], int, int> getweights,
            Matrix4x4[] bindposes,
            GameObject baseobj)
        {
            var mmbs = srcmmbs
                .Where(x => targetList.Mesh.IsTarget(x.mesh.name))
                .Do(x => Debug.Log(x.mesh.name))
                .ToArray();

            var hasWeithts = mmbs.Select(x => x.mesh).HasWeights();
            var hasUv0s = mmbs.Select(x => x.mesh).HasUvs(0);
            var hasUv1s = mmbs.Select(x => x.mesh).HasUvs(1);
            var hasTangents = mmbs.Select(x => x.mesh).HasTangents();
            var hasColors = mmbs.Select(x => x.mesh).HasColors();

            var qmeshvtx =
                from mmb in mmbs

                let mesh = mmb.mesh
                let mats = mmb.mats
                let bones = mmb.bones
                let mt = mmb.mt

                let idxs = mesh.triangles

                let tangents = hasTangents ? mesh.tangents : null

                let cuvs = hasUv0s ? mesh.calc_uvs(0, idxs, i => getcuvs(mats[i])).ToArray() : null

                let nuvs = hasUv1s ? mesh.calc_uvs(1, idxs, i => getnuvs(mats[i])).ToArray() : null

                let weights = hasWeithts ? mesh.calc_weights(wi => getweights(bones, wi)) : null

                let cols = hasColors ? mesh.calc_cols(mats, idxs) : null

                let vtxs = mt.isIdentity
                    ? mesh.vertices
                    : mesh.vertices.Select(v => mt.MultiplyPoint3x4(v)).ToArray()

                select (
                    vtxs,
                    mesh.normals,
                    tangents,
                    cuvs,
                    nuvs,
                    weights,
                    cols
                );

            var qvtxofs = mmbs
                .Scan(0, (pre, mmb) => pre + mmb.mesh.vertexCount)
                .Prepend(0);

            var qsubmesh =
                from x in (mmbs, qvtxofs).Zip()

                let mesh = x.Item1.mesh
                let mats = x.Item1.mats
                let vtxofs = x.Item2

                let idxs = mesh.triangles
                //let idxs_ = filter_index_(mesh, mats, idxs, vtxofs).ToArray()

                from i in Enumerable.Range(0, mesh.subMeshCount)
                let mat = mats[i]
                where targetList.Material.IsTarget(mat.name)
                group
                    mesh.idx_subs(i, idxs, vtxofs)
                by
                    mat;// s[i];


            var newmats = qsubmesh.Select(x => new Material(x.Key)).ToArray();

            var newmesh = new Mesh();

            newmesh.vertices = qmeshvtx.SelectMany(x => x.vtxs).ToArray();
            newmesh.normals = qmeshvtx.SelectMany(x => x.normals).ToArray();
            if (hasTangents) newmesh.tangents = qmeshvtx.SelectMany(x => x.tangents).ToArray();
            if (hasUv0s) newmesh.uv = qmeshvtx.SelectMany(x => x.cuvs).ToArray();
            if (hasUv1s) newmesh.uv2 = qmeshvtx.SelectMany(x => x.nuvs).ToArray();
            if (hasWeithts) newmesh.boneWeights = qmeshvtx.SelectMany(x => x.weights).ToArray();
            if (hasColors) newmesh.colors32 = qmeshvtx.SelectMany(x => x.cols).ToArray();

            newmesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            newmesh.subMeshCount = newmats.Length;
            foreach (var (x, i) in qsubmesh.Select((x, i) => (x, i)))
            {
                //f.WriteLine(x.Key);
                var qidx = x.SelectMany(x => x);//.Do(x => f.WriteLine(x)).ToArray();
                newmesh.SetTriangles(qidx.ToArray(), i);
            }

            newmesh.bindposes = bindposes;

            newmesh.RecalculateBounds();
            return (newmesh, newmats);


            //IEnumerable<int> filter_index_(Mesh mesh, Material[] mats, int[] idxs, int vofs) =>
            //    from isub in Enumerable.Range(0, mats.Length)
            //    let mat = mats[isub]
            //    where targetList.Material.IsTarget(mat.name)

            //    from idx in mesh.idx_subs(isub, idxs, vofs)

            //    select idx;
        }

    }



    public static class SkinMeshUtility
    {


        public static (Transform[] bones, Matrix4x4[] bindposes) buildBonesAndBindposes(
            this IEnumerable<SkinnedMeshRenderer> smrs)
        {
            var arr = smrs
                .SelectMany(x => (x.bones, x.sharedMesh.bindposes).Zip())
                .Distinct(x => x.Item1)
                .ToArray();

            return (arr.Select(x => x.Item1).ToArray(), arr.Select(x => x.Item2).ToArray());
        }

        public static Dictionary<Transform, int> buildBoneToIndexDict(
            this Transform[] bones)
        =>
            bones
                .Select((x, i) => (bone: x, i))
                .ToDictionary(x => x.bone, x => x.i);




        public static Mesh AddBlendShapes(
            this Mesh dstmesh, IEnumerable<SkinnedMeshRenderer> smrs, CombineTargetList targetList)
        {;
            if (dstmesh is null) return null;

            var names = smrs
                .Select(smr => smr.sharedMesh)
                .SelectMany(mesh =>
                    from i in Enumerable.Range(0, mesh.blendShapeCount)
                    select mesh.GetBlendShapeName(i))
                .Distinct();

            var q =
                from name in names
                select (
                    name,
                    vtxs: smrs.SelectMany(smr => blendShapeVertices_(smr.sharedMesh, name).Zip())
                );
            foreach (var x in q)
            {
                var v = x.vtxs.Select(x => x.Item1).ToArray();
                var n = x.vtxs.Select(x => x.Item2).ToArray();
                var t = x.vtxs.Select(x => x.Item3).ToArray();
                dstmesh.AddBlendShapeFrame(x.name, 100, v, n, t);
            }
            return dstmesh;

            static (Vector3[] v, Vector3[] n, Vector3[] t) blendShapeVertices_(Mesh mesh, string name)
            {
                var vlen = mesh.vertexCount;
                var v = new Vector3[vlen];
                var n = new Vector3[vlen];
                var t = new Vector3[vlen];
                var i = mesh.GetBlendShapeIndex(name);
                if (i == -1) return (v, n, t);

                mesh.GetBlendShapeFrameVertices(i, 0, v, n, t);
                return (v, n, t);
            }
        }
    }

    static class MeshElementAccessUtility
    {
        public static Rect? tex_to_rect(this Texture2D tex, Dictionary<Texture2D, Rect> texToUvRectDict) =>
            texToUvRectDict?.TryGetValue(tex, out var rect) ?? false
                ? rect
                : null
                ;



        public static bool HasTangents(this IEnumerable<Mesh> meshes) =>
            meshes.Any(mesh => mesh.HasVertexAttribute(VertexAttribute.Tangent));



        public static bool HasWeights(this IEnumerable<Mesh> meshes) =>
            meshes.Any(mesh => mesh.HasVertexAttribute(VertexAttribute.BlendWeight));

        public static IEnumerable<BoneWeight> calc_weights(
            //this Dictionary<Transform, int> boneToIndex, Transform[] smrbones, BoneWeight[] weights)
            this Mesh mesh, Func<int, int> conv_wi)
        {
            //var attr = VertexAttribute.BlendWeight;
            //if (!mesh.HasVertexAttribute(attr)) return null;

            return
                from w in mesh.boneWeights
                select new BoneWeight
                {
                    boneIndex0 = conv_wi(w.boneIndex0),
                    boneIndex1 = conv_wi(w.boneIndex1),
                    boneIndex2 = conv_wi(w.boneIndex2),
                    boneIndex3 = conv_wi(w.boneIndex3),
                    weight0 = w.weight0,
                    weight1 = w.weight1,
                    weight2 = w.weight2,
                    weight3 = w.weight3,
                };
        }


        public static bool HasUvs(this IEnumerable<Mesh> meshes, int uvchannel) =>
            meshes.Any(mesh => mesh.HasVertexAttribute(VertexAttribute.TexCoord0 + uvchannel));

        public static Vector2[] calc_uvs(
            this Mesh mesh, int uvchannel, int[] idxs, Func<int, Rect?> getuvrect)
        {
            //var attr = VertexAttribute.TexCoord0 + uvchannel;
            //if (!mesh.HasVertexAttribute(attr)) return null;

            var uvs = new List<Vector2>(mesh.vertexCount);
            mesh.GetUVs(uvchannel, uvs);

            var qnewuv =
                from i in Enumerable.Range(0, mesh.subMeshCount)

                let rect = getuvrect(i)
                where rect is not null

                from idx in mesh.idx_subs(i, idxs)
                let uv = uvs[idx]

                select (idx, newuv: uv.ScaleUv(rect.Value))
                ;

            foreach (var (idx, uv) in qnewuv.ToArray())
            {
                uvs[idx] = uv;
            }
            return uvs.ToArray();
        }


        public static bool HasColors(this IEnumerable<Mesh> meshes) =>
            meshes.Any(mesh => mesh.HasVertexAttribute(VertexAttribute.Color));

        public static IEnumerable<Color32> calc_cols(this Mesh mesh, Material[] mats, int[] idxs)
        {
            //var attr = VertexAttribute.Color;
            //if (!mesh.HasVertexAttribute(attr)) return null;

            var cols = mesh.colors32;

            var q =
                from i in Enumerable.Range(0, mesh.subMeshCount)
                
                let matcolor = (Color)mats[i].color

                from idx in mesh.idx_subs(i, idxs)

                let newcol = (Color32)(cols[idx] * matcolor)
                
                select(idx, newcol)
                ;

            foreach (var (idx, col) in q.ToArray())
            {
                cols[idx] = col;
            }
            return cols;
        }

        public static IEnumerable<int> idx_subs(this Mesh mesh, int isub, int[] idxs, int indexOffset = 0)
        {
            var desc = mesh.GetSubMesh(isub);
            var ist = desc.indexStart;
            var ied = ist + desc.indexCount;
            for (var i = ist; i < ied; i++)
            {
                yield return idxs[i] + indexOffset;
            }
            //return
            //    from idx in idxs[ist..ied]
            //    select idx + indexOffset //+ desc.baseVertex
            //    ;
        }



    }


    static class FilterUtility
    {

        public static (IEnumerable<T>, IEnumerable<U>) Concat<T, U>(
            this (IEnumerable<T>, IEnumerable<U>) src1, (IEnumerable<T>, IEnumerable<U>) src2)
        =>
            (src1.Item1.Concat(src2.Item1), src1.Item2.Concat(src2.Item2));



        public static IEnumerable<T> filterRenderer<T>(
            this IEnumerable<T> src, CombineTargetList targetList)
                where T : Renderer
        =>
            src
                .Where(x => targetList.Mesh.IsTarget(x.name))
                .Do(x => Debug.Log(x.name))
                ;//.ToArray();


        //public static IEnumerable<Material> FilterMaterials<T>(
        //    this IEnumerable<T> src, CombineTargetList targetlist)
        //        where T : Renderer
        //=>
        //    src
        //        .Where(r => targetlist.Mesh.IsTarget(r.name))
        //        .FilterMaterials(targetlist);


        public static IEnumerable<Material> FilterMaterials(
            this IEnumerable<Renderer> rs, CombineTargetList targetlist)
        =>
            from r in rs
            from mat in r.sharedMaterials
            where targetlist.Material.IsTarget(mat.name)
            select mat
            ;

    }


    static class TextureUtility
    {


        //public static (Texture2D tex, Dictionary<Texture2D, Rect> texToUvRectDict) PackTexture(
        public static (Texture2D tex, (Texture2D[], Rect[]) texToUvRectList) PackTexture(
            this IEnumerable<Renderer> rs, Func<Material, Texture2D> gettex)
        =>
            rs
                .SelectMany(r => r.sharedMaterials)
                .PackTexture(gettex);

        public static (Texture2D tex, (Texture2D[], Rect[]) texToUvRectList) PackTexture(
            this IEnumerable<Material> mats, Func<Material, Texture2D> gettex)
        {
            var qtex =
                from mat in mats
                let tex = gettex(mat)
                where tex is not null
                select tex
                ;
            var texs = qtex.Distinct().ToArray();

            var (texmain, uvrects) = texs.ToAtlasOrOriginal();

            return (texmain, (texs, uvrects));
        }


        static public Vector2 ScaleUv(this Vector2 uv, Rect rect) =>
            new Vector2
            {
                x = rect.x + (1.0f + uv.x) % 1 * rect.width,
                y = rect.y + (1.0f + uv.y) % 1 * rect.height,
            };


        static public (Texture2D atlas, Rect[] uvRects) ToAtlas(this Texture2D[] srcTextures)
        {
            if (!srcTextures.Any()) return (null, new Rect[] {});

            //var dstTexture = new Texture2D( 0, 0 );
            var dstTexture = new Texture2D(
                width: 1, height: 1, textureFormat: TextureFormat.ARGB32, mipChain: true);

            var uvRects = dstTexture.PackTextures(
                srcTextures, padding: 0, maximumAtlasSize: 4096, makeNoLongerReadable: true);

            return (dstTexture, uvRects);
        }
        

        static public (Texture2D atlas, Rect[] uvRects) ToAtlasOrOriginal(this Texture2D[] srcTextures)
        =>
            srcTextures.HasSingleElement()
                ? (srcTextures.First(), new Rect[] { new Rect(0, 0, 1, 1) })
                : srcTextures.ToAtlas();



        public static Texture2D GetColorTexture2D(this Material mat) =>
            mat.getTex2d("_MainTex") ?? mat.getTex2d("_ShadeTex");

        public static Texture2D GetNormalTexture2D(this Material mat) =>
            mat.getTex2d("_NormalTex") ?? mat.getTex2d("_BumpTex");

        static Texture2D getTex2d(this Material mat, string shadername) =>
            mat.HasProperty(shadername)
                ? mat.GetTexture(shadername) as Texture2D
                : null;


        //public static Material SetColorTexture2D(this Material mat, Texture2D tex) =>
            //mat.setTex2d("_MainTex", tex) ?? mat.setTex2d("_ShadeTex", tex);
        public static Material SetColorTexture2D(this Material mat, Texture2D tex)
        {
            mat.setTex2d("_MainTex", tex);
            mat.setTex2d("_ShadeTex", tex);
            return mat;
        }
        public static Material SetNormalTexture2D(this Material mat, Texture2D tex) =>
            mat.setTex2d("_NormalTex", tex) ?? mat.setTex2d("_BumpTex", tex);

        static Material setTex2d(this Material mat, string shadername, Texture2D tex)
        {
            if (!mat.HasProperty(shadername)) return null;

            mat.SetTexture(shadername, tex);
            return mat;
        }
                


    }


    public static class RendererUtility
    {


        public static void HideRenderer(this IEnumerable<Renderer> rs)
        {
            //rs.ForEach(x => x.enabled = false);
            rs.ForEach(x => x.gameObject.SetActive(false));
        }


        //public static void CreateRenderer(
        //    this GameObject parent, Mesh mesh, Material mat, Transform[] bones, IEnumerable<Renderer> prevsmrs)
        //=>
        //    parent.CreateRenderer(mesh, new[] { mat }, bones, prevsmrs);

        //public static void CreateRenderer(
        //    this GameObject parent, Mesh mesh, Material[] mats, Transform[] bones, IEnumerable<Renderer> prevrs)
        //{
        //    var go = bones is not null
        //        ? createObject_SkinnedMesh_()
        //        : createObject_Mesh_()
        //        ;

        //    go.transform.SetParent(parent.transform, worldPositionStays: false);
        //    return;


        //}
        public static void CreateSkinRendererObject(
            this GameObject parent,
            Mesh mesh, Material[] mats, Transform[] bones, IEnumerable<SkinnedMeshRenderer> prevrs)
        {
            var go = new GameObject(mesh.name);

            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.rootBone = prevrs.First().rootBone?.findRoot(parent.transform);
            smr.bones = bones;
            smr.sharedMesh = mesh;
            smr.sharedMaterials = mats;
            //smr.localBounds = prevrs.Cast<SkinnedMeshRenderer>().CalcLocalBounds(smr.rootBone);

            go.transform.SetParent(parent.transform, worldPositionStays: false);
        }
        public static void CreateRendererObject(
            this GameObject parent, Mesh mesh, Material[] mats)
        {
            var go = new GameObject(mesh.name);

            var mr = go.AddComponent<MeshRenderer>();
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            mr.sharedMaterials = mats;

            go.transform.SetParent(parent.transform, worldPositionStays: false);
        }
        static Transform findRoot(this Transform tfthis, Transform baseobj)
        {
            var tf = tfthis;
            while (tf.parent != baseobj)
            {
                tf = tf.parent;
            }
            return tf;
        }




        public static Material CreateMaterial(string shadername, Texture tex = null)
        {
            var shader = Shader.Find(shadername);
            if (shader is null) return null;

            var newmat = new Material(shader);
            newmat.mainTexture = tex;

            return newmat;
        }

    }



}