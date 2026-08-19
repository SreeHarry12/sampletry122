namespace Rocket.Multiplayer.Core
{
    public enum RoomVisibility
    {
        Public,
        Private
    }

    public enum RoomStatus
    {
        Waiting,
        Full,
        InProgress
    }

    public enum DiscoveryQueryMode
    {
        PublicSearch,
        KeyLookup
    }

    public enum JoinFailureReason
    {
        None,
        RoomNotFound,
        RoomFull,
        RoomNotAccepting,
        ConnectFailed,
        HostUnavailable
    }
}
