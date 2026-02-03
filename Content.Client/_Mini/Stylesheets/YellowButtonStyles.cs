// SPDX-FileCopyrightText: 2024 Your Name <you@example.com>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Maths;

namespace Content.Client.Stylesheets
{
    public static class YellowButtonStyles
    {
        // Константы цветов
        public static readonly Color ButtonColorDefaultYellow = Color.FromHex("#d8a903");
        public static readonly Color ButtonColorHoveredYellow = Color.FromHex("#f0c515");
        public static readonly Color ButtonColorPressedYellow = Color.FromHex("#b88a00");
        public static readonly Color ButtonColorDisabledYellow = Color.FromHex("#5a4a00");
        public static readonly Color ButtonColorTextYellow = Color.FromHex("#000000"); // Черный текст для контраста

        // Классы стилей
        public const string StyleClassButtonColorYellow = "ButtonColorYellow";
        public const string StyleClassButtonColorYellowBright = "ButtonColorYellowBright";
        public const string StyleClassButtonColorYellowDark = "ButtonColorYellowDark";
        public const string StyleClassButtonColorYellowCaution = "ButtonColorYellowCaution";

        // Яркие варианты
        public static readonly Color ButtonColorDefaultYellowBright = Color.FromHex("#FFEB3B");
        public static readonly Color ButtonColorHoveredYellowBright = Color.FromHex("#FFF176");

        // Темные варианты
        public static readonly Color ButtonColorDefaultYellowDark = Color.FromHex("#F57F17");
        public static readonly Color ButtonColorHoveredYellowDark = Color.FromHex("#F9A825");

        // Предупреждающие варианты
        public static readonly Color ButtonColorDefaultYellowCaution = Color.FromHex("#FF9800");
        public static readonly Color ButtonColorHoveredYellowCaution = Color.FromHex("#FFB74D");
    }
}
