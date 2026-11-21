using System.Collections.Generic;
using DanhCu.Core.Tops;
using DanhCu.Data;
using DanhCu.Tops;
using UnityEditor;
using UnityEngine;

namespace DanhCu.Editor
{
    /// <summary>
    /// Creates the top render materials, bakes TopData meshes into assets and builds
    /// Prefabs/Tops/TopModel.prefab. Materials and the prefab are only created if missing;
    /// baking overwrites mesh contents in place (references and GUIDs stay valid).
    /// </summary>
    internal static class TopModelAssetBuilder
    {
        public const string WoodGuava = "M_Top_Wood_GoOi";
        public const string WoodSen = "M_Top_Wood_GoSen";
        public const string WoodJackfruit = "M_Top_Wood_GoMit";
        public const string MetalSteel = "M_Top_Metal_Steel";

        private const string UrpLit = "Universal Render Pipeline/Lit";

        /// <summary>Base colours are neutral placeholders for prototyping, not colour-matched to real woods.</summary>
        public static Dictionary<string, Material> EnsureMaterials()
        {
            return new Dictionary<string, Material>
            {
                [WoodGuava] = GetOrCreateMaterial(WoodGuava, new Color(0.62f, 0.45f, 0.28f), 0f, 0.35f),
                [WoodSen] = GetOrCreateMaterial(WoodSen, new Color(0.40f, 0.22f, 0.14f), 0f, 0.40f),
                [WoodJackfruit] = GetOrCreateMaterial(WoodJackfruit, new Color(0.74f, 0.58f, 0.30f), 0f, 0.30f),
                [MetalSteel] = GetOrCreateMaterial(MetalSteel, new Color(0.56f, 0.57f, 0.58f), 1f, 0.55f),
            };
        }

        [MenuItem("Danh Cu/Tops/Rebake All Top Meshes", priority = 100)]
        public static void RebakeAllMenu()
        {
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(TopData)))
            {
                var data = AssetDatabase.LoadAssetAtPath<TopData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data != null && Bake(data))
                {
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DanhCu] Rebaked {count} top shape(s).");
        }

        [MenuItem("Danh Cu/Tops/Rebuild TopModel Prefab", priority = 101)]
        public static void RebuildPrefabMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPaths.TopModelPrefab) != null
                && !EditorUtility.DisplayDialog("Rebuild TopModel prefab", "Replace the existing TopModel.prefab? Manual edits to it will be lost.", "Replace", "Cancel"))
            {
                return;
            }

            AssetDatabase.DeleteAsset(ProjectPaths.TopModelPrefab);
            var medium = AssetDatabase.LoadAssetAtPath<TopData>($"{ProjectPaths.Data}/TopShapes/TopData_Medium.asset");
            var guava = AssetDatabase.LoadAssetAtPath<TopMaterialDefinition>($"{ProjectPaths.Data}/TopMaterials/TopMaterial_GoOi.asset");
            EnsurePrefab(medium, guava);
        }

        /// <summary>Generates the meshes of <paramref name="data"/> into Art/Models/Tops/Generated. Returns false if the shape is invalid.</summary>
        public static bool Bake(TopData data)
        {
            if (!data.IsShapeValid)
            {
                Debug.LogError($"[DanhCu] Cannot bake '{data.name}': " + string.Join(" ", data.ToParameters().Validate()), data);
                return false;
            }

            TopProfile profile = data.GetProfile();
            string path = $"{ProjectPaths.TopGeneratedMeshes}/{data.name}_Meshes.asset";
            Mesh body = LoadOrCreateMesh(path, data.name + "_Body", true);
            Mesh tip = LoadOrCreateMesh(path, data.name + "_Tip", false);
            Mesh collision = LoadOrCreateMesh(path, data.name + "_Collision", false);

            TopMeshUtility.Fill(body, TopMeshFactory.BuildBody(profile));
            TopMeshUtility.Fill(tip, TopMeshFactory.BuildTip(profile));
            TopMeshUtility.Fill(collision, TopMeshFactory.BuildCollisionHull(profile));
            EditorUtility.SetDirty(body);
            EditorUtility.SetDirty(tip);
            EditorUtility.SetDirty(collision);

            data.SetBake(body, tip, collision, data.ToParameters().GetSignature());
            EditorUtility.SetDirty(data);
            return true;
        }

        /// <summary>Creates TopModel.prefab if it does not exist. Returns the prefab asset.</summary>
        public static GameObject EnsurePrefab(TopData shape, TopMaterialDefinition wood)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPaths.TopModelPrefab);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = CreateHierarchy(out TopModelController controller);
            try
            {
                if (shape != null)
                {
                    if (!shape.HasCurrentBake)
                    {
                        Bake(shape);
                    }

                    controller.Apply(shape, wood, false);
                }

                return PrefabUtility.SaveAsPrefabAsset(root, ProjectPaths.TopModelPrefab);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// TopModel (pivot at tip point; Rigidbody + TopModelController)
        ///   Body (MeshFilter, MeshRenderer [wood, rope zone], convex MeshCollider)
        ///   Tip  (MeshFilter, MeshRenderer [metal], SphereCollider on the axis)
        /// </summary>
        internal static GameObject CreateHierarchy(out TopModelController controller)
        {
            var root = new GameObject("TopModel");
            var rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var body = new GameObject(TopModelController.BodyChildName);
            body.transform.SetParent(root.transform, false);
            MeshFilter bodyFilter = body.AddComponent<MeshFilter>();
            MeshRenderer bodyRenderer = body.AddComponent<MeshRenderer>();
            MeshCollider bodyCollider = body.AddComponent<MeshCollider>();
            bodyCollider.convex = true;

            var tip = new GameObject(TopModelController.TipChildName);
            tip.transform.SetParent(root.transform, false);
            MeshFilter tipFilter = tip.AddComponent<MeshFilter>();
            MeshRenderer tipRenderer = tip.AddComponent<MeshRenderer>();
            SphereCollider tipCollider = tip.AddComponent<SphereCollider>();

            controller = root.AddComponent<TopModelController>();
            controller.SetParts(bodyFilter, bodyRenderer, bodyCollider, tipFilter, tipRenderer, tipCollider);
            return root;
        }

        private static Mesh LoadOrCreateMesh(string path, string name, bool isMainAsset)
        {
            foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (obj is Mesh found && found.name == name)
                {
                    return found;
                }
            }

            var mesh = new Mesh { name = name };
            if (isMainAsset && AssetDatabase.LoadMainAssetAtPath(path) == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
            }
            else
            {
                AssetDatabase.AddObjectToAsset(mesh, path);
            }

            return mesh;
        }

        private static Material GetOrCreateMaterial(string name, Color baseColor, float metallic, float smoothness)
        {
            string path = $"{ProjectPaths.TopMaterials}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find(UrpLit);
            if (shader == null)
            {
                Debug.LogWarning($"[DanhCu] Shader '{UrpLit}' not found; '{name}' uses the default shader. Re-run setup after URP is installed.");
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", baseColor);
            material.color = baseColor;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }

    /// <summary>Re-bakes a TopData's meshes automatically after it is edited in the inspector.</summary>
    [InitializeOnLoad]
    internal static class TopDataAutoBaker
    {
        private static readonly HashSet<TopData> Pending = new HashSet<TopData>();

        static TopDataAutoBaker()
        {
            TopData.EditorChanged += OnChanged;
        }

        private static void OnChanged(TopData data)
        {
            if (data == null || !EditorUtility.IsPersistent(data) || !Pending.Add(data))
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                Pending.Remove(data);
                if (data != null && data.IsShapeValid && !data.HasCurrentBake && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    TopModelAssetBuilder.Bake(data);
                    AssetDatabase.SaveAssets();
                }
            };
        }
    }
}
