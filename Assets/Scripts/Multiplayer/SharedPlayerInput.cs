using UnityEngine;
using UnityEngine.InputSystem;

namespace Rocket.Multiplayer
{
    public class SharedPlayerInput : MonoBehaviour
    {
        [Header("Optional Input Action References")]
        [SerializeField] private InputActionReference moveActionReference;
        [SerializeField] private InputActionReference lookActionReference;
        [SerializeField] private InputActionReference jumpActionReference;

        [Header("Look Settings")]
        [SerializeField] private float mouseLookScale = 0.08f;

        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private bool ownsActions;

        private Vector2 mobileMove;
        private Vector2 accumulatedMobileLook;
        private bool mobileJumpQueued;

        public Vector2 Move
        {
            get
            {
                Vector2 actionMove = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
                Vector2 combined = mobileMove.sqrMagnitude > actionMove.sqrMagnitude ? mobileMove : actionMove;
                return Vector2.ClampMagnitude(combined, 1f);
            }
        }

        private void Awake()
        {
            if (moveActionReference != null && lookActionReference != null && jumpActionReference != null)
            {
                moveAction = moveActionReference.action;
                lookAction = lookActionReference.action;
                jumpAction = jumpActionReference.action;
                ownsActions = false;
                return;
            }

            ownsActions = true;

            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");

            lookAction = new InputAction("Look", InputActionType.Value);
            lookAction.AddBinding("<Mouse>/delta");
            lookAction.AddBinding("<Gamepad>/rightStick");

            jumpAction = new InputAction("Jump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");
        }

        private void OnEnable()
        {
            moveAction?.Enable();
            lookAction?.Enable();
            jumpAction?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.Disable();
            lookAction?.Disable();
            jumpAction?.Disable();
        }

        private void OnDestroy()
        {
            if (!ownsActions)
            {
                return;
            }

            moveAction?.Dispose();
            lookAction?.Dispose();
            jumpAction?.Dispose();
        }

        public Vector2 ConsumeLook()
        {
            Vector2 actionLook = lookAction != null ? lookAction.ReadValue<Vector2>() * mouseLookScale : Vector2.zero;
            Vector2 combinedLook = actionLook + accumulatedMobileLook;
            accumulatedMobileLook = Vector2.zero;
            return combinedLook;
        }

        public bool ConsumeJumpPressed()
        {
            bool jumpPressed = (jumpAction != null && jumpAction.WasPressedThisFrame()) || mobileJumpQueued;
            mobileJumpQueued = false;
            return jumpPressed;
        }

        public void SetMobileMove(Vector2 value)
        {
            mobileMove = Vector2.ClampMagnitude(value, 1f);
        }

        public void AddMobileLook(Vector2 delta)
        {
            accumulatedMobileLook += delta;
        }

        public void QueueMobileJump()
        {
            mobileJumpQueued = true;
        }
    }
}
