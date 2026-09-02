using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices.RTCAudio;
using System;

namespace EpicTransport
{
    /// <summary>
    /// Wraps the EOS Lobby RTC Room (voice chat) feature. Lobbies created via
    /// <see cref="EOSTransport.CreateLobby"/> have their RTC room enabled automatically, and every
    /// member automatically joins that room's voice chat as soon as they join the lobby.
    /// </summary>
    public static class VoiceChat
    {
        public static bool IsMuted { get; private set; }
        public static bool IsRTCConnected { get; private set; }

        public static event Action<bool> OnMuteChanged;
        public static event Action<bool> OnRTCConnectionChanged;

        private static ulong notifyId;

        static VoiceChat()
        {
            EOSTransport.OnJoinedLobby += OnJoinedLobby;
            EOSTransport.OnLeftLobby += OnLeftLobby;
        }

        private static void OnJoinedLobby(string lobbyId)
        {
            IsMuted = false;

            AddNotifyRTCRoomConnectionChangedOptions notifyopt = new AddNotifyRTCRoomConnectionChangedOptions();
            notifyId = EOSManager.GetLobbyInterface().AddNotifyRTCRoomConnectionChanged(ref notifyopt, null, (ref RTCRoomConnectionChangedCallbackInfo cb) =>
            {
                if (cb.LobbyId != lobbyId || cb.LocalUserId != EOSManager.LocalUserProductID) return;

                IsRTCConnected = cb.IsConnected;
                TransportLogger.Log($"Voice room connection changed: {cb.IsConnected} ({cb.DisconnectReason})");
                OnRTCConnectionChanged?.Invoke(cb.IsConnected);

                if (cb.IsConnected) SetMuted(false);
            });
        }

        private static void OnLeftLobby()
        {
            EOSManager.GetLobbyInterface().RemoveNotifyRTCRoomConnectionChanged(notifyId);
            IsRTCConnected = false;
            IsMuted = false;
        }

        private static bool TryGetRoomName(out Utf8String roomName)
        {
            roomName = default;
            if (!EOSTransport.ConnectedToLobby) return false;

            GetRTCRoomNameOptions opt = new GetRTCRoomNameOptions()
            {
                LobbyId = EOSTransport.ConnectedLobbyInfo.LobbyId,
                LocalUserId = EOSManager.LocalUserProductID
            };

            Result res = EOSManager.GetLobbyInterface().GetRTCRoomName(ref opt, out Utf8String outBuffer);
            if (res != Result.Success)
            {
                TransportLogger.LogWarning($"Failed to get RTC room name: {res}");
                return false;
            }

            roomName = outBuffer;
            return true;
        }

        /// <summary>
        /// Mutes/unmutes the local player's microphone in the current lobby's voice room.
        /// </summary>
        public static void SetMuted(bool muted)
        {
            if (!TryGetRoomName(out Utf8String roomName)) return;

            UpdateSendingOptions updateopt = new UpdateSendingOptions()
            {
                LocalUserId = EOSManager.LocalUserProductID,
                RoomName = roomName,
                AudioStatus = muted ? RTCAudioStatus.Disabled : RTCAudioStatus.Enabled
            };

            EOSManager.GetRTCAudioInterface().UpdateSending(ref updateopt, null, (ref UpdateSendingCallbackInfo cb) =>
            {
                if (cb.ResultCode != Result.Success)
                {
                    TransportLogger.LogError($"Failed to update voice sending status: {cb.ResultCode}");
                    return;
                }

                IsMuted = muted;
                OnMuteChanged?.Invoke(muted);
            });
        }

        public static void ToggleMute() => SetMuted(!IsMuted);
    }
}
