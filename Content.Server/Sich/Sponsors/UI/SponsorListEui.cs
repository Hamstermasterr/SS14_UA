using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Sich.Sponsors;
using Robust.Shared.Maths;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.Sich.Sponsors.UI;

public sealed class SponsorListEui : BaseEui
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private readonly ISawmill _sawmill;
    private bool _isLoading = true;

    // Зберігаємо вже сформований і відсортований список для відправки
    private readonly List<PublicSponsorEntry> _publicSponsors = new();

    public SponsorListEui()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = _logManager.GetSawmill("sponsors.view");
    }

    public override void Opened()
    {
        base.Opened();
        LoadFromDb();
    }

    public override EuiStateBase GetNewState()
    {
        // Якщо ще вантажимо з БД - просто відправляємо порожній список, 
        // клієнт почекає наступного оновлення (коли ми викличемо StateDirty)
        if (_isLoading)
        {
            return new SponsorListEuiState(new List<PublicSponsorEntry>());
        }

        return new SponsorListEuiState(_publicSponsors);
    }

    // Нам більше не потрібен HandleMessage, бо в цьому вікні 
    // немає кнопок "Зберегти" чи "Видалити", воно лише для читання!

    private async void LoadFromDb()
    {
        _isLoading = true;
        StateDirty();

        var (sponsors, ranks) = await _db.GetAllSichSponsorsAsync();

        _publicSponsors.Clear();

        // Робимо зручний словник рангів для швидкого пошуку
        var ranksDict = ranks.ToDictionary(r => r.Id);

        // Формуємо та сортуємо список за допомогою LINQ
        var sortedSponsors = sponsors
            .Select(s =>
            {
                // 1. Беремо всі ролі гравця
                var validRanks = s.sponsor.RoleAssignments
                    .Where(ra => ranksDict.ContainsKey(ra.RankId))
                    .Select(ra => ranksDict[ra.RankId])
                    // 2. Відсіюємо ті ранги, які не мають показуватись у вікні
                    .Where(r => r.ShowInSponsorWindow)
                    .ToList();

                return new { SponsorData = s, Ranks = validRanks };
            })
            // 3. Відсіюємо гравців, у яких взагалі немає рангів (або всі приховані)
            .Where(x => x.Ranks.Count > 0)
            .Select(x =>
            {
                // 4. Знаходимо найвищий ранг гравця.
                // Оскільки менше число = вищий пріоритет (напр. 0 > 100), сортуємо по зростанню (OrderBy)
                var topRank = x.Ranks.OrderBy(r => r.Priority).First();

                var userName = string.IsNullOrEmpty(x.SponsorData.lastUserName)
                    ? x.SponsorData.sponsor.UserId.ToString()
                    : x.SponsorData.lastUserName;

                return new
                {
                    Entry = new PublicSponsorEntry
                    {
                        UserName = userName,
                        TopRankName = topRank.Name,
                        TopRankColor = Color.FromHex(topRank.DefaultColor)
                    },
                    // Зберігаємо пріоритет найвищого рангу для фінального сортування списку
                    TopPriority = topRank.Priority
                };
            })
            // 5. Сортуємо фінальний список: спочатку за пріоритетом рангу, потім за алфавітом
            .OrderBy(x => x.TopPriority)
            .ThenBy(x => x.Entry.UserName)
            .Select(x => x.Entry)
            .ToList();

        _publicSponsors.AddRange(sortedSponsors);

        _isLoading = false;
        StateDirty(); // Сповіщаємо клієнт, що дані завантажені і їх можна малювати
    }
}
