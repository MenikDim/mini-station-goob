// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT

namespace Content.Shared._Mini.JobUnlock;

public static class JobUnlockCatalog
{
    public const string EntryPrefix = "job-unlock:";

    public static string GetEntryId(string jobId) => $"{EntryPrefix}{jobId}";
}
