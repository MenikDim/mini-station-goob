// SPDX-FileCopyrightText: 2020 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2021 Acruid <shatter66@gmail.com>
// SPDX-FileCopyrightText: 2021 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.Stylesheets
{
    public sealed class StylesheetManager : IStylesheetManager
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IUiFontStackManager _uiFontStackManager = default!;
        [Dependency] private readonly IFontManager _fontManager = default!;

        public Stylesheet SheetNano { get; private set; } = default!;
        public Stylesheet SheetSpace { get; private set; } = default!;

        public void Initialize()
        {
            _uiFontStackManager.Initialize();

            _configurationManager.OnValueChanged(CCVars.InterfaceAccentRed, _ => UpdateStyles(), true);
            _configurationManager.OnValueChanged(CCVars.InterfaceAccentGreen, _ => UpdateStyles(), true);
            _configurationManager.OnValueChanged(CCVars.InterfaceAccentBlue, _ => UpdateStyles(), true);
            _configurationManager.OnValueChanged(CCVars.UiFontStyle, _ => UpdateStyles(), true);
        }

        private void UpdateStyles()
        {
            var accent = new Color(
                (byte) _configurationManager.GetCVar(CCVars.InterfaceAccentRed),
                (byte) _configurationManager.GetCVar(CCVars.InterfaceAccentGreen),
                (byte) _configurationManager.GetCVar(CCVars.InterfaceAccentBlue));
            SheetNano = new StyleNano(_resourceCache, accent).Stylesheet;
            SheetSpace = new StyleSpace(_resourceCache, accent).Stylesheet;
            _fontManager.ClearFontCache();
            _userInterfaceManager.Stylesheet = SheetNano;
        }
    }
}
