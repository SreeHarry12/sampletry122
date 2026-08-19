using System;
using Rocket.Multiplayer.Rooms;

namespace Rocket.Multiplayer.Matchmaking
{
    /// <summary>
    /// Public "Find Online Game" flow: search-then-join-or-create, driven purely through
    /// IRoomService so this never touches Mirror directly. Kept separate from IRoomService because
    /// "pick the best room or make one" is matchmaking policy, not room CRUD.
    /// </summary>
    public interface IMatchmakingService
    {
        event Action<RoomInfo> MatchFound;
        event Action<RoomInfo> MatchCreated;
        event Action<string> SearchFailed;

        void FindMatch();
        void CreateMatch();
        void CancelSearch();
    }
}
