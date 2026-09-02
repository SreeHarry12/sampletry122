using Mirror;
using System.Collections.Generic;

namespace EpicTransport
{
    public class GameNetworkManager : NetworkManager
    {
        [UnityEngine.SerializeField] private int playersToStart = 2;

        private readonly List<NetworkConnectionToClient> waitingPlayers = new();
        private bool gameStarted;

        public override void OnStartServer()
        {
            base.OnStartServer();
            waitingPlayers.Clear();
            gameStarted = false;
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            if (gameStarted)
            {
                base.OnServerAddPlayer(conn);
                return;
            }

            waitingPlayers.Add(conn);
            TransportLogger.Log($"Player joined ({waitingPlayers.Count}/{playersToStart}). Waiting for more players...");

            if (waitingPlayers.Count >= playersToStart) StartGame();
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            waitingPlayers.Remove(conn);
            base.OnServerDisconnect(conn);
        }

        private void StartGame()
        {
            gameStarted = true;
            TransportLogger.Log($"{playersToStart} players joined. Starting game and spawning players.");

            foreach (NetworkConnectionToClient conn in waitingPlayers) base.OnServerAddPlayer(conn);
            waitingPlayers.Clear();
        }
    }
}
