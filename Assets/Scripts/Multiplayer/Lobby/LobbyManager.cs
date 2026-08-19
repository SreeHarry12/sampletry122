using System;
using System.Collections.Generic;
using System.Linq;
using Rocket.Multiplayer.Core;

namespace Rocket.Multiplayer.Lobby
{
    /// <summary>
    /// Read-only view of the current lobby, event-driven off CustomNetworkManager.RoomPlayersChanged
    /// (fired from RoomPlayer's own SyncVar hook / enter-exit callbacks) - never polls roomSlots per frame.
    /// </summary>
    public class LobbyManager
    {
        readonly CustomNetworkManager networkManager;

        public event Action LobbyChanged;

        public LobbyManager(CustomNetworkManager networkManager)
        {
            this.networkManager = networkManager;
            networkManager.RoomPlayersChanged += () => LobbyChanged?.Invoke();
        }

        public IReadOnlyList<string> GetPlayerNames()
        {
            return networkManager.roomSlots
                .Where(p => p != null)
                .OrderBy(p => p.index)
                .Select(p => (p as RoomPlayer)?.PlayerName)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();
        }

        public int CurrentPlayerCount => networkManager.roomSlots.Count(p => p != null);
        public int MaxPlayers => networkManager.maxConnections;
        public string RoomName => networkManager.CurrentRoomName;
        public bool IsHost => Mirror.NetworkServer.active;
    }
}
