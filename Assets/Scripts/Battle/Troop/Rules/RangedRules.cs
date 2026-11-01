using System;
using UnityEngine;

// Ranged fire (spec §4.7). Pure C#.
//   P(Hit) = (Accuracy + WeaponSkill) / (Accuracy + WeaponSkill + BaseShotDC)   (Q-44)
//   Interval = ActionWindowSeconds / FireRate, with +/- FireRateJitterFraction uniform variation (Q-43).
public static class RangedRules
{
    public static float HitChance(int accuracy, int weaponSkill, int baseShotDC)
    {
        float hit = Math.Max(0, accuracy) + Math.Max(0, weaponSkill);
        float total = hit + Math.Max(0, baseShotDC);
        return total <= 0f ? 0f : hit / total;
    }

    public static float BaseInterval(int fireRate, float actionWindowSeconds) =>
        fireRate <= 0 ? float.PositiveInfinity : actionWindowSeconds / fireRate;

    public static float FireInterval(int fireRate, float actionWindowSeconds, float jitterFraction, ITroopRandom random)
    {
        float interval = BaseInterval(fireRate, actionWindowSeconds);
        if (float.IsInfinity(interval)) return interval;
        float jitter = Math.Max(0f, jitterFraction);
        return interval * (1f + random.Range(-jitter, jitter));
    }

    // Ranged damage is the weapon's damage; Strength is not added (Q-5) and there is no flank bonus.
    public static int RawDamage(int weaponDamage) => Math.Max(0, weaponDamage);

    // Whether a shooter can hit a point (Decisions D-54): the point is within the weapon range of the shooter and within
    // halfArcDeg of the troop's facing, measured from the shooter. offset = target - shooter. An arc of 180 or more is
    // all round. The range mask shader (Resources/Shaders/TroopRangeMask.shader) draws the same region.
    public static bool CanReach(Vector2 offset, Vector2 facing, float halfArcDeg, float range)
    {
        float distanceSquared = offset.sqrMagnitude;
        if (range <= 0f || distanceSquared > range * range) return false;
        if (halfArcDeg >= 180f || distanceSquared < 1e-8f) return true;
        return Vector2.Angle(facing, offset) <= halfArcDeg;
    }
}
