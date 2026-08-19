using Mirror;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private Vector3 cameraPivotOffset = new Vector3(0f, 1.6f, 0f);
        [SerializeField] private float cameraDistance = 4.5f;
        [SerializeField] private float cameraPositionSmoothness = 14f;

        [SyncVar(hook = nameof(OnPlayerNameChanged))]
        private string playerName = "Player";

        [SyncVar]
        private int playerNumber;

        private CharacterController characterController;
        private Vector2 serverMoveInput;
        private float serverYaw;
        private bool serverJumpQueued;
        private float verticalVelocity;
        private float localYaw;
        private float localPitch;
        private Transform runtimeCameraPivot;
        private Vector3 currentCameraVelocity;
        private Text worldNameText;

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

            EnsureWorldNameLabel();
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

        private void LateUpdate()
        {
            if (!isOwned || playerCamera == null)
            {
                return;
            }

            UpdateCameraFollow(Time.deltaTime);
        }

        private void InitializeLocalPlayer()
        {
            EnsureRuntimeReferences();
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

        private void EnsureRuntimeReferences()
        {
            if (playerInput == null)
            {
                playerInput = GetComponent<SharedPlayerInput>();
            }

            if (mobileInputUI == null)
            {
                mobileInputUI = FindFirstObjectByType<MobileInputUI>(FindObjectsInactive.Include);
            }

            if (cameraPivot == null)
            {
                GameObject pivotObject = new GameObject("CameraPivot");
                runtimeCameraPivot = pivotObject.transform;
                runtimeCameraPivot.SetParent(transform, false);
                runtimeCameraPivot.localPosition = cameraPivotOffset;
                cameraPivot = runtimeCameraPivot;
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;

                if (playerCamera == null)
                {
                    playerCamera = FindFirstObjectByType<Camera>();
                }
            }

            if (audioListener == null && playerCamera != null)
            {
                audioListener = playerCamera.GetComponent<AudioListener>();
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

        private void UpdateCameraFollow(float deltaTime)
        {
            if (cameraPivot == null)
            {
                return;
            }

            Quaternion yawRotation = Quaternion.Euler(0f, localYaw, 0f);
            Quaternion pitchRotation = Quaternion.Euler(localPitch, localYaw, 0f);
            Vector3 desiredPosition = cameraPivot.position - (pitchRotation * Vector3.forward * cameraDistance);

            playerCamera.transform.position = Vector3.SmoothDamp(
                playerCamera.transform.position,
                desiredPosition,
                ref currentCameraVelocity,
                1f / Mathf.Max(cameraPositionSmoothness, 0.01f),
                Mathf.Infinity,
                deltaTime);

            playerCamera.transform.rotation = Quaternion.LookRotation(cameraPivot.position - playerCamera.transform.position, Vector3.up);
        }

        [Server]
        public void ServerSetPlayerName(string syncedName, int syncedNumber)
        {
            playerName = syncedName;
            playerNumber = syncedNumber;
        }

        private void OnPlayerNameChanged(string oldValue, string newValue)
        {
            EnsureWorldNameLabel();

            if (worldNameText != null)
            {
                worldNameText.text = $"{playerNumber:00} {newValue}";
            }
        }

        private void EnsureWorldNameLabel()
        {
            if (worldNameText != null)
            {
                return;
            }

            Transform labelRoot = new GameObject("WorldNameLabel").transform;
            labelRoot.SetParent(transform, false);
            labelRoot.localPosition = new Vector3(0f, 2.4f, 0f);

            Canvas canvas = labelRoot.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(240f, 48f);
            canvas.scaleFactor = 10f;

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(labelRoot, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            worldNameText = textObject.GetComponent<Text>();
            worldNameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            worldNameText.alignment = TextAnchor.MiddleCenter;
            worldNameText.color = Color.white;
            worldNameText.text = playerName;
            worldNameText.raycastTarget = false;
        }
    }
}
