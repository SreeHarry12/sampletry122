using Mirror;
using My.DemoScene;
using UnityEngine;

namespace Rocket.Multiplayer.Player
{
    /// <summary>
    /// Server-authoritative session, client-authoritative movement: matches the NetworkTransformReliable
    /// (syncDirection: ClientToServer) and NetworkAnimator (clientAuthority: true) already configured on
    /// this prefab. The owning client simulates its own CharacterController every frame for responsiveness;
    /// NetworkTransformReliable relays the result through the server to everyone else, who see it
    /// interpolated. Remote (non-owned) instances never run movement locally - only the owner ever calls
    /// CharacterController.Move, so a client can never control another player's character.
    /// Movement logic adapted from the existing (non-networked) My.DemoScene.ThirdPersonController demo
    /// script in Cute_Characters_Pack_1, driving the same Anim_Controller parameters.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class NetworkPlayerController : NetworkBehaviour
    {
        [Header("Wiring")]
        public PlayerInputReader playerInput;
        public GameObject mobileInputUI;
        public Transform cameraPivot;
        public Camera playerCamera;
        public AudioListener audioListener;

        [Header("Movement")]
        public float moveSpeed = 5f;
        public float rotationSharpness = 12f;
        public float gravity = -25f;
        public float jumpSpeed = 8f;
        public float groundedVerticalVelocity = -2f;

        [Header("Look (reserved - the shared ThirdPersonCamera rig owns look input today)")]
        public float pitchSensitivity = 1f;
        public float minPitch = -35f;
        public float maxPitch = 60f;
        public bool lockCursorOnDesktop = true;

        [Header("Camera Rig")]
        public Vector3 cameraPivotOffset = new Vector3(0f, 1.6f, 0f);
        public float cameraDistance = 4.5f;
        public float cameraPositionSmoothness = 14f;

        [SyncVar]
        public string PlayerName = string.Empty;

        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

        CharacterController characterController;
        Animator animator;
        NetworkAnimator networkAnimator;
        Transform cameraTransform;
        float verticalVelocity;

        void Awake()
        {
            characterController = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            networkAnimator = GetComponent<NetworkAnimator>();

            if (playerInput == null)
                playerInput = GetComponent<PlayerInputReader>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            // Remote (non-owned) instances never read input locally - their transform arrives via
            // NetworkTransformReliable instead.
            if (playerInput != null)
                playerInput.enabled = isOwned;
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            if (playerInput != null)
                playerInput.enabled = true;

            WireLocalCamera();

            if (lockCursorOnDesktop && !Application.isMobilePlatform)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void OnDestroy()
        {
            if (isOwned && lockCursorOnDesktop && !Application.isMobilePlatform)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        void WireLocalCamera()
        {
            Camera targetCamera = playerCamera != null ? playerCamera : Camera.main;
            if (targetCamera == null)
                return;

            playerCamera = targetCamera;
            cameraTransform = targetCamera.transform;

            ThirdPersonCamera rig = targetCamera.GetComponent<ThirdPersonCamera>();
            if (rig == null)
                return;

            rig.target = cameraPivot != null ? cameraPivot : transform;
            rig.height = cameraPivotOffset.y;
            rig.distance = cameraDistance;
            rig.isControlEnabled = true;
        }

        void Update()
        {
            if (!isOwned || playerInput == null || characterController == null)
                return;

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            bool isGrounded = characterController.isGrounded;

            HandleMovement();
            HandleJump(isGrounded);
            ApplyGravity(isGrounded);
            UpdateAnimation(isGrounded);
        }

        void HandleMovement()
        {
            Vector2 move = playerInput.MoveInput;
            if (move.sqrMagnitude < 0.01f)
                return;

            float cameraYaw = cameraTransform != null ? cameraTransform.eulerAngles.y : transform.eulerAngles.y;
            float targetYaw = cameraYaw + Mathf.Atan2(move.x, move.y) * Mathf.Rad2Deg;

            float smoothYaw = Mathf.LerpAngle(transform.eulerAngles.y, targetYaw, rotationSharpness * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, smoothYaw, 0f);

            characterController.Move(transform.forward * moveSpeed * Time.deltaTime);
        }

        void HandleJump(bool isGrounded)
        {
            if (!isGrounded || !playerInput.JumpPressed)
                return;

            verticalVelocity = jumpSpeed;

            if (networkAnimator != null)
                networkAnimator.SetTrigger("Jump");
            else
                animator.SetTrigger("Jump");
        }

        void ApplyGravity(bool isGrounded)
        {
            if (isGrounded && verticalVelocity < 0f)
                verticalVelocity = groundedVerticalVelocity;

            verticalVelocity += gravity * Time.deltaTime;
            characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }

        void UpdateAnimation(bool isGrounded)
        {
            if (animator == null)
                return;

            animator.SetBool(IsGroundedHash, isGrounded);

            float speed01 = Mathf.Clamp01(playerInput.MoveInput.magnitude);
            animator.SetFloat(SpeedHash, speed01, 0.1f, Time.deltaTime);
        }

        [Server]
        public void ServerSetPlayerName(string playerName)
        {
            PlayerName = playerName;
        }
    }
}
