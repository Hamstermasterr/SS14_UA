using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Sich.Sponsors;
using Robust.Shared.Player;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Content.Server.Sich.Sponsors.UI;

public sealed class PersonalSponsorEui : BaseEui
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ISponsorManager _sponsorManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    private bool _isLoading = true;
    private SichSponsor? _cachedSponsor;

    public PersonalSponsorEui()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = _logManager.GetSawmill("sponsors.personal");
    }

    public override void Opened()
    {
        base.Opened();
        LoadDataAsync();
    }

    private async void LoadDataAsync()
    {
        _isLoading = true;
        StateDirty();

        // Завантажуємо спонсора зі всіма його ролями та рангами
        _cachedSponsor = await _db.GetSponsorDataForAsync(Player.UserId);

        _isLoading = false;
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        if (_isLoading || _cachedSponsor == null)
        {
            // Якщо ще вантажимо, або гравець не спонсор взагалі (відправляємо порожні дозволи)
            return new PersonalSponsorSettingsEuiState(
                false, false, null, null, null, null, new List<PersonalSponsorRankInfo>());
        }

        // 1. Вираховуємо глобальні права гравця (чи є хоча б один ранг, що це дозволяє)
        var canSetCustomGhostColor = _cachedSponsor.RoleAssignments.Any(ra => ra.Rank != null && ra.Rank.CanSetGhostColor);
        var canSetCustomOocColor = _cachedSponsor.RoleAssignments.Any(ra => ra.Rank != null && ra.Rank.CanSetOocColor);

        // 2. Формуємо відсортований список доступних рангів для вибору фіксованих кольорів
        var allowedRanks = _cachedSponsor.RoleAssignments
            .Where(ra => ra.Rank != null && ra.Rank.ShowInSponsorWindow)
            // Сортуємо: чим менше число, тим вище пріоритет
            .OrderBy(ra => ra.Rank!.Priority)
            .Select(ra => new PersonalSponsorRankInfo
            {
                Id = ra.Rank!.Id,
                Name = ra.Rank.Name,
                DefaultColor = ra.Rank.DefaultColor,
                FixedGhostColor = ra.Rank.DefaultGhostColor,
                FixedOocColor = ra.Rank.DefaultOocColor
            })
            .ToList();

        // 3. Відправляємо стан на клієнт
        return new PersonalSponsorSettingsEuiState(
            canSetCustomGhostColor,
            canSetCustomOocColor,
            _cachedSponsor.SelectedGhostColor,
            _cachedSponsor.SelectedOocColor,
            _cachedSponsor.SelectedGhostRankId,
            _cachedSponsor.SelectedOocRankId,
            allowedRanks
        );
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is PersonalSponsorEuiMsg.UpdateSettings updateMsg)
        {
            await HandleUpdateSettingsAsync(updateMsg);
        }
    }

    private async Task HandleUpdateSettingsAsync(PersonalSponsorEuiMsg.UpdateSettings msg)
    {
        if (_cachedSponsor == null)
            return; // Гравець не спонсор, ігноруємо

        bool isModified = false;

        // --- ВАЛІДАЦІЯ ТА ЗБЕРЕЖЕННЯ КОЛЬОРУ ПРИВИДА ---
        var canSetGhostColor = _cachedSponsor.RoleAssignments.Any(ra => ra.Rank != null && ra.Rank.CanSetGhostColor);

        // Перевіряємо кастомний колір
        if (canSetGhostColor)
        {
            if (_cachedSponsor.SelectedGhostColor != msg.NewGhostColor)
            {
                _cachedSponsor.SelectedGhostColor = msg.NewGhostColor;
                isModified = true;
            }
        }
        else if (!string.IsNullOrEmpty(msg.NewGhostColor))
        {
            _sawmill.Warning($"Гравець {Player.UserId} спробував встановити кастомний колір привида без прав!");
        }

        // Перевіряємо обраний ранг для кольору
        if (msg.SelectedGhostRankId != _cachedSponsor.SelectedGhostRankId)
        {
            // Переконуємось, що цей ранг дійсно належить гравцю
            if (msg.SelectedGhostRankId == null || _cachedSponsor.RoleAssignments.Any(ra => ra.RankId == msg.SelectedGhostRankId))
            {
                _cachedSponsor.SelectedGhostRankId = msg.SelectedGhostRankId;
                isModified = true;
            }
            else
            {
                _sawmill.Warning($"Гравець {Player.UserId} спробував обрати чужий ранг ({msg.SelectedGhostRankId}) для кольору привида!");
            }
        }


        // --- ВАЛІДАЦІЯ ТА ЗБЕРЕЖЕННЯ КОЛЬОРУ OOC ---
        var canSetOocColor = _cachedSponsor.RoleAssignments.Any(ra => ra.Rank != null && ra.Rank.CanSetOocColor);

        // Перевіряємо кастомний колір
        if (canSetOocColor)
        {
            if (_cachedSponsor.SelectedOocColor != msg.NewOocColor)
            {
                _cachedSponsor.SelectedOocColor = msg.NewOocColor;
                isModified = true;
            }
        }
        else if (!string.IsNullOrEmpty(msg.NewOocColor))
        {
            _sawmill.Warning($"Гравець {Player.UserId} спробував встановити кастомний колір OOC без прав!");
        }

        // Перевіряємо обраний ранг для кольору
        if (msg.SelectedOocRankId != _cachedSponsor.SelectedOocRankId)
        {
            if (msg.SelectedOocRankId == null || _cachedSponsor.RoleAssignments.Any(ra => ra.RankId == msg.SelectedOocRankId))
            {
                _cachedSponsor.SelectedOocRankId = msg.SelectedOocRankId;
                isModified = true;
            }
            else
            {
                _sawmill.Warning($"Гравець {Player.UserId} спробував обрати чужий ранг ({msg.SelectedOocRankId}) для OOC!");
            }
        }

        // --- ЗБЕРЕЖЕННЯ В БАЗУ ТА ОНОВЛЕННЯ КЕШУ ---
        if (isModified)
        {
            await _db.UpdateSponsorAsync(_cachedSponsor);

            // Оновлюємо кеш менеджера миттєво, щоб нові налаштування працювали відразу (без перезаходу)
            _sponsorManager.UpdateCache(Player.UserId, _cachedSponsor);

            _sawmill.Info($"Гравець {Player.UserId} успішно оновив свої персональні налаштування спонсора.");

            // Оновлюємо UI, щоб показати, що збереження пройшло успішно
            StateDirty();
        }
    }
}
