using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Administration.Systems;

/// <summary>
/// Tracks AHelp participants and persists player ratings of admins.
/// Limits: 1 rating per admin per UTC day, max 5 ratings per player per UTC day.
/// </summary>
public sealed class AdminHelpRatingSystem : EntitySystem
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly BwoinkSystem _bwoink = default!;

    private readonly Dictionary<NetUserId, Dictionary<NetUserId, string>> _participants = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AdminHelpRatingOpenRequestEvent>(OnOpenRequest);
        SubscribeNetworkEvent<AdminHelpRatingSubmitEvent>(OnSubmit);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _participants.Clear();
    }

    public void RecordParticipant(NetUserId playerChannel, NetUserId adminId, string adminName)
    {
        if (!_participants.TryGetValue(playerChannel, out var admins))
        {
            admins = new Dictionary<NetUserId, string>();
            _participants[playerChannel] = admins;
        }

        admins[adminId] = adminName;
    }

    private void OnOpenRequest(AdminHelpRatingOpenRequestEvent ev, EntitySessionEventArgs args)
    {
        _ = SendStateAsync(args.SenderSession);
    }

    private void OnSubmit(AdminHelpRatingSubmitEvent ev, EntitySessionEventArgs args)
    {
        _ = SubmitAsync(args.SenderSession, ev.AdminUserId, ev.Stars);
    }

    private async Task SendStateAsync(ICommonSession session)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var ratingsToday = await _db.GetAdminHelpRatingCountSince(session.UserId.UserId, todayUtc);
        var ratedAdminIds = await _db.GetRatedAdminUserIdsSince(session.UserId.UserId, todayUtc);
        var timeUntilReset = todayUtc.AddDays(1) - DateTime.UtcNow;
        if (timeUntilReset < TimeSpan.Zero)
            timeUntilReset = TimeSpan.Zero;

        var participants = GetParticipants(session.UserId, ratedAdminIds);

        RaiseNetworkEvent(
            new AdminHelpRatingStateEvent(
                ratingsToday,
                AdminHelpRatingPaths.MaxRatingsPerDay,
                timeUntilReset,
                participants),
            session);
    }

    private async Task SubmitAsync(ICommonSession session, NetUserId adminUserId, byte stars)
    {
        try
        {
            if (stars is < 1 or > 5)
            {
                await SendStateAsync(session);
                return;
            }

            if (adminUserId == session.UserId)
            {
                await SendStateAsync(session);
                return;
            }

            var todayUtc = DateTime.UtcNow.Date;

            if (await _db.GetAdminHelpRatingCountSince(session.UserId.UserId, todayUtc) >= AdminHelpRatingPaths.MaxRatingsPerDay)
            {
                await SendStateAsync(session);
                return;
            }

            if (await _db.HasPlayerRatedAdminToday(session.UserId.UserId, adminUserId.UserId, todayUtc))
            {
                await SendStateAsync(session);
                return;
            }

            if (!TryResolveParticipant(session.UserId, adminUserId, out var resolvedAdminId, out var adminName))
            {
                await SendStateAsync(session);
                return;
            }

            var added = await _db.TryAddAdminHelpRating(new AdminHelpRating
            {
                PlayerUserId = session.UserId.UserId,
                AdminUserId = resolvedAdminId.UserId,
                RoundId = _gameTicker.RoundId == 0 ? null : _gameTicker.RoundId,
                Stars = stars,
                CreatedAt = DateTime.UtcNow,
            });

            if (!added)
            {
                await SendStateAsync(session);
                return;
            }

            SendRatingChatMessage(session, adminName, stars);
            await SendStateAsync(session);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to submit admin help rating: {e}");
            await SendStateAsync(session);
        }
    }

    private void SendRatingChatMessage(ICommonSession session, string adminName, byte stars)
    {
        var text = Loc.GetString(
            "admin-help-rating-chat",
            ("player", session.Name),
            ("stars", stars),
            ("admin", adminName));

        _bwoink.SendPlayerChannelSystemMessage(session.UserId, $"[color=#B5B3BD]{text}[/color]");
    }

    private bool TryResolveParticipant(
        NetUserId playerId,
        NetUserId adminUserId,
        out NetUserId resolvedAdminId,
        out string adminName)
    {
        resolvedAdminId = adminUserId;
        adminName = string.Empty;

        if (!_participants.TryGetValue(playerId, out var admins) || admins.Count == 0)
            return false;

        if (admins.TryGetValue(adminUserId, out adminName!))
            return true;

        foreach (var (id, name) in admins)
        {
            if (id.UserId != adminUserId.UserId)
                continue;

            resolvedAdminId = id;
            adminName = name;
            return true;
        }

        return false;
    }

    private List<AdminHelpRatingParticipant> GetParticipants(NetUserId playerId, HashSet<Guid> ratedAdminIds)
    {
        if (!_participants.TryGetValue(playerId, out var admins) || admins.Count == 0)
            return [];

        return admins
            .Where(kv => !ratedAdminIds.Contains(kv.Key.UserId))
            .Select(kv => new AdminHelpRatingParticipant(kv.Key, kv.Value))
            .OrderBy(p => p.DisplayName)
            .ToList();
    }
}
