// SPDX-FileCopyrightText: 2026 Mini Station
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server.Shuttles.Events;

/// <summary>
/// Raised before any shuttle begins an FTL jump (console, dock, script, etc.).
/// </summary>
[ByRefEvent]
public record struct ShuttleFTLAttemptEvent(EntityUid ShuttleUid, bool Cancelled, string Reason);
