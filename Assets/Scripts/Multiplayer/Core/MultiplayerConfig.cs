using UnityEngine;

namespace Rocket.Multiplayer.Core
{
    /// <summary>
    /// Single source of truth for multiplayer tunables. Loaded once by MultiplayerBootstrap
    /// via Resources.Load("Multiplayer/MultiplayerConfig") before any other multiplayer object exists.
    /// </summary>
    [CreateAssetMenu(fileName = "MultiplayerConfig", menuName = "Rocket/Multiplayer/Multiplayer Config")]
    public class MultiplayerConfig : ScriptableObject
    {
        [Header("Room")]
        [Min(2)] public int MaxPlayers = 4;
        [Min(4)] public int RoomKeyLength = 5;
        public string RoomKeyAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        public string DefaultRoomName = "Cute Arena";

        [Header("Network")]
        public ushort NetworkPort = 7777;
        public int DiscoveryBroadcastPort = 47777;

        [Header("Timeouts")]
        [Min(1f)] public float RoomSearchTimeoutSeconds = 5f;
        [Min(1f)] public float ConnectionTimeoutSeconds = 8f;
        [Min(0.5f)] public float DiscoveryActiveIntervalSeconds = 2f;

        [Header("Player")]
        [Min(0.1f)] public float PlayerMovementSpeed = 5f;
    }
}
