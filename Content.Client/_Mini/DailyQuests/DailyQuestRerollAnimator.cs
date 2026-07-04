// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT
using System;
using System.Collections.Generic;
using Content.Shared._Mini.DailyQuests;
using Robust.Shared.Maths;

namespace Content.Client._Mini.DailyQuests;

/// <summary>
/// Cycles quest previews with accelerating intervals and a pulsing highlight.
/// </summary>
public sealed class DailyQuestRerollAnimator
{
    private readonly DailyQuestCardControl _card;

    private IReadOnlyList<DailyQuestEntry> _sequence = Array.Empty<DailyQuestEntry>();
    private DailyQuestEntry _finalEntry = null!;
    private Action<DailyQuestEntry>? _onComplete;
    private int _index;
    private int _step;
    private float _timer;
    private float _interval = 0.055f;
    private float _pulsePhase;

    public bool IsPlaying { get; private set; }

    public DailyQuestRerollAnimator(DailyQuestCardControl card)
    {
        _card = card;
    }

    public void Start(IReadOnlyList<DailyQuestEntry> sequence, DailyQuestEntry final, Action<DailyQuestEntry> onComplete)
    {
        if (sequence.Count == 0)
        {
            onComplete(final);
            return;
        }

        _sequence = sequence;
        _finalEntry = final;
        _onComplete = onComplete;
        _index = 0;
        _step = 0;
        _timer = 0f;
        _interval = 0.055f;
        _pulsePhase = 0f;
        IsPlaying = true;

        _card.SetInteractable(false);
        _card.SetQuest(_sequence[0]);
        _card.SetRerollPulse(1f);
    }

    public void Update(float frameTime)
    {
        if (!IsPlaying)
            return;

        _pulsePhase += frameTime * 14f;
        var pulse = 0.55f + 0.45f * (0.5f + 0.5f * MathF.Sin(_pulsePhase));
        _card.SetRerollPulse(pulse);

        _timer += frameTime;
        if (_timer < _interval)
            return;

        _timer = 0f;
        _step++;
        _index++;

        _interval = Math.Min(0.42f, 0.055f + _step * _step * 0.0018f);

        if (_index >= _sequence.Count)
        {
            Finish(_finalEntry);
            return;
        }

        _card.SetQuest(_sequence[_index]);
    }

    public void Cancel(DailyQuestEntry fallback)
    {
        if (!IsPlaying)
            return;

        Finish(fallback);
    }

    private void Finish(DailyQuestEntry final)
    {
        IsPlaying = false;
        _card.SetRerollPulse(1f);
        _card.SetInteractable(true);
        _card.SetQuest(final, forceRebuild: true);
        _onComplete?.Invoke(final);
        _onComplete = null;
    }
}
