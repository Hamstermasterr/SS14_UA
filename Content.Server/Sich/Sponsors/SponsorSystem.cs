using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.Sich.Sponsors.UI; // Тут будуть жити твої класи EUI
using Content.Shared.Sich.Sponsors;
using Robust.Shared.Player;

namespace Content.Server.Sich.Sponsors;

public sealed partial class SponsorSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestPersonalSponsorWindowMessage>(OnRequestPersonalWindow);
        SubscribeNetworkEvent<RequestAdminSponsorWindowMessage>(OnRequestAdminWindow);
        SubscribeNetworkEvent<RequestSponsorListWindowMessage>(OnRequestSponsorListWindow);
    }

    private void OnRequestPersonalWindow(RequestPersonalSponsorWindowMessage ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not { } session)
            return;

        OpenPersonalEui(session);
    }

    private void OnRequestAdminWindow(RequestAdminSponsorWindowMessage ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not { } session)
            return;

        // БЕЗПЕКА: Перевіряємо, чи має користувач права адміністратора
        if (!_adminManager.IsAdmin(session))
            return;

        OpenAdminEui(session);
    }

    private void OnRequestSponsorListWindow(RequestSponsorListWindowMessage ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not { } session) return;

        // Тут перевірка на адміна не потрібна, бо це публічний список
        OpenSponsorListEui(session);
    }

    public void OpenPersonalEui(ICommonSession session)
    {
        var eui = new PersonalSponsorEui();
        _euiManager.OpenEui(eui, session);
        eui.StateDirty();
    }

    public void OpenAdminEui(ICommonSession session)
    {
        var eui = new AdminSponsorsEui();
        _euiManager.OpenEui(eui, session);
        eui.StateDirty();
    }

    public void OpenSponsorListEui(ICommonSession session)
    {
        var eui = new SponsorListEui();
        _euiManager.OpenEui(eui, session);
        eui.StateDirty();
    }
}
