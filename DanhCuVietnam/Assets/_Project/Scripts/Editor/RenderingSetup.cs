using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DanhCu.Editor
{
    /// <summary>
    /// Creates two URP assets (desktop "HighFidelity" and "Mobile") and assigns them to
    /// Graphics Settings and every Quality level. Existing assets are reused, not replaced.
    /// </summary>
    internal static class RenderingSetup
    {
        public static bool IsConfigured()
        {
            return GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset;
        }

        public static void Configure()
        {
            UniversalRenderPipelineAsset high = GetOrCreate(ProjectPaths.UrpHighAsset, ProjectPaths.UrpHighRenderer, asset =>
            {
                asset.renderScale = 1f;
                asset.msaaSampleCount = 4;
                asset.supportsHDR = true;
                asset.shadowDistance = 8f; // the play area is small; spend shadow resolution near the ring
            });

            UniversalRenderPipelineAsset mobile = GetOrCreate(ProjectPaths.UrpMobileAsset, ProjectPaths.UrpMobileRenderer, asset =>
            {
                asset.renderScale = 0.85f;
                asset.msaaSampleCount = 2;
                asset.supportsHDR = false;
                asset.shadowDistance = 4f;
            });

            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                GraphicsSettings.defaultRenderPipeline = high;
            }

            // Lower half of the quality ladder -> Mobile, upper half -> HighFidelity.
            // Quality levels whose pipeline was already set by hand are left alone.
            int original = QualitySettings.GetQualityLevel();
            string[] levels = QualitySettings.names;
            for (int i = 0; i < levels.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                if (QualitySettings.renderPipeline == null)
                {
                    QualitySettings.renderPipeline = i < levels.Length / 2 ? mobile : high;
                }
            }

            QualitySettings.SetQualityLevel(original, false);
        }

        private static UniversalRenderPipelineAsset GetOrCreate(string assetPath, string rendererPath, System.Action<UniversalRenderPipelineAsset> tune)
        {
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(ProjectPaths.UrpDefaultPostProcessData);
                if (rendererData.postProcessData == null)
                {
                    Debug.LogWarning($"[DanhCu] Default URP PostProcessData not found at {ProjectPaths.UrpDefaultPostProcessData}; assign it on {rendererPath} to enable post-processing.");
                }

                AssetDatabase.CreateAsset(rendererData, rendererPath);
            }

            UniversalRenderPipelineAsset asset = UniversalRenderPipelineAsset.Create(rendererData);
            tune(asset);
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }
    }
}
