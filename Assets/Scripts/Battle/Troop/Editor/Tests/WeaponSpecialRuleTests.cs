using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// SU-90: weapon special rules are a list; each rule applies only to the kind of weapon it is for.
// SU-140 (Q-2): a weapon outside the character's weapon skills is warned about; its soldiers fight with skill 0.
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

    // --- SU-140: unskilled weapons ---

    readonly List<Object> _created = new();

    [TearDown]
    public void DestroyCreated()
    {
        foreach (var obj in _created)
            if (obj != null) Object.DestroyImmediate(obj);
        _created.Clear();
    }

    MeleeWeaponProfile Melee(MeleeWeaponClass weaponClass)
    {
        var weapon = MeleeWeaponProfile.CreateRuntimeProfile("Test " + weaponClass, weaponClass, 2, 1, null);
        _created.Add(weapon);
        return weapon;
    }

    RangedWeaponProfile Ranged(RangedWeaponClass weaponClass)
    {
        var weapon = RangedWeaponProfile.CreateRuntimeProfile("Test " + weaponClass, weaponClass, 2, 20f, 2, 10, null);
        _created.Add(weapon);
        return weapon;
    }

    CharacterProfile Character(string characterName, MeleeWeaponProfile melee, RangedWeaponProfile ranged,
        params WeaponSkill[] skills)
    {
        var character = CharacterProfile.CreateRuntimeProfile(characterName, null, null, null, 10, 5, 5,
            new CharacterEquipment(melee, ranged, null), new SoldierStats(5, 5, 5, 5, 5, 5, 15, skills));
        _created.Add(character);
        return character;
    }

    [Test]
    public void SkilledCharacter_HasNoUnskilledWeapons()
    {
        CharacterProfile character = Character("Archer", Melee(MeleeWeaponClass.Sword), Ranged(RangedWeaponClass.Bow),
            new WeaponSkill(WeaponClass.Bow, 5), new WeaponSkill(WeaponClass.Sword, 3));

        Assert.IsEmpty(character.UnskilledWeapons());
        Assert.IsEmpty(TroopRoster.UnskilledWeaponWarnings(new[] { character }));
    }

    [Test]
    public void MissingMeleeClass_ReportsTheMeleeWeapon()
    {
        MeleeWeaponProfile sword = Melee(MeleeWeaponClass.Sword);
        CharacterProfile character = Character("Skirmisher", sword, Ranged(RangedWeaponClass.Javelin),
            new WeaponSkill(WeaponClass.Axe, 6), new WeaponSkill(WeaponClass.Javelin, 4));

        CollectionAssert.AreEqual(new WeaponProfile[] { sword }, character.UnskilledWeapons());
        string warning = character.UnskilledWeaponWarning(sword);
        StringAssert.Contains("Skirmisher", warning);
        StringAssert.Contains(sword.DisplayName, warning);
        StringAssert.Contains("Sword", warning);
    }

    [Test]
    public void MissingRangedClass_ReportsTheRangedWeapon()
    {
        RangedWeaponProfile bow = Ranged(RangedWeaponClass.Bow);
        CharacterProfile character = Character("Levy", Melee(MeleeWeaponClass.Spear), bow,
            new WeaponSkill(WeaponClass.Spear, 5));

        CollectionAssert.AreEqual(new WeaponProfile[] { bow }, character.UnskilledWeapons());
        // Unskilled: the hit chance has no WeaponSkill term (spec §4.7).
        Assert.AreEqual(0, character.SkillFor(bow.Class));
        Assert.AreEqual(2f / 12f, RangedRules.HitChance(2, character.SkillFor(bow.Class), 10), 1e-6f);
    }

    [Test]
    public void UnskilledSoldier_MeleeSkillIsZero_SoCombatPowerHasNoSkillTerm()
    {
        CharacterProfile character = Character("Bowman", Melee(MeleeWeaponClass.Axe), null,
            new WeaponSkill(WeaponClass.Bow, 7));

        Assert.AreEqual(0, character.MeleeWeaponSkill);
        // Charging with support 2: only Charge + weapon charge bonus + support count.
        int power = CombatPower.Compute(character.MeleeWeaponSkill, true, character.Charge,
            character.MeleeWeaponChargeBonus, false, 0, 2);
        Assert.AreEqual(character.Charge + character.MeleeWeaponChargeBonus + 2, power);
    }

    [Test]
    public void SpawnWarnings_OnePerCharacterAndWeapon_NotPerSoldier()
    {
        CharacterProfile unskilled = Character("Skirmisher", Melee(MeleeWeaponClass.Sword),
            Ranged(RangedWeaponClass.Javelin));
        CharacterProfile skilled = Character("Spearman", Melee(MeleeWeaponClass.Spear), null,
            new WeaponSkill(WeaponClass.Spear, 5));

        // The unskilled character is listed twice and brings 10 soldiers: still one warning per weapon.
        List<string> warnings = TroopRoster.UnskilledWeaponWarnings(new[] { unskilled, skilled, unskilled });

        Assert.AreEqual(2, warnings.Count);
        StringAssert.Contains("(Sword)", warnings[0]);
        StringAssert.Contains("(Javelin)", warnings[1]);
    }
}
