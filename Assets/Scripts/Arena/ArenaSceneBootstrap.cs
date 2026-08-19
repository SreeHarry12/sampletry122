using Mirror;
using My.DemoScene;
using Rocket.Multiplayer.UI;
using SimpleInputNamespace;
using UnityEngine;
using UnityEngine.UI;

namespace Rocket.Multiplayer.Arena
{
    /// <summary>
    /// Builds the Arena scene's non-networked content at runtime on every client (ground, light,
    /// spawn points, camera, EventSystem, mobile controls) rather than relying on hand-authored scene
    /// YAML for a full level - the same low-risk pattern already used for the menu/lobby UI.
    /// </summary>
    public class ArenaSceneBootstrap : MonoBehaviour
    {
        [Tooltip("Assets/Plugins/SimpleInput/Prefabs/Joystick.prefab - reused as-is for movement input.")]
        [SerializeField] GameObject joystickPrefab;

        void Awake()
        {
            EnsureLight();
            EnsureGround();
            EnsureSpawnPoints();
            EnsureCamera();
            UiBuilder.EnsureEventSystem();
            EnsureMobileCanvas();
        }

        void EnsureLight()
        {
            if (FindFirstObjectByType<Light>() != null)
                return;

            GameObject go = new GameObject("Directional Light");
            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        void EnsureGround()
        {
            if (GameObject.Find("ArenaGround") != null)
                return;

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "ArenaGround";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(40f, 1f, 40f);
        }

        void EnsureSpawnPoints()
        {
            if (FindFirstObjectByType<NetworkStartPosition>() != null)
                return;

            Vector3[] positions =
            {
                new Vector3(-6f, 0.1f, -6f),
                new Vector3(6f, 0.1f, -6f),
                new Vector3(-6f, 0.1f, 6f),
                new Vector3(6f, 0.1f, 6f),
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject spawn = new GameObject($"SpawnPoint_{i + 1:00}");
                spawn.transform.position = positions[i];
                spawn.AddComponent<NetworkStartPosition>();
            }
        }

        void EnsureCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            if (cam.GetComponent<ThirdPersonCamera>() == null)
                cam.gameObject.AddComponent<ThirdPersonCamera>();
        }

        void EnsureMobileCanvas()
        {
            if (GameObject.Find("MobileInputCanvas") != null)
                return;

            Canvas canvas = UiBuilder.CreateCanvas(null, "MobileInputCanvas");

            BuildLookArea(canvas.transform);
            BuildJoystick(canvas.transform);
            BuildJumpButton(canvas.transform);
        }

        void BuildLookArea(Transform parent)
        {
            GameObject lookArea = new GameObject("LookArea", typeof(RectTransform), typeof(Image));
            lookArea.transform.SetParent(parent, false);

            RectTransform rect = lookArea.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Effectively invisible but still a valid raycast target for drag input.
            lookArea.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

            Touchpad touchpad = lookArea.AddComponent<Touchpad>();
            touchpad.xAxis.Key = "Mouse X";
            touchpad.yAxis.Key = "Mouse Y";
            touchpad.sensitivity = 1f;
        }

        void BuildJoystick(Transform parent)
        {
            GameObject joystickGo = joystickPrefab != null
                ? Instantiate(joystickPrefab, parent)
                : null;

            if (joystickGo == null)
            {
                Debug.LogWarning("ArenaSceneBootstrap: joystickPrefab not assigned - mobile movement joystick will be missing (desktop input still works).");
                return;
            }

            RectTransform rect = joystickGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(160f, 160f);
        }

        void BuildJumpButton(Transform parent)
        {
            GameObject jumpGo = new GameObject("JumpButton", typeof(RectTransform), typeof(Image));
            jumpGo.transform.SetParent(parent, false);

            RectTransform rect = jumpGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-140f, 140f);
            rect.sizeDelta = new Vector2(140f, 140f);

            jumpGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.35f);

            ButtonInputUI buttonInput = jumpGo.AddComponent<ButtonInputUI>();
            buttonInput.button.Key = "Jump";

            Text label = UiBuilder.CreateLabel(jumpGo.transform, "JUMP", 20);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.GetComponent<LayoutElement>().ignoreLayout = true;
        }
    }
}
