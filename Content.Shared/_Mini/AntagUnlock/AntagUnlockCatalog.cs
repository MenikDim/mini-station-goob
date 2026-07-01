// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT

namespace Content.Shared._Mini.AntagUnlock;

public static class AntagUnlockCatalog
{
    public const string EntryPrefix = "antag-unlock:";

    public static string GetEntryId(string antagId) => $"{EntryPrefix}{antagId}";
}
