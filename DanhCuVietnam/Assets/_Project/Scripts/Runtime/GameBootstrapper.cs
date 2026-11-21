using DanhCu.Data;
using UnityEngine;

namespace DanhCu.Runtime
{
    /// <summary>
    /// Guarantees exactly one GameManager exists before any scene loads, whichever scene
    /// Play is pressed in. This is the only automatic entry point of the game.
    /// </summary>
    internal static class GameBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (GameManager.Instance != null)
            {
                return;
            }

            GameConfig config = Resources.Load<GameConfig>(GameConfig.ResourcesPath);
            if (config == null)
            {
                Debug.LogError($"[DanhCu] Resources/{GameConfig.ResourcesPath} not found. Run 'Danh Cu > Setup > Run Project Setup'. Using empty defaults.");
                config = ScriptableObject.CreateInstance<GameConfig>();
            }

            var host = new GameObject("[GameManager]");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<GameManager>().Initialize(config);
        }
    }
}
