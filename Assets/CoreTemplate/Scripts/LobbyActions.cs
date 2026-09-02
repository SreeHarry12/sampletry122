using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

using Attribute = Epic.OnlineServices.Lobby.Attribute;

namespace EpicTransport
{
    [Serializable] public class StringUnityEvent : UnityEvent<string> { }
    [Serializable] public class ProductUserIdUnityEvent : UnityEvent<ProductUserId> { }
    [Serializable] public class LobbyMemberStatusUnityEvent : UnityEvent<LobbyMemberStatusReceivedCallbackInfo> { }
    [Serializable] public class LobbyUpdateUnityEvent : UnityEvent<LobbyUpdateReceivedCallbackInfo> { }

    /// <summary>
    /// A single player currently in the lobby. Kept in sync by <see cref="LobbyActions"/> as
    /// players join/leave/get updated - use <see cref="LobbyActions.Players"/> to read the live list.
    /// </summary>
    public class LobbyPlayer
    {
        public ProductUserId UserId { get; internal set; }
        public string DisplayName { get; internal set; }
        public bool IsOwner { get; internal set; }

        /// <summary>
        /// Every lobby member attribute for this player (includes DisplayName), keyed by attribute name.
        /// Add more attributes later (e.g. "score", "team", "ready") with <see cref="LobbyActions.UpdateMemberAttribute"/>
        /// and they'll show up here automatically once <see cref="LobbyActions.OnPlayerUpdated"/> fires.
        /// </summary>
        public readonly Dictionary<string, object> Attributes = new Dictionary<string, object>();

        // Add your own strongly-typed gameplay properties here as you decide on them, e.g.:
        // public int Score;
        // public bool IsReady;

        internal LobbyPlayer(ProductUserId userId) { UserId = userId; }

        public object GetAttribute(string key) => Attributes.TryGetValue(key, out object value) ? value : null;
    }

    /// <summary>
    /// Single entry point for every lobby action and every lobby callback/event.
    /// Wraps <see cref="EOSTransport"/>'s lobby methods and the raw EOS lobby notifications
    /// so the rest of the game only has to talk to this one script.
    /// </summary>
    public class LobbyActions : MonoBehaviour
    {
        public static LobbyActions instance { get; private set; }

        #region Events (code subscribers)

        /// <summary>Fired when the local player successfully creates a lobby. arg: lobby id.</summary>
        public static event Action<string> OnLobbyCreated;

        /// <summary>Fired when the local player successfully joins an existing lobby. arg: lobby id.</summary>
        public static event Action<string> OnLobbyJoined;

        /// <summary>Fired after the local player leaves the current lobby (voluntarily or via LeaveLobby).</summary>
        public static event Action OnLobbyLeft;

        /// <summary>Fired when the current lobby is destroyed/closed while the local player was in it. arg: lobby id.</summary>
        public static event Action<string> OnLobbyDestroyed;

        /// <summary>Fired when any lobby-wide attribute is updated.</summary>
        public static event Action<LobbyUpdateReceivedCallbackInfo> OnLobbyUpdated;

        /// <summary>Fired when a player joins the current lobby.</summary>
        public static event Action<ProductUserId> OnPlayerJoined;

        /// <summary>Fired when a player explicitly leaves the current lobby.</summary>
        public static event Action<ProductUserId> OnPlayerLeft;

        /// <summary>Fired when a player unexpectedly disconnects from the current lobby.</summary>
        public static event Action<ProductUserId> OnPlayerDisconnected;

        /// <summary>Fired when a player is kicked from the current lobby.</summary>
        public static event Action<ProductUserId> OnPlayerKicked;

        /// <summary>Fired when a player is promoted to lobby owner.</summary>
        public static event Action<ProductUserId> OnPlayerPromoted;

        /// <summary>Fired when a player's member attributes are updated (e.g. via <see cref="UpdateMemberAttribute"/>).</summary>
        public static event Action<ProductUserId> OnPlayerUpdated;

        /// <summary>Fired whenever a lobby member status notification is received, before it's split into the events above. arg: the raw callback info.</summary>
        public static event Action<LobbyMemberStatusReceivedCallbackInfo> OnLobbyMemberStatusReceived;

        /// <summary>Fired whenever <see cref="Players"/> changes (join/leave/kick/promote/attribute update).</summary>
        public static event Action OnPlayerListChanged;

        #endregion

        #region Events (Inspector - drag a GameObject + pick a method, like a Button's OnClick)

        [Header("Lobby Events")]
        [SerializeField] private StringUnityEvent lobbyCreated;
        [SerializeField] private StringUnityEvent lobbyJoined;
        [SerializeField] private UnityEvent lobbyLeft;
        [SerializeField] private StringUnityEvent lobbyDestroyed;
        [SerializeField] private LobbyUpdateUnityEvent lobbyUpdated;

        [Header("Player Events")]
        [SerializeField] private ProductUserIdUnityEvent playerJoined;
        [SerializeField] private ProductUserIdUnityEvent playerLeft;
        [SerializeField] private ProductUserIdUnityEvent playerDisconnected;
        [SerializeField] private ProductUserIdUnityEvent playerKicked;
        [SerializeField] private ProductUserIdUnityEvent playerPromoted;
        [SerializeField] private ProductUserIdUnityEvent playerUpdated;

        [Header("Raw")]
        [SerializeField] private LobbyMemberStatusUnityEvent lobbyMemberStatusReceived;
        [SerializeField] private UnityEvent playerListChanged;

        #endregion

        /// <summary>The live list of players currently in the lobby. Kept up to date automatically.</summary>
        public static IReadOnlyList<LobbyPlayer> Players => instance.players;

        private readonly List<LobbyPlayer> players = new List<LobbyPlayer>();

        private bool creatingLobby;

        private ulong memberUpdateNotifyId;
        private ulong lobbyUpdateNotifyId;
        private bool subscribedToLobbyNotifications;

        private void Awake()
        {
            instance = this;
        }

        private void OnEnable()
        {
            EOSTransport.OnJoinedLobby += HandleJoinedLobby;
            EOSTransport.OnLeftLobby += HandleLeftLobby;
            EOSTransport.OnLobbyMemberStatusUpdated += HandleMemberStatusUpdated;
        }

        private void OnDisable()
        {
            EOSTransport.OnJoinedLobby -= HandleJoinedLobby;
            EOSTransport.OnLeftLobby -= HandleLeftLobby;
            EOSTransport.OnLobbyMemberStatusUpdated -= HandleMemberStatusUpdated;

            UnregisterLobbyNotifications();
        }

        #region Actions

        /// <summary>Creates a public lobby, then starts the Mirror host if successful.</summary>
        public static void CreateLobby(string lobbyId, uint maxPlayers, List<AttributeData> attributes = null, string bucketId = null, LobbyPermissionLevel permissionLevel = LobbyPermissionLevel.Publicadvertised, bool presenceEnabled = false)
        {
            instance.creatingLobby = true;
            EOSTransport.CreateLobby(lobbyId, maxPlayers, attributes, bucketId, permissionLevel, presenceEnabled);
        }

        /// <summary>Joins a lobby based on the provided <see cref="LobbyDetails"/>. Starts the Mirror client if successful.</summary>
        public static void JoinLobby(LobbyDetails lobby) => EOSTransport.JoinLobby(lobby);

        /// <summary>Joins a lobby using its id. Requires the lobby to have join-by-id enabled.</summary>
        public static void JoinLobbyByID(string id) => EOSTransport.JoinLobbyByID(id);

        /// <summary>Finds all currently open lobbies.</summary>
        public static void FindLobbies(Action<List<LobbyDetails>> callback, uint maxResults = 200) => EOSTransport.FindLobbies(callback, maxResults);

        /// <summary>Searches for lobbies based on a lobby attribute.</summary>
        public static void SearchForLobbiesByAttribute(AttributeData attribute, uint maxResults, Action<List<LobbyDetails>> callback) => EOSTransport.SearchForLobbiesByAttribute(attribute, maxResults, callback);

        /// <summary>Searches for a lobby by its id.</summary>
        public static void SearchForLobbiesByID(string id, Action<List<LobbyDetails>> callback, uint maxResults = 1) => EOSTransport.SearchForLobbiesByID(id, callback, maxResults);

        /// <summary>Searches for the lobby a specific member is in.</summary>
        public static void SearchForLobbiesByMember(ProductUserId member, Action<List<LobbyDetails>> callback, uint maxResults = 1) => EOSTransport.SearchForLobbiesByMember(member, callback, maxResults);

        /// <summary>Leaves/destroys the currently connected lobby and stops the Mirror client/server.</summary>
        public static void LeaveLobby() => EOSTransport.LeaveLobby();

        /// <summary>Kicks the provided lobby member from the lobby. Owner only.</summary>
        public static void KickMember(ProductUserId member) => EOSTransport.KickMember(member);

        /// <summary>Kicks the lobby member with the given connection id. Owner only.</summary>
        public static void KickMember(int connectionId) => EOSTransport.KickMember(connectionId);

        /// <summary>Promotes the provided lobby member to become the new lobby owner. Owner only.</summary>
        public static void PromoteMember(ProductUserId newOwner, Action<bool> success) => EOSTransport.PromoteMember(newOwner, success);

        /// <summary>Adds/updates an attribute on the current lobby. Owner only.</summary>
        public static void UpdateLobbyAttribute(AttributeData attribute, LobbyAttributeVisibility visibility = LobbyAttributeVisibility.Public) => EOSTransport.UpdateAttribute(attribute, visibility);

        /// <summary>Adds/updates an attribute on the local player within the current lobby.</summary>
        public static void UpdateMemberAttribute(AttributeData attribute, LobbyAttributeVisibility visibility = LobbyAttributeVisibility.Public) => EOSTransport.UpdateMemberAttribute(attribute, visibility);

        #endregion

        #region Callback Handlers

        private void HandleJoinedLobby(string lobbyId)
        {
            bool wasCreating = creatingLobby;
            creatingLobby = false;

            RegisterLobbyNotifications();
            RefreshPlayerList();

            if (wasCreating)
            {
                OnLobbyCreated?.Invoke(lobbyId);
                lobbyCreated?.Invoke(lobbyId);
            }
            else
            {
                OnLobbyJoined?.Invoke(lobbyId);
                lobbyJoined?.Invoke(lobbyId);
            }
        }

        private void HandleLeftLobby()
        {
            UnregisterLobbyNotifications();

            players.Clear();
            NotifyPlayerListChanged();

            OnLobbyLeft?.Invoke();
            lobbyLeft?.Invoke();
        }

        private void HandleMemberStatusUpdated(LobbyMemberStatusReceivedCallbackInfo cb)
        {
            OnLobbyMemberStatusReceived?.Invoke(cb);
            lobbyMemberStatusReceived?.Invoke(cb);

            switch (cb.CurrentStatus)
            {
                case LobbyMemberStatus.Joined:
                    AddPlayer(cb.TargetUserId);
                    OnPlayerJoined?.Invoke(cb.TargetUserId);
                    playerJoined?.Invoke(cb.TargetUserId);
                    break;

                case LobbyMemberStatus.Left:
                    RemovePlayer(cb.TargetUserId);
                    OnPlayerLeft?.Invoke(cb.TargetUserId);
                    playerLeft?.Invoke(cb.TargetUserId);
                    break;

                case LobbyMemberStatus.Disconnected:
                    RemovePlayer(cb.TargetUserId);
                    OnPlayerDisconnected?.Invoke(cb.TargetUserId);
                    playerDisconnected?.Invoke(cb.TargetUserId);
                    break;

                case LobbyMemberStatus.Kicked:
                    RemovePlayer(cb.TargetUserId);
                    OnPlayerKicked?.Invoke(cb.TargetUserId);
                    playerKicked?.Invoke(cb.TargetUserId);
                    break;

                case LobbyMemberStatus.Promoted:
                    UpdatePlayerOwner(cb.TargetUserId);
                    OnPlayerPromoted?.Invoke(cb.TargetUserId);
                    playerPromoted?.Invoke(cb.TargetUserId);
                    break;

                case LobbyMemberStatus.Closed:
                    string lobbyId = EOSTransport.ConnectedLobbyInfo?.LobbyId;

                    players.Clear();
                    NotifyPlayerListChanged();

                    OnLobbyDestroyed?.Invoke(lobbyId);
                    lobbyDestroyed?.Invoke(lobbyId);
                    break;
            }
        }

        private void RegisterLobbyNotifications()
        {
            if (subscribedToLobbyNotifications) return;
            subscribedToLobbyNotifications = true;

            AddNotifyLobbyMemberUpdateReceivedOptions memberUpdateOpt = new AddNotifyLobbyMemberUpdateReceivedOptions();
            memberUpdateNotifyId = EOSManager.GetLobbyInterface().AddNotifyLobbyMemberUpdateReceived(ref memberUpdateOpt, null, (ref LobbyMemberUpdateReceivedCallbackInfo cb) =>
            {
                RefreshPlayerAttributes(cb.TargetUserId);
                OnPlayerUpdated?.Invoke(cb.TargetUserId);
                playerUpdated?.Invoke(cb.TargetUserId);
            });

            AddNotifyLobbyUpdateReceivedOptions lobbyUpdateOpt = new AddNotifyLobbyUpdateReceivedOptions();
            lobbyUpdateNotifyId = EOSManager.GetLobbyInterface().AddNotifyLobbyUpdateReceived(ref lobbyUpdateOpt, null, (ref LobbyUpdateReceivedCallbackInfo cb) =>
            {
                OnLobbyUpdated?.Invoke(cb);
                lobbyUpdated?.Invoke(cb);
            });
        }

        private void UnregisterLobbyNotifications()
        {
            if (!subscribedToLobbyNotifications) return;
            subscribedToLobbyNotifications = false;

            EOSManager.GetLobbyInterface().RemoveNotifyLobbyMemberUpdateReceived(memberUpdateNotifyId);
            EOSManager.GetLobbyInterface().RemoveNotifyLobbyUpdateReceived(lobbyUpdateNotifyId);
        }

        #endregion

        #region Player List

        private ProductUserId GetLobbyOwner()
        {
            LobbyDetails details = EOSTransport.ConnectedLobbyInfo?.CurrentLobbyDetails;
            if (details == null) return null;

            LobbyDetailsGetLobbyOwnerOptions ownerOpt = new LobbyDetailsGetLobbyOwnerOptions();
            return details.GetLobbyOwner(ref ownerOpt);
        }

        private void RefreshPlayerList()
        {
            players.Clear();

            LobbyInfo info = EOSTransport.ConnectedLobbyInfo;
            if (info?.CurrentLobbyDetails == null) { NotifyPlayerListChanged(); return; }

            ProductUserId owner = GetLobbyOwner();
            uint count = info.GetPlayerCount();

            for (uint i = 0; i < count; i++)
            {
                ProductUserId id = info.GetMemberByIndex(i);
                LobbyPlayer player = new LobbyPlayer(id) { IsOwner = id == owner };
                FetchMemberAttributes(player);
                players.Add(player);
            }

            NotifyPlayerListChanged();
        }

        private void AddPlayer(ProductUserId id)
        {
            if (players.Exists(p => p.UserId == id)) return; //already known about (e.g. from the initial RefreshPlayerList)

            LobbyPlayer player = new LobbyPlayer(id) { IsOwner = id == GetLobbyOwner() };
            FetchMemberAttributes(player);
            players.Add(player);

            NotifyPlayerListChanged();
        }

        private void RemovePlayer(ProductUserId id)
        {
            if (players.RemoveAll(p => p.UserId == id) > 0) NotifyPlayerListChanged();
        }

        private void UpdatePlayerOwner(ProductUserId newOwner)
        {
            foreach (LobbyPlayer player in players) player.IsOwner = player.UserId == newOwner;
            NotifyPlayerListChanged();
        }

        private void RefreshPlayerAttributes(ProductUserId id)
        {
            LobbyPlayer player = players.Find(p => p.UserId == id);
            if (player == null) return;

            FetchMemberAttributes(player);
            NotifyPlayerListChanged();
        }

        private void FetchMemberAttributes(LobbyPlayer player)
        {
            LobbyDetails details = EOSTransport.ConnectedLobbyInfo?.CurrentLobbyDetails;
            if (details == null) return;

            player.Attributes.Clear();

            LobbyDetailsGetMemberAttributeCountOptions countOpt = new LobbyDetailsGetMemberAttributeCountOptions() { TargetUserId = player.UserId };
            uint count = details.GetMemberAttributeCount(ref countOpt);

            for (uint i = 0; i < count; i++)
            {
                LobbyDetailsCopyMemberAttributeByIndexOptions copyOpt = new LobbyDetailsCopyMemberAttributeByIndexOptions() { TargetUserId = player.UserId, AttrIndex = i };
                if (details.CopyMemberAttributeByIndex(ref copyOpt, out Attribute? attr) != Result.Success || attr?.Data == null) continue;

                AttributeData data = attr.Value.Data.Value;
                object value = data.Value.ValueType switch
                {
                    AttributeType.String => (string)data.Value.AsUtf8,
                    AttributeType.Int64 => data.Value.AsInt64,
                    AttributeType.Double => data.Value.AsDouble,
                    AttributeType.Boolean => data.Value.AsBool,
                    _ => null
                };

                player.Attributes[data.Key] = value;
            }

            if (player.Attributes.TryGetValue(EOSTransport.DisplayNameKey, out object name)) player.DisplayName = name as string;
        }

        private void NotifyPlayerListChanged()
        {
            OnPlayerListChanged?.Invoke();
            playerListChanged?.Invoke();
        }

        #endregion
    }
}
