using System.Collections.Generic;
using UnityEngine;

// Rolling record of one troop's melee over the last Window seconds. The troop is the attacker for attacks it makes and
// the defender for damage it takes. Samples older than the window are dropped; Reset starts a fresh record.
public sealed class MeleeMetrics
{
    public const float DefaultWindow = 30f;

    readonly Queue<Sample> _samples = new();
    float _window;
    float _startTime;

    public MeleeMetrics(float window = DefaultWindow)
    {
        _window = Mathf.Max(1f, window);
        _startTime = Time.time;
    }

    // An attack is a soldier spending a banked attack to start a clash.
    public void RecordAttack()
    {
        Add(new Sample { Time = Time.time, Attacks = 1 });
    }

    // A hit is a clash the troop's soldier won.
    public void RecordHit()
    {
        Add(new Sample { Time = Time.time, Hits = 1 });
    }

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

    // A clash the troop took part in, whether or not an attack was spent in it.
    public void RecordClash()
    {
        Add(new Sample { Time = Time.time, Clashes = 1 });
    }

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
            summary.Attacks += sample.Attacks;
            summary.Hits += sample.Hits;
            summary.DamageDone += sample.DamageDone;
            summary.DamageReceived += sample.DamageReceived;
            summary.Clashes += sample.Clashes;
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
        public int Attacks;
        public int Hits;
        public float DamageDone;
        public float DamageReceived;
        public int Clashes;
    }

    public struct Summary
    {
        public float Window;
        public float Elapsed;
        public int LivingSoldiers;
        public int Attacks;
        public int Hits;
        public float DamageDone;
        public float DamageReceived;
        public int Clashes;

        public float AttacksPerSecond => Elapsed > 0f ? Attacks / Elapsed : 0f;
        public float AttacksPerSecondPerSoldier => LivingSoldiers > 0 ? AttacksPerSecond / LivingSoldiers : 0f;
        public float HitRate => Attacks > 0 ? (float)Hits / Attacks : 0f;
        public float DamageDonePerSecond => Elapsed > 0f ? DamageDone / Elapsed : 0f;
        public float DamageReceivedPerSecond => Elapsed > 0f ? DamageReceived / Elapsed : 0f;
    }
}
