using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EpicTransport
{
    public class PlayerController : NetworkBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;

        [SyncVar(hook = nameof(OnColorChanged))]
        private Color playerColor = Color.white;

        private Renderer visualRenderer;

        public override void OnStartServer()
        {
            playerColor = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f);
            transform.position = new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
        }

        public override void OnStartClient()
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = Vector3.zero;
            Destroy(visual.GetComponent<Collider>());

            visualRenderer = visual.GetComponent<Renderer>();

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            visualRenderer.material = new Material(shader);
            visualRenderer.material.color = playerColor;
        }

        private void OnColorChanged(Color oldColor, Color newColor)
        {
            if (visualRenderer != null) visualRenderer.material.color = newColor;
        }

        private void Update()
        {
            if (!isOwned) return;
            if (Keyboard.current == null) return;

            float h = 0f, v = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;

            Vector3 move = new Vector3(h, 0f, v) * moveSpeed * Time.deltaTime;
            if (move != Vector3.zero) transform.Translate(move, Space.World);
        }
    }
}
