using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

[Serializable, NetSerializable]
public sealed record AdminHelpRatingParticipant(NetUserId UserId, string DisplayName);

[Serializable, NetSerializable]
public sealed class AdminHelpRatingStateEvent : EntityEventArgs
{
    public int RatingsToday { get; }
    public int MaxRatingsPerDay { get; }
    public TimeSpan TimeUntilReset { get; }
    public List<AdminHelpRatingParticipant> Participants { get; }

    public AdminHelpRatingStateEvent(
        int ratingsToday,
        int maxRatingsPerDay,
        TimeSpan timeUntilReset,
        List<AdminHelpRatingParticipant> participants)
    {
        RatingsToday = ratingsToday;
        MaxRatingsPerDay = maxRatingsPerDay;
        TimeUntilReset = timeUntilReset;
        Participants = participants;
    }
}

[Serializable, NetSerializable]
public sealed class AdminHelpRatingOpenRequestEvent : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class AdminHelpRatingSubmitEvent : EntityEventArgs
{
    public NetUserId AdminUserId { get; }
    public byte Stars { get; }

    public AdminHelpRatingSubmitEvent(NetUserId adminUserId, byte stars)
    {
        AdminUserId = adminUserId;
        Stars = stars;
    }
}

public static class AdminHelpRatingPaths
{
    public const string StarIconPath = "/Textures/_Mini/Interface/star.png";
    public const int MaxRatingsPerDay = 5;
}
