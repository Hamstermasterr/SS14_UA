using Content.Server.Database;
using Content.Shared.Preferences;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.Sich.Sponsors;

/// <summary>
/// Менеджер налаштувань спонсорів. Кешує дані при підключенні та надає зручний API для інших систем.
/// </summary>
public sealed class SponsorManager : ISponsorManager, IPostInjectInit
{
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;

    // Cache player prefs on the server so we don't need as much async hell related to them.
    private readonly Dictionary<NetUserId, PlayerSponsorData> _cachedPlayerPrefs = new();

    private ISawmill _sawmill = default!;

    public void Init()
    {
        _netManager.RegisterNetMessage<MsgPreferencesAndSettings>();
        _sawmill = _log.GetSawmill("sponsorPrefs");
    }

    #region Lifecycle & Database Loading

    // Should only be called via UserDbDataManager.
    public async Task<PlayerSponsorData> LoadData(ICommonSession session, CancellationToken cancel = default)
    {
        if (!ShouldStorePrefs(session.Channel.AuthType))
        {
            // Don't store data for guests.
            var sponsorData = new PlayerSponsorData
            {
                SponsorLoaded = true,
                Sponsor = null
            };

            _cachedPlayerPrefs[session.UserId] = sponsorData;
            return sponsorData;
        }
        else
        {
            var sponsorData = new PlayerSponsorData();
            var loadTask = LoadPrefs();
            _cachedPlayerPrefs[session.UserId] = sponsorData;

            await loadTask;

            async Task LoadPrefs()
            {
                var spons = await GetOrCreateSponsorAsync(session.UserId, cancel);
                sponsorData.Sponsor = spons;
            }
            return sponsorData;
        }
    }

    public void FinishLoad(ICommonSession session)
    {
        var sponsData = _cachedPlayerPrefs[session.UserId];
        sponsData.SponsorLoaded = true;
    }

    public void OnClientDisconnected(ICommonSession session)
    {
        _cachedPlayerPrefs.Remove(session.UserId);
    }

    public bool HavePreferencesLoaded(ICommonSession session)
    {
        return _cachedPlayerPrefs.ContainsKey(session.UserId);
    }

    private async Task<SichSponsor?> GetOrCreateSponsorAsync(NetUserId userId, CancellationToken cancel)
    {
        var prefs = await _db.GetSponsorDataForAsync(userId, cancel);
        return prefs;
    }

    internal static bool ShouldStorePrefs(LoginType loginType)
    {
        return loginType.HasStaticUserId();
    }

    void IPostInjectInit.PostInject()
    {
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnFinishLoad(FinishLoad);
        _userDb.AddOnPlayerDisconnect(OnClientDisconnected);
    }

    #endregion

    #region Raw Data Access

    public bool TryGetCachedSponsor(NetUserId userId, [NotNullWhen(true)] out SichSponsor? playerSponsor)
    {
        if (_cachedPlayerPrefs.TryGetValue(userId, out var spons))
        {
            playerSponsor = spons.Sponsor;
            return spons.Sponsor != null;
        }

        playerSponsor = null;
        return false;
    }

    public SichSponsor GetSponsor(NetUserId userId)
    {
        var spons = _cachedPlayerPrefs[userId].Sponsor;
        if (spons == null)
        {
            throw new InvalidOperationException("Preferences for this player have not loaded yet.");
        }

        return spons;
    }

    public SichSponsor? GetSichSponsorOrNull(NetUserId? userId)
    {
        if (userId == null)
            return null;

        if (_cachedPlayerPrefs.TryGetValue(userId.Value, out var spons))
            return spons.Sponsor;
        return null;
    }

    #endregion

    #region Feature Helpers (Фасад)

    public bool HasTag(NetUserId userId, string tag)
    {
        if (!TryGetCachedSponsor(userId, out var sponsor) || sponsor.RoleAssignments == null)
            return false;

        return sponsor.RoleAssignments.Any(ra =>
            ra.Rank != null && ra.Rank.Tags != null && ra.Rank.Tags.Any(t => t.TagValue == tag));
    }

    public string? GetGhostColor(NetUserId userId)
    {
        if (!TryGetCachedSponsor(userId, out var sponsor) || string.IsNullOrEmpty(sponsor.SelectedGhostColor) || sponsor.RoleAssignments == null)
            return null;

        var canSetColor = sponsor.RoleAssignments.Any(ra => ra.Rank != null && ra.Rank.CanSetGhostColor);
        return canSetColor ? sponsor.SelectedGhostColor : null;
    }

    public string? GetOocColor(NetUserId userId)
    {
        if (!TryGetCachedSponsor(userId, out var sponsor) || string.IsNullOrEmpty(sponsor.SelectedOocColor) || sponsor.RoleAssignments == null)
            return null;

        var canSetColor = sponsor.RoleAssignments.Any(ra => ra.Rank != null && ra.Rank.CanSetOocColor);
        return canSetColor ? sponsor.SelectedOocColor : null;
    }

    #endregion

    #region Cache Management

    public async Task ReloadSponsorsAsync()
    {
        _cachedPlayerPrefs.Clear();
        var chanels = _netManager.Channels.ToList();
        foreach (var chanel in chanels)
        {
            if (!chanel.IsConnected)
                continue;

            var session = _playerManager.GetSessionByChannel(chanel);
            if (session == null)
                continue;

            await LoadData(session);
        }
    }

    public async Task ReloadSponsorAsync(NetUserId userId, CancellationToken cancel = default)
    {
        // Не вантажимо з БД, якщо гравця немає на сервері
        if (!_playerManager.TryGetSessionById(userId, out _))
            return;

        var spons = await GetOrCreateSponsorAsync(userId, cancel);

        if (_cachedPlayerPrefs.TryGetValue(userId, out var data))
        {
            data.Sponsor = spons;
        }
        else
        {
            _cachedPlayerPrefs[userId] = new PlayerSponsorData { SponsorLoaded = true, Sponsor = spons };
        }
    }

    public void UpdateCache(NetUserId userId, SichSponsor updatedSponsor)
    {
        if (_cachedPlayerPrefs.TryGetValue(userId, out var data))
        {
            data.Sponsor = updatedSponsor;
        }
        else
        {
            _cachedPlayerPrefs[userId] = new PlayerSponsorData { SponsorLoaded = true, Sponsor = updatedSponsor };
        }
    }

    #endregion
}

public sealed class PlayerSponsorData
{
    public bool SponsorLoaded;
    public SichSponsor? Sponsor;
}
