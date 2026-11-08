using NUnit.Framework;
using UnityEngine;

// SU-90: weapon special rules are a list; each rule applies only to the kind of weapon it is for.
public sealed class WeaponSpecialRuleTests
{
    [Test]
    public void MeleeWeapon_ReadsItsRules()
    {
        var weapon = MeleeWeaponProfile.CreateRuntimeProfile("Test Lance", MeleeWeaponClass.Spear, 3, 8,
            new[]
            {
                new WeaponSpecialRuleEntry(WeaponSpecialRule.CouchedLance),
                new WeaponSpecialRuleEntry(WeaponSpecialRule.Piercing, 2)
            });
        try
        {
            Assert.IsTrue(weapon.HasRule(WeaponSpecialRule.CouchedLance));
            Assert.IsFalse(weapon.HasRule(WeaponSpecialRule.SpearSupport));
            Assert.AreEqual(2, weapon.Piercing);
            Assert.AreEqual(0, weapon.Bludgeoning);
            Assert.AreEqual(2, weapon.SpecialRules.Count);
        }
        finally { Object.DestroyImmediate(weapon); }
    }

    [Test]
    public void RulesForTheOtherKindOfWeapon_HaveNoEffect()
    {
        var melee = MeleeWeaponProfile.CreateRuntimeProfile("Sword", MeleeWeaponClass.Sword, 1, 0,
            new[] { new WeaponSpecialRuleEntry(WeaponSpecialRule.FireAndEngage) });
        var ranged = RangedWeaponProfile.CreateRuntimeProfile("Bow", RangedWeaponClass.Bow, 1, 20f, 2, 10,
            new[]
            {
                new WeaponSpecialRuleEntry(WeaponSpecialRule.SpearSupport),
                new WeaponSpecialRuleEntry(WeaponSpecialRule.Bludgeoning, 3),
                new WeaponSpecialRuleEntry(WeaponSpecialRule.FireAndEngage)
            });
        try
        {
            Assert.IsFalse(melee.HasRule(WeaponSpecialRule.FireAndEngage));
            Assert.IsFalse(ranged.HasRule(WeaponSpecialRule.SpearSupport));
            Assert.IsTrue(ranged.HasRule(WeaponSpecialRule.FireAndEngage));
            Assert.AreEqual(3, ranged.Bludgeoning);
        }
        finally
        {
            Object.DestroyImmediate(melee);
            Object.DestroyImmediate(ranged);
        }
    }

    [Test]
    public void RepeatedValuedRules_AddUp()
    {
        var weapon = RangedWeaponProfile.CreateRuntimeProfile("Bow", RangedWeaponClass.Bow, 1, 20f, 2, 10,
            new[]
            {
                new WeaponSpecialRuleEntry(WeaponSpecialRule.Piercing, 1),
                new WeaponSpecialRuleEntry(WeaponSpecialRule.Piercing, 2)
            });
        try { Assert.AreEqual(3, weapon.Piercing); }
        finally { Object.DestroyImmediate(weapon); }
    }
}
