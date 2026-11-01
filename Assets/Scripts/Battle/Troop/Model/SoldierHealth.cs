using System;
using UnityEngine;

// Integer HP pool for one soldier (spec §4.6). Current HP is MaxHp minus Bleed minus Bruise.
//   Bleed:  permanent for the battle. Never heals.
//   Bruise: heals BruiseHealPerSecond (1) points per second (TickBruiseHealing), so a bruised soldier recovers.
// Fatigued at CurrentHp <= MaxHp x FatigueFraction (0.5). Dead at CurrentHp 0. Pure C#, no Unity lifecycle.
[Serializable]
public sealed class SoldierHealth
{
    [SerializeField] int _maxHp;
    [SerializeField] int _bleed;
    [SerializeField] int _bruise;
    [SerializeField] float _healProgress;

    public SoldierHealth() : this(CombatStatLimits.HpDefault) { }

    public SoldierHealth(int maxHp)
    {
        _maxHp = Math.Max(1, maxHp);
    }

    public int MaxHp => _maxHp;
    public int Bleed => _bleed;
    public int Bruise => _bruise;
    public int CurrentHp => Math.Max(0, _maxHp - _bleed - _bruise);
    public bool IsDead => CurrentHp <= 0;

    // CurrentHp <= MaxHp x fraction (spec §4.6). With the default 0.5 and MaxHp 15 the threshold is 7.5: 7 is fatigued,
    // 8 is not.
    public bool IsFatigued(float fatigueFraction = 0.5f) => !IsDead && CurrentHp <= _maxHp * fatigueFraction + 1e-4f;

    // Each returns the points actually taken, clamped to the HP that remains. Anything beyond that is lost.
    public int ApplyBleed(int points)
    {
        int applied = Clamp(points);
        _bleed += applied;
        return applied;
    }

    public int ApplyBruise(int points)
    {
        int applied = Clamp(points);
        _bruise += applied;
        return applied;
    }

    // Heals bruise at healPerSecond points per second, one whole point at a time. Returns the points healed this call.
    // Dead soldiers and soldiers with no bruise do not accumulate time.
    public int TickBruiseHealing(float deltaTime, float healPerSecond = 1f)
    {
        if (IsDead || _bruise <= 0 || healPerSecond <= 0f)
        {
            _healProgress = 0f;
            return 0;
        }

        _healProgress += deltaTime * healPerSecond;
        int healed = 0;
        while (_healProgress >= 1f && _bruise > 0)
        {
            _healProgress -= 1f;
            _bruise--;
            healed++;
        }
        if (_bruise == 0) _healProgress = 0f;
        return healed;
    }

    int Clamp(int points) => points <= 0 ? 0 : Math.Min(points, CurrentHp);
}
