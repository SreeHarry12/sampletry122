using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Rocket.Multiplayer
{
    public class ArenaSceneBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            EnsureLighting();
            EnsureGround();
            EnsureSpawnManager();
            EnsureCamera();
            EnsureEventSystem();
            EnsureMobileCanvas();
        }

        private void EnsureLighting()
        {
            if (FindFirstObjectByType<Light>() != null)
            {
                return;
            }

            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private void EnsureGround()
        {
            if (GameObject.Find("ArenaGround") != null)
            {
                return;
            }

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "ArenaGround";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
        }

        private void EnsureSpawnManager()
        {
            if (FindFirstObjectByType<ArenaSpawnManager>() != null)
            {
                return;
            }

            GameObject managerObject = new GameObject("ArenaSpawnManager");
            ArenaSpawnManager manager = managerObject.AddComponent<ArenaSpawnManager>();

            Vector3[] positions =
            {
                new Vector3(-6f, 0f, -6f),
                new Vector3(6f, 0f, -6f),
                new Vector3(-6f, 0f, 6f),
                new Vector3(6f, 0f, 6f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject spawnPoint = new GameObject($"SpawnPoint_{i + 1:00}");
                spawnPoint.transform.SetParent(manager.transform, false);
                spawnPoint.transform.position = positions[i];
            }
        }

        private void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1000f;
            cameraObject.AddComponent<AudioListener>();
            camera.transform.position = new Vector3(0f, 3f, -7f);
            camera.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private void EnsureMobileCanvas()
        {
            if (FindFirstObjectByType<MobileInputUI>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject("MobileInputCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<GraphicRaycaster>();
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();

            MobileInputUI mobileInputUI = canvasObject.AddComponent<MobileInputUI>();
            mobileInputUI.ConfigureRuntimeCanvas(scaler);
        }
    }
}
