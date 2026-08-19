using Mirror;
using Rocket.Multiplayer.Utilities;

namespace Rocket.Multiplayer.Core
{
    /// <summary>
    /// Mirror's lobby-scene player object (NetworkRoomPlayer is Mirror's own internal room-plumbing
    /// concept - unrelated to the app-facing RoomInfo/IRoomService "room"). Carries only what the
    /// lobby UI needs: a display name.
    /// </summary>
    public class RoomPlayer : NetworkRoomPlayer
    {
        /// <summary>Set by the UI immediately before Host()/Join() so the freshly spawned RoomPlayer
        /// can push it to the server on start - Mirror gives no other way to pass spawn arguments.</summary>
        public static string PendingLocalPlayerName;

        [SyncVar(hook = nameof(OnPlayerNameChanged))]
        public string PlayerName = string.Empty;

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            string desiredName = PlayerNameGenerator.ResolveName(PendingLocalPlayerName);
            CmdSetPlayerName(desiredName);
        }

        [Command]
        public void CmdSetPlayerName(string requestedName)
        {
            PlayerName = PlayerNameGenerator.ResolveName(requestedName);
        }

        void OnPlayerNameChanged(string oldName, string newName)
        {
            CustomNetworkManager.Instance?.RaiseRoomPlayersChanged();
        }

        public override void OnClientEnterRoom()
        {
            CustomNetworkManager.Instance?.RaiseRoomPlayersChanged();
        }

        public override void OnClientExitRoom()
        {
            CustomNetworkManager.Instance?.RaiseRoomPlayersChanged();
        }

        // Suppress Mirror's legacy IMGUI room UI - this project uses its own UI layer.
        public override void OnGUI() { }
    }
}
