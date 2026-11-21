using System.Collections.Generic;
using System.Linq;
using DanhCu.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DanhCu.Editor
{
    /// <summary>
    /// Idempotent project setup. Runs automatically the first time the project is opened
    /// (when anything is missing) and can be re-run from "Danh Cu > Setup".
    /// It only creates what is missing; it never overwrites existing assets or scenes.
    ///
    /// CI / batch mode: -executeMethod DanhCu.Editor.ProjectSetup.RunBatch
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        private const string SessionKey = "DanhCu.ProjectSetup.AutoRunChecked";

        static ProjectSetup()
        {
            EditorApplication.delayCall += AutoRunIfNeeded;
        }

        public static bool IsComplete()
        {
            return RenderingSetup.IsConfigured()
                && PlayerSettingsSetup.IsInputSystemActive()
                && AssetDatabase.LoadAssetAtPath<GameConfig>(ProjectPaths.GameConfig) != null
                && !SceneFactory.AnyMissing()
                && BuildSettingsContainAll();
        }

        [MenuItem("Danh Cu/Setup/Run Project Setup", priority = 0)]
        public static void RunInteractive() => Run(true);

        [MenuItem("Danh Cu/Setup/Validate Data", priority = 20)]
        public static void ValidateDataMenu()
        {
            List<string> errors = ValidateAllData();
            if (errors.Count == 0)
            {
                Debug.Log("[DanhCu] All data assets are valid.");
            }
            else
            {
                Debug.LogError("[DanhCu] Data validation failed:\n- " + string.Join("\n- ", errors));
            }
        }

        /// <summary>Entry point for -executeMethod. Exits with code 1 if data is invalid.</summary>
        public static void RunBatch()
        {
            Run(false);
            List<string> errors = ValidateAllData();
            if (errors.Count > 0)
            {
                Debug.LogError("[DanhCu] Data validation failed:\n- " + string.Join("\n- ", errors));
                EditorApplication.Exit(1);
            }
        }

        public static List<string> ValidateAllData()
        {
            var errors = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { ProjectPaths.Root }))
            {
                Object asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset is IValidatable validatable)
                {
                    validatable.Validate(errors);
                }
            }

            return errors;
        }

        private static void AutoRunIfNeeded()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);
            if (!IsComplete())
            {
                Debug.Log("[DanhCu] Project setup incomplete; running first-time setup.");
                Run(true);
            }
        }

        private static void Run(bool interactive)
        {
            if (interactive && SceneFactory.AnyMissing() && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[DanhCu] Setup cancelled: unsaved scene changes.");
                return;
            }

            EnsureFolders();
            PlayerSettingsSetup.Configure();
            RenderingSetup.Configure();

            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ProjectPaths.InputActions);
            if (inputActions == null)
            {
                Debug.LogError($"[DanhCu] Input actions asset missing at {ProjectPaths.InputActions}.");
            }

            DefaultContentFactory.EnsureDefaultContent(inputActions);
            SceneFactory.EnsureScenes();
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();

            bool needsRestart = PlayerSettingsSetup.EnsureInputSystemBackend();
            AssetDatabase.SaveAssets();

            if (interactive && AssetDatabase.LoadAssetAtPath<SceneAsset>(ProjectPaths.BootstrapScene) != null
                && string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path))
            {
                EditorSceneManager.OpenScene(ProjectPaths.BootstrapScene);
            }

            Debug.Log("[DanhCu] Project setup complete.");

            if (needsRestart && interactive
                && EditorUtility.DisplayDialog(
                    "Danh Cu Vietnam setup",
                    "Active Input Handling was switched to the Input System package. Unity must restart for this to take effect.",
                    "Restart now",
                    "Later"))
            {
                EditorApplication.OpenProject(System.IO.Directory.GetCurrentDirectory());
            }
        }

        private static void EnsureFolders()
        {
            foreach (string folder in ProjectPaths.Folders)
            {
                CreateFolderRecursive(folder);
            }
        }

        private static void CreateFolderRecursive(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            CreateFolderRecursive(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        private static string[] RequiredBuildScenes => new[]
        {
            ProjectPaths.BootstrapScene, // index 0: first scene in every build
            ProjectPaths.MainMenuScene,
            ProjectPaths.GameplayScene,
            ProjectPaths.DefaultArenaScene,
        };

        private static bool BuildSettingsContainAll()
        {
            HashSet<string> present = new HashSet<string>(EditorBuildSettings.scenes.Select(s => s.path));
            return RequiredBuildScenes.All(present.Contains)
                && EditorBuildSettings.scenes.Length > 0
                && EditorBuildSettings.scenes[0].path == ProjectPaths.BootstrapScene;
        }

        /// <summary>Puts the required scenes first (Bootstrap at index 0) and keeps any extra scenes after them.</summary>
        private static void EnsureBuildSettings()
        {
            if (BuildSettingsContainAll())
            {
                return;
            }

            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string path in RequiredBuildScenes)
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (!RequiredBuildScenes.Contains(existing.path))
                {
                    scenes.Add(existing);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
