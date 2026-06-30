// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Preferences;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.Client.Lobby.UI;

/// <summary>
/// Shared visual constants for lobby character setup UI, aligned with the liquid-glass theme.
/// </summary>
public static class LobbyUiStyles
{
    public const float SelectedButtonAlpha = 0.88f;
    public const float DepartmentAccentMix = 0.38f;
    public const float DepartmentHeaderAlpha = 0.55f;

    public static readonly Color GlassPanel = Color.FromHex("#2A2A38D9");
    public static readonly Color GlassHeading = Color.FromHex("#323240CC");
    public static readonly Color GlassSetupBackground = Color.FromHex("#14141CCC");
    public static readonly Color GlassAccentLine = Color.FromHex("#A88B5E80");
    public static readonly Color SubduedText = Color.FromHex("#AEABC4");
    public static readonly Color HeaderText = Color.FromHex("#E8E6F0");

    public static StyleBoxFlat GlassPanelBox() => new()
    {
        BackgroundColor = GlassPanel,
        BorderThickness = new Thickness(0),
        ContentMarginTopOverride = 10,
        ContentMarginBottomOverride = 10,
        ContentMarginLeftOverride = 10,
        ContentMarginRightOverride = 10,
    };

    public static StyleBoxFlat SectionHeader() => new()
    {
        BackgroundColor = GlassHeading,
        BorderThickness = new Thickness(0),
        ContentMarginLeftOverride = 8,
        ContentMarginTopOverride = 4,
        ContentMarginRightOverride = 8,
        ContentMarginBottomOverride = 4,
    };

    public static StyleBoxFlat SelectedPriorityButton(Color color) => new()
    {
        BackgroundColor = color.WithAlpha(SelectedButtonAlpha),
        BorderThickness = new Thickness(0),
        Padding = new Thickness(2),
        ContentMarginLeftOverride = 10,
        ContentMarginTopOverride = 2,
        ContentMarginRightOverride = 10,
        ContentMarginBottomOverride = 2,
    };

    public static Color MuteDepartmentColor(Color color) =>
        Color.InterpolateBetween(GlassPanel, color, DepartmentAccentMix);

    public static Color PriorityColor(JobPriority priority) => priority switch
    {
        JobPriority.High => Color.FromHex("#4A8F62"),
        JobPriority.Medium => Color.FromHex("#9A8A4A"),
        JobPriority.Low => Color.FromHex("#9A6848"),
        _ => Color.FromHex("#555566"),
    };

    public static StyleBoxFlat DepartmentHeader(Color accent) => new()
    {
        BackgroundColor = GlassHeading.WithAlpha(DepartmentHeaderAlpha),
        BorderThickness = new Thickness(3, 0, 0, 0),
        BorderColor = accent.WithAlpha(0.85f),
        ContentMarginLeftOverride = 10,
        ContentMarginTopOverride = 8,
        ContentMarginRightOverride = 8,
        ContentMarginBottomOverride = 8,
    };

    public static StyleBoxFlat DepartmentBody(Color accent) => new()
    {
        BackgroundColor = GlassPanel.WithAlpha(0.35f),
        BorderThickness = new Thickness(2, 0, 0, 0),
        BorderColor = accent.WithAlpha(0.55f),
        ContentMarginLeftOverride = 0,
        ContentMarginTopOverride = 0,
        ContentMarginRightOverride = 0,
        ContentMarginBottomOverride = 0,
    };

    public static Color PriorityPointerModulate(JobPriority priority) => priority switch
    {
        JobPriority.High => Color.FromHex("#5CBF6A"),
        JobPriority.Medium => Color.FromHex("#E8963A"),
        JobPriority.Low => Color.FromHex("#E04848"),
        _ => Color.FromHex("#9A9AA8"),
    };
}
