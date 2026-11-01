using System.Collections.Generic;
using UnityEngine;

// Rolling record of one troop's melee over the last Window seconds: charges started by its soldiers, clashes they took
// part in, duels they won, and damage dealt and taken. Samples older than the window are dropped; Reset starts a fresh
// record. The charge rate per soldier is the check for spec §4.3 (about Aggression charges per ActionWindowSeconds).
public sealed class MeleeMetrics
{
    public const float DefaultWindow = 30f;

    readonly Queue<Sample> _samples = new();
    readonly float _window;
    float _startTime;

    public MeleeMetrics(float window = DefaultWindow)
    {
        _window = Mathf.Max(1f, window);
        _startTime = Time.time;
    }

    public float Window => _window;

    public void RecordChargeStarted() => Add(new Sample { Time = Time.time, Charges = 1 });

    public void RecordFollowerJoined() => Add(new Sample { Time = Time.time, Followers = 1 });

    // A clash the troop took part in (either side).
    public void RecordClash() => Add(new Sample { Time = Time.time, Clashes = 1 });

    // A duel this troop's soldier won.
    public void RecordDuelWin() => Add(new Sample { Time = Time.time, DuelWins = 1 });

    public void RecordDamageDone(float damage)
    {
        if (damage <= 0f) return;
        Add(new Sample { Time = Time.time, DamageDone = damage });
    }

    public void RecordDamageReceived(float damage)
    {
        if (damage <= 0f) return;
        Add(new Sample { Time = Time.time, DamageReceived = damage });
    }

    public void RecordShot(bool hit) => Add(new Sample { Time = Time.time, Shots = 1, ShotHits = hit ? 1 : 0 });

    public void Reset()
    {
        _samples.Clear();
        _startTime = Time.time;
    }

    public Summary Snapshot(int livingSoldiers)
    {
        Prune();
        var summary = new Summary { Window = _window, LivingSoldiers = livingSoldiers };
        foreach (var sample in _samples)
        {
            summary.Charges += sample.Charges;
            summary.Followers += sample.Followers;
            summary.Clashes += sample.Clashes;
            summary.DuelWins += sample.DuelWins;
            summary.DamageDone += sample.DamageDone;
            summary.DamageReceived += sample.DamageReceived;
            summary.Shots += sample.Shots;
            summary.ShotHits += sample.ShotHits;
        }
        // Until a full window has passed, rates use the time actually recorded so they are not understated.
        summary.Elapsed = Mathf.Min(_window, Time.time - _startTime);
        return summary;
    }

    void Add(Sample sample)
    {
        _samples.Enqueue(sample);
        Prune();
    }

    void Prune()
    {
        float cutoff = Time.time - _window;
        while (_samples.Count > 0 && _samples.Peek().Time < cutoff)
            _samples.Dequeue();
    }

    struct Sample
    {
        public float Time;
        public int Charges;
        public int Followers;
        public int Clashes;
        public int DuelWins;
        public float DamageDone;
        public float DamageReceived;
        public int Shots;
        public int ShotHits;
    }

    public struct Summary
    {
        public float Window;
        public float Elapsed;
        public int LivingSoldiers;
        public int Charges;
        public int Followers;
        public int Clashes;
        public int DuelWins;
        public float DamageDone;
        public float DamageReceived;
        public int Shots;
        public int ShotHits;

        public float ChargesPerSecond => Elapsed > 0f ? Charges / Elapsed : 0f;
        // Charges per soldier, scaled to a full window: compare with Aggression (spec §4.3).
        public float ChargesPerSoldierPerWindow =>
            LivingSoldiers > 0 && Elapsed > 0f ? Charges / Elapsed * Window / LivingSoldiers : 0f;
        public float DuelWinRate => Clashes > 0 ? (float)DuelWins / Clashes : 0f;
        public float HitRate => Shots > 0 ? (float)ShotHits / Shots : 0f;
        public float DamageDonePerSecond => Elapsed > 0f ? DamageDone / Elapsed : 0f;
        public float DamageReceivedPerSecond => Elapsed > 0f ? DamageReceived / Elapsed : 0f;
    }
}
