using Mirror;
using UnityEngine;

namespace Rocket.Multiplayer
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NetworkIdentity))]
    public class NetworkThirdPersonController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private SharedPlayerInput playerInput;
        [SerializeField] private MobileInputUI mobileInputUI;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private AudioListener audioListener;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSharpness = 12f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float jumpSpeed = 8f;
        [SerializeField] private float groundedVerticalVelocity = -2f;

        [Header("Look")]
        [SerializeField] private float pitchSensitivity = 1f;
        [SerializeField] private float minPitch = -35f;
        [SerializeField] private float maxPitch = 70f;
        [SerializeField] private bool lockCursorOnDesktop = true;

        private CharacterController characterController;
        private Vector2 serverMoveInput;
        private float serverYaw;
        private bool serverJumpQueued;
        private float verticalVelocity;
        private float localYaw;
        private float localPitch;

        public override void OnStartAuthority()
        {
            InitializeLocalPlayer();
        }

        public override void OnStartClient()
        {
            if (isOwned)
            {
                return;
            }

            SetLocalOnlyObjectsActive(false);

            if (playerInput != null)
            {
                playerInput.enabled = false;
            }
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (playerInput == null)
            {
                playerInput = GetComponent<SharedPlayerInput>();
            }

            if (audioListener == null && playerCamera != null)
            {
                audioListener = playerCamera.GetComponent<AudioListener>();
            }
        }

        private void Update()
        {
            if (isOwned)
            {
                ProcessLocalInput();
            }

            if (isServer)
            {
                SimulateAuthoritativeMovement(Time.deltaTime);
            }
        }

        private void InitializeLocalPlayer()
        {
            SetLocalOnlyObjectsActive(true);

            if (playerInput != null)
            {
                playerInput.enabled = true;
            }

            if (mobileInputUI != null)
            {
                mobileInputUI.BindInput(playerInput);
                mobileInputUI.ApplyVisibility();
            }

            localYaw = transform.eulerAngles.y;

            if (lockCursorOnDesktop && !Application.isMobilePlatform)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void SetLocalOnlyObjectsActive(bool isActive)
        {
            if (playerCamera != null)
            {
                playerCamera.enabled = isActive;
            }

            if (audioListener != null)
            {
                audioListener.enabled = isActive;
            }

            if (cameraPivot != null)
            {
                cameraPivot.gameObject.SetActive(isActive);
            }
        }

        private void ProcessLocalInput()
        {
            if (playerInput == null)
            {
                return;
            }

            Vector2 lookDelta = playerInput.ConsumeLook();
            localYaw += lookDelta.x;
            localPitch = Mathf.Clamp(localPitch - lookDelta.y * pitchSensitivity, minPitch, maxPitch);

            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(localPitch, 0f, 0f);
            }

            transform.rotation = Quaternion.Euler(0f, localYaw, 0f);

            Vector2 moveInput = playerInput.Move;
            bool jumpPressed = playerInput.ConsumeJumpPressed();
            CmdSetInput(moveInput, localYaw, jumpPressed);
        }

        [Command]
        private void CmdSetInput(Vector2 moveInput, float yaw, bool jumpPressed)
        {
            serverMoveInput = Vector2.ClampMagnitude(moveInput, 1f);
            serverYaw = yaw;

            if (jumpPressed)
            {
                serverJumpQueued = true;
            }
        }

        [ServerCallback]
        private void SimulateAuthoritativeMovement(float deltaTime)
        {
            bool isGrounded = characterController.isGrounded;
            if (isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedVerticalVelocity;
            }

            Quaternion cameraYawRotation = Quaternion.Euler(0f, serverYaw, 0f);
            Vector3 desiredMove = cameraYawRotation * new Vector3(serverMoveInput.x, 0f, serverMoveInput.y);

            Quaternion targetRotation = desiredMove.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(desiredMove.normalized, Vector3.up)
                : cameraYawRotation;

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSharpness * deltaTime);

            if (serverJumpQueued && isGrounded)
            {
                verticalVelocity = jumpSpeed;
                serverJumpQueued = false;
            }

            verticalVelocity += gravity * deltaTime;

            Vector3 velocity = desiredMove * moveSpeed;
            velocity.y = verticalVelocity;

            characterController.Move(velocity * deltaTime);
        }
    }
}
