using System;
using System.Collections.Generic;
using System.Linq;
using Rocket.Multiplayer.Core;
using Rocket.Multiplayer.Rooms;

namespace Rocket.Multiplayer.Matchmaking
{
    public class LocalMatchmakingService : IMatchmakingService
    {
        readonly IRoomService roomService;
        readonly MultiplayerConfig config;
        readonly List<RoomInfo> foundRooms = new List<RoomInfo>();
        bool searching;

        public event Action<RoomInfo> MatchFound;
        public event Action<RoomInfo> MatchCreated;
        public event Action<string> SearchFailed;

        public LocalMatchmakingService(IRoomService roomService, MultiplayerConfig config)
        {
            this.roomService = roomService;
            this.config = config;
        }

        public void FindMatch()
        {
            if (searching)
                return;

            searching = true;
            foundRooms.Clear();

            roomService.RoomFound += OnRoomFound;
            roomService.SearchCompleted += OnSearchResolved;
            roomService.SearchTimedOut += OnSearchResolved;

            roomService.FindPublicRooms(config.RoomSearchTimeoutSeconds);
        }

        public void CreateMatch()
        {
            RoomInfo room = roomService.CreateRoom(new RoomCreateRequest(config.DefaultRoomName, RoomVisibility.Public, config.MaxPlayers));
            MatchCreated?.Invoke(room);
        }

        public void CancelSearch()
        {
            if (!searching)
                return;

            Unsubscribe();
            roomService.CancelSearch();
            searching = false;
        }

        void OnRoomFound(RoomInfo room)
        {
            if (room.Visibility == RoomVisibility.Public && room.CanJoin)
                foundRooms.Add(room);
        }

        void OnSearchResolved()
        {
            if (!searching)
                return;

            Unsubscribe();
            searching = false;

            // Prefer the most-filled room that still has room, per spec.
            RoomInfo best = foundRooms.Where(r => r.CanJoin).OrderByDescending(r => r.CurrentPlayers).FirstOrDefault();

            if (best != null)
            {
                roomService.JoinRoom(best);
                MatchFound?.Invoke(best);
            }
            else
            {
                CreateMatch();
            }
        }

        void Unsubscribe()
        {
            roomService.RoomFound -= OnRoomFound;
            roomService.SearchCompleted -= OnSearchResolved;
            roomService.SearchTimedOut -= OnSearchResolved;
        }
    }
}
