using Content.Client.Eui;
using Content.Client.Stylesheets;
using Content.Shared.Eui;
using Content.Shared.Sich.Sponsors;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;
using System.Linq;
using System.Numerics;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Sich.Sponsors.UI;

[UsedImplicitly]
public sealed partial class PersonalSponsorEui : BaseEui
{
    private readonly PersonalSponsorWindow _window;

    public PersonalSponsorEui()
    {
        IoCManager.InjectDependencies(this);

        _window = new PersonalSponsorWindow(this);
        _window.OnClose += CloseEverything;
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        SendMessage(new CloseEuiMessage());
        CloseEverything();
    }

    private void CloseEverything()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not PersonalSponsorSettingsEuiState s)
            return;

        _window.UpdateState(s);
    }

    public void SaveSettings(string? ghostColor, string? oocColor, int? ghostRankId, int? oocRankId)
    {
        SendMessage(new PersonalSponsorEuiMsg.UpdateSettings
        {
            NewGhostColor = ghostColor,
            NewOocColor = oocColor,
            SelectedGhostRankId = ghostRankId,
            SelectedOocRankId = oocRankId
        });
    }

    // =====================================================================
    // Вікно інтерфейсу
    // =====================================================================
    private sealed class PersonalSponsorWindow : DefaultWindow
    {
        private const int OptionNone = -1;
        private const int OptionCustom = -2;

        private readonly PersonalSponsorEui _eui;

        public readonly Label NameLabel;
        public readonly BoxContainer RanksContainer;

        public readonly OptionButton GhostDropdown;
        public readonly ColorSelectorSliders GhostColorPicker;

        public readonly OptionButton OocDropdown;
        public readonly ColorSelectorSliders OocColorPicker;

        public readonly Button SaveButton;

        public PersonalSponsorWindow(PersonalSponsorEui eui)
        {
            _eui = eui;
            Title = Loc.GetString("sponsors-eui-personal-title"); // "Налаштування Спонсора"
            MinSize = new Vector2(450, 550);

            var playerManager = IoCManager.Resolve<IPlayerManager>();
            var playerName = playerManager.LocalPlayer?.Name ?? "Гравець";

            NameLabel = new Label
            {
                Text = playerName,
                HorizontalAlignment = HAlignment.Center,
                StyleClasses = { StyleClass.LabelHeading }
            };

            // Вкладки (Tabs)
            var tabs = new TabContainer { VerticalExpand = true };

            // --- ВКЛАДКА 1: ОГЛЯД РАНГІВ ---
            RanksContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                VerticalExpand = true,
                Margin = new Thickness(5)
            };

            var overviewScroll = new ScrollContainer
            {
                VerticalExpand = true,
                Children = { RanksContainer }
            };

            var overviewBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Children = { new Label { Text = "Ваші активні ранги:", Margin = new Thickness(0, 0, 0, 10) }, overviewScroll }
            };
            TabContainer.SetTabTitle(overviewBox, Loc.GetString("sponsors-eui-personal-tab-overview")); // "Огляд"

            // --- ВКЛАДКА 2: КОЛЬОРИ ---
            GhostDropdown = new OptionButton { HorizontalExpand = true };
            GhostColorPicker = new ColorSelectorSliders
            {
                SelectorType = ColorSelectorSliders.ColorSelectorType.Hsv,
                Visible = false // Приховано за замовчуванням
            };

            GhostDropdown.OnItemSelected += args =>
            {
                GhostDropdown.SelectId(args.Id);
                GhostColorPicker.Visible = args.Id == OptionCustom;
            };

            var ghostSection = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Margin = new Thickness(0, 0, 0, 15),
                Children =
                {
                    new Label { Text = "Колір Привида", StyleClasses = { StyleClass.LabelHeading } },
                    GhostDropdown,
                    GhostColorPicker
                }
            };

            OocDropdown = new OptionButton { HorizontalExpand = true };
            OocColorPicker = new ColorSelectorSliders
            {
                SelectorType = ColorSelectorSliders.ColorSelectorType.Hsv,
                Visible = false
            };

            OocDropdown.OnItemSelected += args =>
            {
                OocDropdown.SelectId(args.Id);
                OocColorPicker.Visible = args.Id == OptionCustom;
            };

            var oocSection = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Children =
                {
                    new Label { Text = "Колір OOC чату", StyleClasses = { StyleClass.LabelHeading } },
                    OocDropdown,
                    OocColorPicker
                }
            };

            var colorsScroll = new ScrollContainer
            {
                VerticalExpand = true,
                Children =
                {
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        Children = { ghostSection, oocSection } // Тут все правильно, бо використовується синтаксис колекції
                    }
                }
            };
            TabContainer.SetTabTitle(colorsScroll, Loc.GetString("sponsors-eui-personal-tab-colors")); // "Кольори"

            // Додаємо вкладки
            tabs.AddChild(overviewBox);
            tabs.AddChild(colorsScroll);

            // Кнопка збереження
            SaveButton = new Button
            {
                Text = Loc.GetString("sponsors-eui-personal-save"), // "Зберегти налаштування"
                HorizontalAlignment = HAlignment.Right
            };
            SaveButton.OnPressed += OnSavePressed;

            // Головний контейнер вікна
            Contents.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Children = { NameLabel, tabs, SaveButton }
            });
        }

        public void UpdateState(PersonalSponsorSettingsEuiState state)
        {
            // 1. Фарбуємо нікнейм у колір найвищого рангу (оскільки вони відсортовані сервером, беремо перший)
            var topRank = state.AllowedRanks.FirstOrDefault();
            if (topRank.Name != null)
            {
                NameLabel.FontColorOverride = Color.FromHex(topRank.DefaultColor);
            }

            // 2. Малюємо плашки рангів
            RanksContainer.RemoveAllChildren();
            if (state.AllowedRanks.Count == 0)
            {
                RanksContainer.AddChild(new Label { Text = "У вас немає активних рангів.", StyleClasses = { StyleClass.Italic } });
            }
            else
            {
                foreach (var rank in state.AllowedRanks)
                {
                    var color = Color.FromHex(rank.DefaultColor);

                    // Створюємо гарну плашку (Panel) з напівпрозорим фоном
                    var panel = new PanelContainer
                    {
                        PanelOverride = new StyleBoxFlat
                        {
                            BackgroundColor = color.WithAlpha(0.15f),
                            BorderColor = color.WithAlpha(0.5f),
                            BorderThickness = new Thickness(1)
                        },
                        Margin = new Thickness(0, 0, 0, 5)
                    };

                    panel.AddChild(new Label
                    {
                        Text = rank.Name,
                        FontColorOverride = color,
                        Margin = new Thickness(10, 5),
                        HorizontalAlignment = HAlignment.Center
                    });

                    RanksContainer.AddChild(panel);
                }
            }

            // 3. Налаштовуємо випадні списки для кольорів
            PopulateDropdown(GhostDropdown, state.CanSetCustomGhostColor, state.AllowedRanks, true);
            PopulateDropdown(OocDropdown, state.CanSetCustomOocColor, state.AllowedRanks, false);

            // Встановлюємо збережені значення для Привида
            if (state.SelectedGhostRankId != null)
            {
                GhostDropdown.SelectId(state.SelectedGhostRankId.Value);
                GhostColorPicker.Visible = false;
            }
            else if (!string.IsNullOrEmpty(state.CurrentGhostColor) && state.CanSetCustomGhostColor)
            {
                GhostDropdown.SelectId(OptionCustom);
                GhostColorPicker.Visible = true;
                GhostColorPicker.Color = Color.FromHex(state.CurrentGhostColor);
            }
            else
            {
                GhostDropdown.SelectId(OptionNone);
                GhostColorPicker.Visible = false;
            }

            // Встановлюємо збережені значення для OOC
            if (state.SelectedOocRankId != null)
            {
                OocDropdown.SelectId(state.SelectedOocRankId.Value);
                OocColorPicker.Visible = false;
            }
            else if (!string.IsNullOrEmpty(state.CurrentOocColor) && state.CanSetCustomOocColor)
            {
                OocDropdown.SelectId(OptionCustom);
                OocColorPicker.Visible = true;
                OocColorPicker.Color = Color.FromHex(state.CurrentOocColor);
            }
            else
            {
                OocDropdown.SelectId(OptionNone);
                OocColorPicker.Visible = false;
            }
        }

        private void PopulateDropdown(OptionButton dropdown, bool canSetCustom, List<PersonalSponsorRankInfo> ranks, bool isGhost)
        {
            dropdown.Clear();
            dropdown.AddItem("Стандартний (Вимкнено)", OptionNone);

            if (canSetCustom)
            {
                dropdown.AddItem("Власний колір (Кастомний)", OptionCustom);
            }

            foreach (var rank in ranks)
            {
                var fixedColor = isGhost ? rank.FixedGhostColor : rank.FixedOocColor;
                if (!string.IsNullOrEmpty(fixedColor))
                {
                    dropdown.AddItem($"Колір рангу: {rank.Name}", rank.Id);
                }
            }
        }

        private void OnSavePressed(BaseButton.ButtonEventArgs args)
        {
            int? ghostRankId = null;
            string? customGhostColor = null;

            if (GhostDropdown.SelectedId == OptionCustom)
                customGhostColor = GhostColorPicker.Color.ToHex();
            else if (GhostDropdown.SelectedId > 0)
                ghostRankId = GhostDropdown.SelectedId;

            int? oocRankId = null;
            string? customOocColor = null;

            if (OocDropdown.SelectedId == OptionCustom)
                customOocColor = OocColorPicker.Color.ToHex();
            else if (OocDropdown.SelectedId > 0)
                oocRankId = OocDropdown.SelectedId;

            _eui.SaveSettings(customGhostColor, customOocColor, ghostRankId, oocRankId);
        }
    }
}
