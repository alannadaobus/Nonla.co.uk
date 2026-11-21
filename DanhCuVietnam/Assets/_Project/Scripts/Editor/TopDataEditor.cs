using System.Collections.Generic;
using DanhCu.Core.Tops;
using DanhCu.Data;
using UnityEditor;
using UnityEngine;

namespace DanhCu.Editor
{
    /// <summary>
    /// TopData inspector: the editable dimensions, then a read-only summary in millimetres
    /// and grams, integrated mass properties for a chosen wood density, validation messages,
    /// bake status and preset buttons.
    /// </summary>
    [CustomEditor(typeof(TopData))]
    internal sealed class TopDataEditor : UnityEditor.Editor
    {
        private const string DensityPrefKey = "DanhCu.TopDataEditor.PreviewDensity";

        public override void OnInspectorGUI()
        {
            var data = (TopData)target;
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Derived (read-only)", EditorStyles.boldLabel);

            List<string> errors = data.ToParameters().Validate();
            if (errors.Count > 0)
            {
                EditorGUILayout.HelpBox(string.Join("\n", errors), MessageType.Error);
                return;
            }

            TopShapeParameters p = data.ToParameters();
            TopProfile profile = data.GetProfile();
            EditorGUILayout.LabelField("Height × diameter", $"{p.TotalHeight * 1000:0.0} × {p.BodyDiameter * 1000:0.0} mm");
            EditorGUILayout.LabelField("Exposed tip / shank Ø", $"{p.TipExposedLength * 1000:0.0} mm / {p.TipShankDiameter * 1000:0.0} mm");
            EditorGUILayout.LabelField("Belly height", $"{p.BellyHeight * 1000:0.0} mm above pivot");
            EditorGUILayout.LabelField("Rope zone", $"{profile.RopeZoneStartHeight * 1000:0.0} – {profile.RopeZoneEndHeight * 1000:0.0} mm");
            EditorGUILayout.LabelField("Tip half-angle", $"{profile.ConeHalfAngle * Mathf.Rad2Deg:0.0}°");

            float density = EditorPrefs.GetFloat(DensityPrefKey, (float)TopData.FallbackWoodDensity);
            float newDensity = EditorGUILayout.Slider("Preview wood density", density, 300f, 1300f);
            if (!Mathf.Approximately(density, newDensity))
            {
                EditorPrefs.SetFloat(DensityPrefKey, newDensity);
            }

            TopMassProperties m = data.GetMassProperties(newDensity);
            double crit = m.CriticalAngularSpeed(9.81);
            EditorGUILayout.LabelField("Mass (wood + metal)", $"{m.Mass * 1000:0.0} g ({m.WoodMass * 1000:0.0} + {m.MetalMass * 1000:0.0})");
            EditorGUILayout.LabelField("Centre of mass", $"{m.CenterOfMassHeight * 1000:0.0} mm above pivot ({m.CenterOfMassHeight / p.TotalHeight:P0} of height)");
            EditorGUILayout.LabelField("I₃ / I₁(CoM)", $"{m.AxialInertia:0.00e0} / {m.TransverseInertiaAtCenterOfMass:0.00e0} kg·m²");
            EditorGUILayout.LabelField("Critical spin ω_crit", $"{crit:0} rad/s ({crit * 60 / (2 * Mathf.PI):0} RPM)");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Meshes", data.HasCurrentBake ? "Baked, up to date" : "Not baked / out of date");
            if (GUILayout.Button("Rebake meshes"))
            {
                TopModelAssetBuilder.Bake(data);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Reset dimensions to preset", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Small"))
            {
                ApplyPreset(data, TopShapePresets.Small());
            }

            if (GUILayout.Button("Medium"))
            {
                ApplyPreset(data, TopShapePresets.Medium());
            }

            if (GUILayout.Button("Large"))
            {
                ApplyPreset(data, TopShapePresets.Large());
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void ApplyPreset(TopData data, TopShapeParameters preset)
        {
            Undo.RecordObject(data, "Apply top size preset");
            data.SetParameters(preset);
            EditorUtility.SetDirty(data);
            TopModelAssetBuilder.Bake(data);
            AssetDatabase.SaveAssets();
        }
    }
}
