using System;

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
}
