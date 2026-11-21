using System.Collections.Generic;
using DanhCu.Core.Match;
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
            TopMaterialDefinition guava = GetOrCreate<TopMaterialDefinition>("TopMaterials/TopMaterial_GoOi", "go_oi", "Gỗ ổi", "Guava wood",
                m => m.Configure(900f, 0.45f, 0.35f, 0.75f));
            TopMaterialDefinition ironwood = GetOrCreate<TopMaterialDefinition>("TopMaterials/TopMaterial_GoSen", "go_sen", "Gỗ sến", "Sen ironwood",
                m => m.Configure(1050f, 0.40f, 0.30f, 0.9f));
            TopMaterialDefinition jackfruit = GetOrCreate<TopMaterialDefinition>("TopMaterials/TopMaterial_GoMit", "go_mit", "Gỗ mít", "Jackfruit wood",
                m => m.Configure(600f, 0.50f, 0.40f, 0.5f));

            var tops = new List<TopDefinition>
            {
                GetOrCreate<TopDefinition>("Tops/Top_CuGoOi", "cu_go_oi", "Cù gỗ ổi", "Guava top",
                    t => t.Configure(guava, TipType.IronNail, 0.18f, 0.035f, 0.075f, 2200f,
                        new LocalizedText("Con cù truyền thống, cân bằng giữa sức bền và tấn công.", "The classic village top: balanced endurance and attack."))),
                GetOrCreate<TopDefinition>("Tops/Top_CuGoSen", "cu_go_sen", "Cù gỗ sến", "Ironwood top",
                    t => t.Configure(ironwood, TipType.IronNail, 0.24f, 0.038f, 0.08f, 1900f,
                        new LocalizedText("Nặng và cứng, khó bị bổ vỡ.", "Heavy and hard; very difficult to knock down."))),
                GetOrCreate<TopDefinition>("Tops/Top_CuGoMit", "cu_go_mit", "Cù gỗ mít", "Jackfruit top",
                    t => t.Configure(jackfruit, TipType.Rounded, 0.13f, 0.033f, 0.07f, 2600f,
                        new LocalizedText("Nhẹ, quay nhanh nhưng dễ bị đánh bật.", "Light and fast, but easily knocked away."))),
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
            asset.SetIdentity(id, new LocalizedText(vi, en));
            configure?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
