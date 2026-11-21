using System.Collections.Generic;
using DanhCu.Core.Match;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DanhCu.Data
{
    /// <summary>
    /// Root configuration asset. Exactly one exists, at Resources/DanhCu/GameConfig,
    /// and it is the only asset loaded through Resources. Everything else is reached
    /// from here (or, later, through Addressables).
    /// </summary>
    [CreateAssetMenu(menuName = "Danh Cu/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject, IValidatable
    {
        public const string ResourcesPath = "DanhCu/GameConfig";

        [Header("Scenes (names, must be in Build Settings)")]
        [SerializeField] private string bootstrapScene = "Bootstrap";
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string gameplayScene = "Gameplay";

        [Header("Content")]
        [SerializeField] private List<GameModeDefinition> gameModes = new List<GameModeDefinition>();
        [SerializeField] private List<TopDefinition> tops = new List<TopDefinition>();
        [SerializeField] private List<ArenaDefinition> arenas = new List<ArenaDefinition>();
        [SerializeField] private List<AIDifficultyProfile> aiProfiles = new List<AIDifficultyProfile>();

        [Header("Defaults")]
        [SerializeField] private TopDefinition defaultTop;
        [SerializeField] private ArenaDefinition defaultArena;
        [SerializeField] private AIDifficultyProfile defaultAIProfile;

        [Header("Systems")]
        [SerializeField] private PhysicsTuningProfile physicsTuning;
        [SerializeField] private InputActionAsset inputActions;

        [Header("Platform")]
        [Tooltip("Target frame rate on phones and tablets.")]
        [SerializeField] private int mobileTargetFrameRate = 60;

        [Tooltip("Target frame rate on desktop; -1 = follow VSync.")]
        [SerializeField] private int desktopTargetFrameRate = -1;

        public string BootstrapScene => bootstrapScene;

        public string MainMenuScene => mainMenuScene;

        public string GameplayScene => gameplayScene;

        public IReadOnlyList<GameModeDefinition> GameModes => gameModes;

        public IReadOnlyList<TopDefinition> Tops => tops;

        public IReadOnlyList<ArenaDefinition> Arenas => arenas;

        public IReadOnlyList<AIDifficultyProfile> AIProfiles => aiProfiles;

        public TopDefinition DefaultTop => defaultTop;

        public ArenaDefinition DefaultArena => defaultArena;

        public AIDifficultyProfile DefaultAIProfile => defaultAIProfile;

        public PhysicsTuningProfile PhysicsTuning => physicsTuning;

        public InputActionAsset InputActions => inputActions;

        public int MobileTargetFrameRate => mobileTargetFrameRate;

        public int DesktopTargetFrameRate => desktopTargetFrameRate;

        public TopDefinition FindTop(string id) => Find(tops, id);

        public ArenaDefinition FindArena(string id) => Find(arenas, id);

        public AIDifficultyProfile FindAIProfile(string id) => Find(aiProfiles, id);

        public GameModeDefinition FindMode(GameModeType type)
        {
            for (int i = 0; i < gameModes.Count; i++)
            {
                if (gameModes[i] != null && gameModes[i].ModeType == type)
                {
                    return gameModes[i];
                }
            }

            return null;
        }

        public bool IsArenaScene(string sceneName)
        {
            for (int i = 0; i < arenas.Count; i++)
            {
                if (arenas[i] != null && arenas[i].SceneName == sceneName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Builds playable settings for a mode from the configured defaults.</summary>
        public MatchSettings CreateDefaultMatchSettings(GameModeType mode)
        {
            GameModeDefinition modeDef = FindMode(mode);
            TopDefinition opponent = FindOpponentTop();

            var settings = new MatchSettings
            {
                Mode = mode,
                Ruleset = modeDef != null ? modeDef.DefaultRuleset : MatchRuleset.LongestSpin,
                RoundsToWin = modeDef != null ? modeDef.RoundsToWin : 1,
                RoundTimeLimitSeconds = modeDef != null ? modeDef.RoundTimeLimitSeconds : 0f,
                ArenaId = defaultArena != null ? defaultArena.Id : string.Empty,
                PlayerTopId = defaultTop != null ? defaultTop.Id : string.Empty,
                OpponentTopId = GameModeRules.RequiresOpponentTop(mode) && opponent != null ? opponent.Id : string.Empty,
                AIProfileId = GameModeRules.RequiresAIOpponent(mode) && defaultAIProfile != null ? defaultAIProfile.Id : string.Empty,
            };

            return settings;
        }

        /// <summary>Checks that every id in the settings resolves to content in this config.</summary>
        public List<string> ValidateMatch(MatchSettings settings)
        {
            List<string> errors = settings.Validate();
            if (!string.IsNullOrEmpty(settings.ArenaId) && FindArena(settings.ArenaId) == null)
            {
                errors.Add($"Unknown arena '{settings.ArenaId}'.");
            }

            if (!string.IsNullOrEmpty(settings.PlayerTopId) && FindTop(settings.PlayerTopId) == null)
            {
                errors.Add($"Unknown top '{settings.PlayerTopId}'.");
            }

            if (!string.IsNullOrEmpty(settings.OpponentTopId) && FindTop(settings.OpponentTopId) == null)
            {
                errors.Add($"Unknown opponent top '{settings.OpponentTopId}'.");
            }

            if (!string.IsNullOrEmpty(settings.AIProfileId) && FindAIProfile(settings.AIProfileId) == null)
            {
                errors.Add($"Unknown AI profile '{settings.AIProfileId}'.");
            }

            return errors;
        }

        public void Validate(List<string> errors)
        {
            Require(!string.IsNullOrWhiteSpace(bootstrapScene), errors, "bootstrapScene is empty.");
            Require(!string.IsNullOrWhiteSpace(mainMenuScene), errors, "mainMenuScene is empty.");
            Require(!string.IsNullOrWhiteSpace(gameplayScene), errors, "gameplayScene is empty.");
            Require(physicsTuning != null, errors, "physicsTuning is not assigned.");
            Require(inputActions != null, errors, "inputActions is not assigned.");
            Require(defaultTop != null && tops.Contains(defaultTop), errors, "defaultTop must be assigned and listed in tops.");
            Require(defaultArena != null && arenas.Contains(defaultArena), errors, "defaultArena must be assigned and listed in arenas.");
            Require(defaultAIProfile != null && aiProfiles.Contains(defaultAIProfile), errors, "defaultAIProfile must be assigned and listed in aiProfiles.");

            foreach (GameModeType type in (GameModeType[])System.Enum.GetValues(typeof(GameModeType)))
            {
                Require(FindMode(type) != null, errors, $"no GameModeDefinition for {type}.");
            }

            CheckUniqueIds(tops, errors);
            CheckUniqueIds(arenas, errors);
            CheckUniqueIds(aiProfiles, errors);
            CheckUniqueIds(gameModes, errors);
        }

        internal void Configure(
            List<GameModeDefinition> newModes,
            List<TopDefinition> newTops,
            List<ArenaDefinition> newArenas,
            List<AIDifficultyProfile> newAIProfiles,
            PhysicsTuningProfile newPhysics,
            InputActionAsset newInputActions)
        {
            gameModes = newModes;
            tops = newTops;
            arenas = newArenas;
            aiProfiles = newAIProfiles;
            physicsTuning = newPhysics;
            inputActions = newInputActions;
            defaultTop = newTops.Count > 0 ? newTops[0] : null;
            defaultArena = newArenas.Count > 0 ? newArenas[0] : null;
            defaultAIProfile = newAIProfiles.Count > 1 ? newAIProfiles[1] : (newAIProfiles.Count > 0 ? newAIProfiles[0] : null);
        }

        private TopDefinition FindOpponentTop()
        {
            for (int i = 0; i < tops.Count; i++)
            {
                if (tops[i] != null && tops[i] != defaultTop)
                {
                    return tops[i];
                }
            }

            return defaultTop;
        }

        private static T Find<T>(List<T> list, string id) where T : DefinitionAsset
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].Id == id)
                {
                    return list[i];
                }
            }

            return null;
        }

        private static void CheckUniqueIds<T>(List<T> list, List<string> errors) where T : DefinitionAsset
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                {
                    errors.Add($"GameConfig: null entry in {typeof(T).Name} list at index {i}.");
                    continue;
                }

                if (!seen.Add(list[i].Id))
                {
                    errors.Add($"GameConfig: duplicate {typeof(T).Name} id '{list[i].Id}'.");
                }
            }
        }

        private static void Require(bool condition, List<string> errors, string message)
        {
            if (!condition)
            {
                errors.Add("GameConfig: " + message);
            }
        }
    }
}
