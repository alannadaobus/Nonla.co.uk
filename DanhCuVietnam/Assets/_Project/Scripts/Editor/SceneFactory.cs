using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace DanhCu.Editor
{
    /// <summary>Creates the placeholder scenes. Existing scenes are never touched.</summary>
    internal static class SceneFactory
    {
        public static bool AnyMissing()
        {
            return !Exists(ProjectPaths.BootstrapScene)
                || !Exists(ProjectPaths.MainMenuScene)
                || !Exists(ProjectPaths.GameplayScene)
                || !Exists(ProjectPaths.DefaultArenaScene);
        }

        public static void EnsureScenes()
        {
            if (!AnyMissing())
            {
                return;
            }

            CreateIfMissing(ProjectPaths.BootstrapScene, () =>
            {
                // Intentionally almost empty: GameBootstrapper creates the GameManager from code.
                CreateCamera(new Vector3(0f, 1f, -10f), Color.black);
            });

            CreateIfMissing(ProjectPaths.MainMenuScene, () =>
            {
                CreateCamera(new Vector3(0f, 1f, -10f), new Color(0.12f, 0.09f, 0.07f));
                CreateSun();
                CreateEventSystem();
            });

            CreateIfMissing(ProjectPaths.GameplayScene, () =>
            {
                // Tops are ~7 cm tall: the camera sits close and low, looking at the ring.
                Camera camera = CreateCamera(new Vector3(0f, 0.9f, -1.3f), new Color(0.5f, 0.6f, 0.7f));
                camera.transform.LookAt(Vector3.zero);
                camera.nearClipPlane = 0.01f;
                camera.fieldOfView = 45f;
                CreateSun();
                CreateEventSystem();
            });

            CreateIfMissing(ProjectPaths.DefaultArenaScene, () =>
            {
                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground_PackedEarth";
                ground.transform.localScale = new Vector3(0.5f, 1f, 0.5f); // 5 m x 5 m

                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "Ring_Marker";
                ring.transform.position = new Vector3(0f, 0.0005f, 0f);
                ring.transform.localScale = new Vector3(1.2f, 0.0005f, 1.2f); // 0.6 m radius
                Object.DestroyImmediate(ring.GetComponent<Collider>());
            });
        }

        private static void CreateIfMissing(string path, System.Action populate)
        {
            if (Exists(path))
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            populate();
            EditorSceneManager.SaveScene(scene, path);
        }

        private static bool Exists(string path) => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null;

        private static Camera CreateCamera(Vector3 position, Color background)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = position;
            Camera camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            go.AddComponent<AudioListener>();
            return camera;
        }

        private static void CreateSun()
        {
            var go = new GameObject("Sun");
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            light.color = new Color(1f, 0.95f, 0.86f);
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            InputSystemUIInputModule module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }
    }
}
