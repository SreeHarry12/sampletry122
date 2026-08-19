using UnityEngine;
using UnityEngine.InputSystem;

namespace Rocket.Multiplayer.Player
{
    /// <summary>
    /// Plain (non-networked) input reader for the local player. Combines the New Input System
    /// (desktop keyboard, via optional action references or a Keyboard.current fallback) with
    /// SimpleInput's static axes (already-installed mobile joystick/button plugin) using the same
    /// "SimpleInput first, legacy Input fallback" precedence already used by this asset pack's own
    /// demo controller (Control.cs), so both playstyles work without a platform branch.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        public InputActionReference moveActionReference;
        public InputActionReference lookActionReference;
        public InputActionReference jumpActionReference;
        public float mouseLookScale = 0.08f;

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpPressed { get; private set; }

        void OnEnable()
        {
            moveActionReference?.action?.Enable();
            lookActionReference?.action?.Enable();
            jumpActionReference?.action?.Enable();
        }

        void OnDisable()
        {
            moveActionReference?.action?.Disable();
            lookActionReference?.action?.Disable();
            jumpActionReference?.action?.Disable();
        }

        void Update()
        {
            MoveInput = ReadMoveInput();
            LookInput = ReadLookInput();
            JumpPressed = ReadJumpPressed();
        }

        Vector2 ReadMoveInput()
        {
            if (moveActionReference != null && moveActionReference.action != null)
                return moveActionReference.action.ReadValue<Vector2>();

            float horizontal = SimpleInput.GetAxisRaw("Horizontal");
            float vertical = SimpleInput.GetAxisRaw("Vertical");

            if (Mathf.Abs(horizontal) < 0.01f && Mathf.Abs(vertical) < 0.01f && Keyboard.current != null)
            {
                horizontal = (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f);
                vertical = (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f);
            }

            return Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        }

        Vector2 ReadLookInput()
        {
            if (lookActionReference != null && lookActionReference.action != null)
                return lookActionReference.action.ReadValue<Vector2>() * mouseLookScale;

            return Vector2.zero;
        }

        bool ReadJumpPressed()
        {
            if (jumpActionReference != null && jumpActionReference.action != null)
                return jumpActionReference.action.WasPressedThisFrame();

            if (SimpleInput.GetButtonDown("Jump"))
                return true;

            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        }
    }
}
