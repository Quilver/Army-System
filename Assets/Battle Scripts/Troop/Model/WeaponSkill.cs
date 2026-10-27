using System;
using UnityEngine;

// A soldier's skill with one weapon class (spec §1.6). Integer 1-20, default 5 when added. A soldier has skill in at
// most two classes. A class that is not listed counts as unskilled (level 0; Questions.md Q-2).
[Serializable]
public struct WeaponSkill
{
    public WeaponClass Class;
    [Range(1, 20)] public int Level;

    public WeaponSkill(WeaponClass weaponClass, int level)
    {
        Class = weaponClass;
        Level = CombatStatLimits.Clamp(level);
    }
}
