using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Lobby;

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace EpicTransport
{
    public class LoginUI : MonoBehaviour
    {
        [Header("EOS Product Config")]
        [Tooltip("Fill these in from the Epic Games Dev Portal for your product.")]
        [SerializeField] private string productName = "MyGame";
        [SerializeField] private string productId;
        [SerializeField] private string clientId;
        [SerializeField] private string clientSecret;
        [SerializeField] private string sandboxId;
        [SerializeField] private string deploymentId;
        [Tooltip("32-byte (64 hex character) key used to encrypt Title Storage and Player Data Storage.")]
        [SerializeField] private string encryptionKey;
        [SerializeField] private string fallbackDisplayName = "Player";

        [Header("Login Panel")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private Button loginButton;
        [SerializeField] private Text statusText;

        [Header("Lobby Panel")]
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private InputField lobbyNameInput;
        [SerializeField] private Button createLobbyButton;
        [SerializeField] private Button findLobbiesButton;
        [SerializeField] private Button leaveLobbyButton;
        [SerializeField] private Transform lobbyListContent;
        [SerializeField] private Text playerInfoText;
        [SerializeField] private Button muteButton;
        [SerializeField] private Text muteButtonText;

        private bool loggingIn;

        private void Awake()
        {
            if (loginButton != null) loginButton.onClick.AddListener(OnLoginClicked);
            createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
            findLobbiesButton.onClick.AddListener(OnFindLobbiesClicked);
            leaveLobbyButton.onClick.AddListener(() => EOSTransport.LeaveLobby());
            if (muteButton != null) muteButton.onClick.AddListener(() => VoiceChat.ToggleMute());

            lobbyPanel.SetActive(false);
            if (loginPanel != null) loginPanel.SetActive(false);

            OnLoginClicked();
        }

        private void Update()
        {
            if (EOSManager.Initialized && !lobbyPanel.activeSelf)
            {
                if (loginPanel != null) loginPanel.SetActive(false);
                lobbyPanel.SetActive(true);
            }

            if (statusText != null) statusText.text = loggingIn ? "Signing in..." : "Not signed in";
            if (loginButton != null) loginButton.interactable = !loggingIn;

            if (playerInfoText != null && EOSManager.Initialized)
                playerInfoText.text = $"Signed in as {EOSManager.DisplayName}\nID: {EOSManager.LocalUserProductIDString}";

            if (leaveLobbyButton != null) leaveLobbyButton.interactable = EOSTransport.ConnectedToLobby;
            if (createLobbyButton != null) createLobbyButton.interactable = !EOSTransport.ConnectedToLobby;

            if (muteButton != null) muteButton.interactable = EOSTransport.ConnectedToLobby;
            if (muteButtonText != null) muteButtonText.text = VoiceChat.IsMuted ? "Unmute" : "Mute";
        }

        private void OnLoginClicked()
        {
            loggingIn = true;

            EOSManager.Initialize(new TransportInitializeOptions()
            {
                ProductName = productName,
                ProductId = productId,
                ClientId = clientId,
                ClientSecret = clientSecret,
                SandboxId = sandboxId,
                DeploymentId = deploymentId,
                EncryptionKey = encryptionKey,
                DisplayName = fallbackDisplayName,

                AuthInterfaceCredentialType = LoginCredentialType.ExternalAuth,
                ConnectInterfaceCredentialType = ExternalCredentialType.DeviceidAccessToken
            });
        }

        private void OnCreateLobbyClicked()
        {
            string lobbyName = string.IsNullOrEmpty(lobbyNameInput.text) ? "My Lobby" : lobbyNameInput.text;
            EOSTransport.CreateLobby(lobbyName, 10);
        }

        private void OnFindLobbiesClicked()
        {
            foreach (Transform child in lobbyListContent) Destroy(child.gameObject);

            findLobbiesButton.interactable = false;
            EOSTransport.FindLobbies(results =>
            {
                findLobbiesButton.interactable = true;

                foreach (LobbyDetails details in results)
                {
                    LobbyDetailsCopyInfoOptions copyopt = new LobbyDetailsCopyInfoOptions();
                    details.CopyInfo(ref copyopt, out LobbyDetailsInfo? info);
                    if (!info.HasValue) continue;

                    LobbyDetailsGetMemberCountOptions memberCountOpt = new LobbyDetailsGetMemberCountOptions();
                    uint memberCount = details.GetMemberCount(ref memberCountOpt);

                    CreateLobbyListEntry(info.Value.LobbyId, memberCount, info.Value.MaxMembers, details);
                }
            });
        }

        private void CreateLobbyListEntry(string lobbyId, uint memberCount, uint maxMembers, LobbyDetails details)
        {
            GameObject row = new GameObject(lobbyId, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(lobbyListContent, false);
            row.GetComponent<LayoutElement>().minHeight = 30;

            GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(LayoutElement));
            labelGO.transform.SetParent(row.transform, false);
            labelGO.GetComponent<LayoutElement>().flexibleWidth = 1;
            Text label = labelGO.AddComponent<Text>();
            label.text = $"{lobbyId} ({memberCount}/{maxMembers})";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;

            GameObject buttonGO = new GameObject("JoinButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGO.transform.SetParent(row.transform, false);
            buttonGO.GetComponent<LayoutElement>().preferredWidth = 80;
            buttonGO.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f);
            buttonGO.GetComponent<Button>().onClick.AddListener(() => EOSTransport.JoinLobby(details));

            GameObject buttonTextGO = new GameObject("Text", typeof(RectTransform));
            buttonTextGO.transform.SetParent(buttonGO.transform, false);
            Text buttonText = buttonTextGO.AddComponent<Text>();
            buttonText.text = "Join";
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            RectTransform buttonTextRect = buttonTextGO.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
        }
    }
}
