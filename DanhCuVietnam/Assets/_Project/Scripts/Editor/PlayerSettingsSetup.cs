using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace DanhCu.Editor
{
    /// <summary>Cross-platform player settings and the Input System backend.</summary>
    internal static class PlayerSettingsSetup
    {
        public const string CompanyName = "Nonla";
        public const string ProductName = "Danh Cu Vietnam";
        public const string ApplicationId = "uk.co.nonla.danhcuvietnam";

        /// <summary>ProjectSettings.asset value: 0 = legacy Input Manager, 1 = Input System, 2 = both.</summary>
        private const int InputSystemOnly = 1;

        public static void Configure()
        {
            if (PlayerSettings.companyName == "DefaultCompany")
            {
                PlayerSettings.companyName = CompanyName;
            }

            if (PlayerSettings.productName != ProductName && PlayerSettings.productName == GetFolderName())
            {
                PlayerSettings.productName = ProductName;
            }

            PlayerSettings.colorSpace = ColorSpace.Linear;

            SetIdIfDefault(NamedBuildTarget.Standalone);
            SetIdIfDefault(NamedBuildTarget.Android);
            SetIdIfDefault(NamedBuildTarget.iOS);
            SetIdIfDefault(NamedBuildTarget.WebGL);

            // Store builds require IL2CPP on mobile; ARM64 is mandatory on Google Play.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        }

        public static bool IsInputSystemActive()
        {
            SerializedProperty property = GetInputHandlerProperty(out _);
            return property != null && property.intValue == InputSystemOnly;
        }

        /// <summary>Switches to the Input System backend. Returns true if a restart is needed.</summary>
        public static bool EnsureInputSystemBackend()
        {
            SerializedProperty property = GetInputHandlerProperty(out SerializedObject settings);
            if (property == null)
            {
                Debug.LogWarning("[DanhCu] Could not find 'activeInputHandler'. Set Project Settings > Player > Active Input Handling to 'Input System Package (New)' manually.");
                return false;
            }

            if (property.intValue == InputSystemOnly)
            {
                return false;
            }

            property.intValue = InputSystemOnly;
            settings.ApplyModifiedProperties();
            return true;
        }

        private static SerializedProperty GetInputHandlerProperty(out SerializedObject settings)
        {
            settings = null;
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0)
            {
                return null;
            }

            settings = new SerializedObject(assets[0]);
            return settings.FindProperty("activeInputHandler");
        }

        private static void SetIdIfDefault(NamedBuildTarget target)
        {
            string current = PlayerSettings.GetApplicationIdentifier(target);
            if (string.IsNullOrEmpty(current) || current.StartsWith("com.DefaultCompany", System.StringComparison.Ordinal) || current.StartsWith("com.Unity", System.StringComparison.Ordinal))
            {
                PlayerSettings.SetApplicationIdentifier(target, ApplicationId);
            }
        }

        private static string GetFolderName()
        {
            return System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(Application.dataPath));
        }
    }
}
