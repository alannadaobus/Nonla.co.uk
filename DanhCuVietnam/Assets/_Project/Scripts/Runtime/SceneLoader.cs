using System;
using System.Collections;
using DanhCu.Core.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DanhCu.Runtime
{
    /// <summary>What to load: a primary scene (loaded Single) and an optional additive one.</summary>
    public readonly struct SceneLoadRequest
    {
        public SceneLoadRequest(string primaryScene, string additiveScene = null)
        {
            PrimaryScene = primaryScene;
            AdditiveScene = additiveScene;
        }

        public string PrimaryScene { get; }

        /// <summary>Loaded additively after the primary scene and made active (for its lighting). May be null.</summary>
        public string AdditiveScene { get; }

        public bool HasAdditive => !string.IsNullOrEmpty(AdditiveScene);

        public override string ToString() => HasAdditive ? $"{PrimaryScene} + {AdditiveScene}" : PrimaryScene;
    }

    /// <summary>
    /// Asynchronous scene loading. Uses the host MonoBehaviour's coroutines so it works
    /// identically on every platform, including WebGL (no threads, no blocking loads).
    /// </summary>
    public sealed class SceneLoader
    {
        private readonly MonoBehaviour _host;
        private readonly EventBus _events;

        public SceneLoader(MonoBehaviour host, EventBus events)
        {
            _host = host ? host : throw new ArgumentNullException(nameof(host));
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public bool IsLoading { get; private set; }

        public float Progress { get; private set; }

        /// <summary>Starts loading. <paramref name="onComplete"/> receives false if any scene could not be loaded.</summary>
        public void Load(SceneLoadRequest request, Action<bool> onComplete)
        {
            if (IsLoading)
            {
                Debug.LogError($"[DanhCu] Scene load '{request}' requested while another load is running.");
                onComplete?.Invoke(false);
                return;
            }

            _host.StartCoroutine(LoadRoutine(request, onComplete));
        }

        private IEnumerator LoadRoutine(SceneLoadRequest request, Action<bool> onComplete)
        {
            IsLoading = true;
            Progress = 0f;
            bool success = true;

            if (!CanLoad(request.PrimaryScene) || (request.HasAdditive && !CanLoad(request.AdditiveScene)))
            {
                success = false;
            }
            else
            {
                float weight = request.HasAdditive ? 0.5f : 1f;
                yield return Track(SceneManager.LoadSceneAsync(request.PrimaryScene, LoadSceneMode.Single), 0f, weight);

                if (request.HasAdditive)
                {
                    yield return Track(SceneManager.LoadSceneAsync(request.AdditiveScene, LoadSceneMode.Additive), 0.5f, 0.5f);
                    Scene additive = SceneManager.GetSceneByName(request.AdditiveScene);
                    if (additive.IsValid())
                    {
                        SceneManager.SetActiveScene(additive);
                    }
                }
            }

            Progress = 1f;
            _events.Publish(new LoadingProgressEvent(1f));
            IsLoading = false;
            onComplete?.Invoke(success);
        }

        private IEnumerator Track(AsyncOperation operation, float offset, float weight)
        {
            if (operation == null)
            {
                yield break;
            }

            while (!operation.isDone)
            {
                // Unity reports 0..0.9 for loading and jumps to 1 on activation.
                Progress = offset + (Mathf.Clamp01(operation.progress / 0.9f) * weight);
                _events.Publish(new LoadingProgressEvent(Progress));
                yield return null;
            }
        }

        private static bool CanLoad(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[DanhCu] Scene '{sceneName}' is not in Build Settings. Run 'Danh Cu > Setup > Run Project Setup'.");
                return false;
            }

            return true;
        }
    }
}
