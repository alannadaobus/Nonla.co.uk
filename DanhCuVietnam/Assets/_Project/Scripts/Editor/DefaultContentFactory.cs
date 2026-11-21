using System.Collections.Generic;
using DanhCu.Core.Match;
using DanhCu.Core.Tops;
using DanhCu.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DanhCu.Editor
{
    /// <summary>
    /// Creates the starter data set. Only creates assets that do not exist yet; never
    /// overwrites designer edits. Values are first-pass estimates (see PHYSICS_SPEC.md §3).
    /// </summary>
    internal static class DefaultContentFactory
    {
        public static GameConfig EnsureDefaultContent(InputActionAsset inputActions)
        {
            Dictionary<string, Material> renderMaterials = TopModelAssetBuilder.EnsureMaterials();

            TopMaterialDefinition guava = GetOrCreate<TopMaterialDefinition>("TopMaterials/TopMaterial_GoOi", "go_oi", "Gỗ ổi", "Guava wood",
                m =>
                {
                    m.Configure(900f, 0.45f, 0.35f, 0.75f);
                    m.SetVisualMaterial(renderMaterials[TopModelAssetBuilder.WoodGuava]);
                });
            TopMaterialDefinition ironwood = GetOrCreate<TopMaterialDefinition>("TopMaterials/TopMaterial_GoSen", "go_sen", "Gỗ sến", "Sen ironwood",
                m =>
                {
                    m.Configure(1050f, 0.40f, 0.30f, 0.9f);
                    m.SetVisualMaterial(renderMaterials[TopModelAssetBuilder.WoodSen]);
                });
            TopMaterialDefinition jackfruit = GetOrCreate<TopMaterialDefinition>("TopMaterials/TopMaterial_GoMit", "go_mit", "Gỗ mít", "Jackfruit wood",
                m =>
                {
                    m.Configure(600f, 0.50f, 0.40f, 0.5f);
                    m.SetVisualMaterial(renderMaterials[TopModelAssetBuilder.WoodJackfruit]);
                });

            // Prototype sizes: assumed dimensions, documented in Docs/TOP_MODEL.md §2.
            Material metal = renderMaterials[TopModelAssetBuilder.MetalSteel];
            TopData small = GetOrCreateShape("TopData_Small", "top_shape_small", "Cỡ nhỏ", "Small", TopShapePresets.Small(), metal);
            TopData medium = GetOrCreateShape("TopData_Medium", "top_shape_medium", "Cỡ vừa", "Medium", TopShapePresets.Medium(), metal);
            TopData large = GetOrCreateShape("TopData_Large", "top_shape_large", "Cỡ lớn", "Large", TopShapePresets.Large(), metal);

            GameObject prefab = TopModelAssetBuilder.EnsurePrefab(medium, guava);

            // Launch caps keep >= 1.2x headroom over each shape's critical spin (PHYSICS_SPEC §4.2).
            var tops = new List<TopDefinition>
            {
                GetOrCreate<TopDefinition>("Tops/Top_CuGoOi", "cu_go_oi", "Cù gỗ ổi", "Guava top",
                    t => t.Configure(guava, TipType.IronNail, medium, 2800f,
                        new LocalizedText("Con cù truyền thống, cân bằng giữa sức bền và tấn công.", "The classic village top: balanced endurance and attack."), prefab)),
                GetOrCreate<TopDefinition>("Tops/Top_CuGoSen", "cu_go_sen", "Cù gỗ sến", "Ironwood top",
                    t => t.Configure(ironwood, TipType.IronNail, large, 2600f,
                        new LocalizedText("To, nặng và cứng, khó bị bổ ngã.", "Large, heavy and hard; very difficult to knock down."), prefab)),
                GetOrCreate<TopDefinition>("Tops/Top_CuGoMit", "cu_go_mit", "Cù gỗ mít", "Jackfruit top",
                    t => t.Configure(jackfruit, TipType.IronNail, small, 3000f,
                        new LocalizedText("Nhỏ, nhẹ, quay nhanh nhưng dễ bị đánh bật.", "Small, light and fast, but easily knocked away."), prefab)),
            };

            var arenas = new List<ArenaDefinition>
            {
                GetOrCreate<ArenaDefinition>("Arenas/Arena_SanDinh", "san_dinh", "Sân đình", "Communal house courtyard",
                    a => a.Configure(ProjectPaths.DefaultArenaSceneName, ArenaSurface.PackedEarth, 0.35f, 0.6f,
                        new LocalizedText("Sân đất nện trước đình làng, nơi trẻ em chơi cù ngày Tết.", "Packed-earth yard before the village communal house, where children play at Tết."))),
            };

            var modes = new List<GameModeDefinition>
            {
                GetOrCreate<GameModeDefinition>("GameModes/GameMode_SinglePlayer", "single_player", "Luyện tập", "Practice",
                    m => m.Configure(GameModeType.SinglePlayer, MatchRuleset.LongestSpin, 1, true,
                        new LocalizedText("Tập quấn dây và thả cù.", "Practise winding and throwing."))),
                GetOrCreate<GameModeDefinition>("GameModes/GameMode_VersusAI", "versus_ai", "Đấu với máy", "Versus AI",
                    m => m.Configure(GameModeType.VersusAI, MatchRuleset.Strike, 2, true,
                        new LocalizedText("Thi đấu với đối thủ máy.", "Compete against a computer opponent."))),
                GetOrCreate<GameModeDefinition>("GameModes/GameMode_Online", "online", "Trực tuyến", "Online",
                    m => m.Configure(GameModeType.OnlineMultiplayer, MatchRuleset.Strike, 2, false,
                        new LocalizedText("Thi đấu với người chơi khác.", "Play against other players."))),
            };

            var ai = new List<AIDifficultyProfile>
            {
                GetOrCreate<AIDifficultyProfile>("AI/AI_Easy", "ai_easy", "Dễ", "Easy", p => p.Configure(0, 12f, 0.25f, 1.4f, 0.2f)),
                GetOrCreate<AIDifficultyProfile>("AI/AI_Medium", "ai_medium", "Vừa", "Medium", p => p.Configure(1, 6f, 0.15f, 0.8f, 0.5f)),
                GetOrCreate<AIDifficultyProfile>("AI/AI_Hard", "ai_hard", "Khó", "Hard", p => p.Configure(2, 2.5f, 0.06f, 0.4f, 0.8f)),
            };

            PhysicsTuningProfile physics = GetOrCreate<PhysicsTuningProfile>("Physics/PhysicsTuning_Default", "physics_default", "Mặc định", "Default", null);

            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ProjectPaths.GameConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameConfig>();
                config.Configure(modes, tops, arenas, ai, physics, inputActions);
                AssetDatabase.CreateAsset(config, ProjectPaths.GameConfig);
            }

            return config;
        }

        private static TopData GetOrCreateShape(string assetName, string id, string vi, string en, TopShapeParameters preset, Material metal)
        {
            TopData shape = GetOrCreate<TopData>("TopShapes/" + assetName, id, vi, en, d =>
            {
                d.SetParameters(preset);
                d.SetRenderMaterials(metal, null);
            });

            if (!shape.HasCurrentBake)
            {
                TopModelAssetBuilder.Bake(shape);
            }

            return shape;
        }

        private static T GetOrCreate<T>(string relativePath, string id, string vi, string en, System.Action<T> configure)
            where T : DefinitionAsset
        {
            string path = $"{ProjectPaths.Data}/{relativePath}.asset";
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            asset.name = System.IO.Path.GetFileName(relativePath);
            asset.SetIdentity(id, new LocalizedText(vi, en));
            configure?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
