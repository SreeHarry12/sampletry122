using Mirror;
using UnityEngine;

namespace Rocket.Multiplayer
{
    public class RocketRoomPlayer : NetworkRoomPlayer
    {
        [SyncVar(hook = nameof(OnPlayerNameChanged))]
        private string playerName = "Player_01";

        [SyncVar]
        private int playerNumber;

        public string PlayerName => playerName;
        public int PlayerNumber => playerNumber;

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            CmdSetPlayerName(MultiplayerLocalSettings.PlayerName);
        }

        public override void OnClientEnterRoom()
        {
            base.OnClientEnterRoom();
            RocketNetworkRoomManager.Instance?.NotifyLobbyStateChanged();
        }

        public override void OnClientExitRoom()
        {
            base.OnClientExitRoom();
            RocketNetworkRoomManager.Instance?.NotifyLobbyStateChanged();
        }

        [Command]
        public void CmdSetPlayerName(string requestedName)
        {
            playerName = RocketNetworkRoomManager.SanitizePlayerName(requestedName);
        }

        [Server]
        public void ServerAssignNumber(int number)
        {
            playerNumber = number;
        }

        private void OnPlayerNameChanged(string oldValue, string newValue)
        {
            RocketNetworkRoomManager.Instance?.NotifyLobbyStateChanged();
        }
    }
}
