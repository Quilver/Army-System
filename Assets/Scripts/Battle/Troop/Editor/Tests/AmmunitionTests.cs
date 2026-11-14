using NUnit.Framework;
using UnityEngine;

// Ammunition (SU-151): each soldier spends its weapon's Ammunition and runs out only while it is limited.
public sealed class AmmunitionTests
{
    TroopTuning _tuning;

    [SetUp]
    public void SetUp()
    {
        _tuning = TroopTuning.CreateDefaults();
        TroopTuning.OverrideForTests(_tuning);
    }

    [TearDown]
    public void TearDown()
    {
        TroopTuning.OverrideForTests(null);
        _tuning?.DestroySections();
        _tuning = null;
    }

    [Test]
    public void Soldier_SpendsAmmunition_AndRunsOutOnlyWhenItIsLimited()
    {
        var bow = RangedWeaponProfile.CreateRuntimeProfile("Bow", RangedWeaponClass.Bow, 1, 15f, 2, 5, ammunition: 2);
        var sword = MeleeWeaponProfile.CreateRuntimeProfile("Sword", MeleeWeaponClass.Sword, 0, 0, null);
        var archer = CharacterProfile.CreateRuntimeProfile("Archer", null, null, null, 1, 5, CombatStatLimits.Default,
            new CharacterEquipment(sword, bow, null), new SoldierStats(5, 5, 5, 5, 5, 5, 15, null));
        try
        {
            var defaultBow = RangedWeaponProfile.CreateRuntimeProfile("Default", RangedWeaponClass.Bow, 1, 15f, 2, 5);
            Assert.AreEqual(24, defaultBow.Ammunition, "24 shots by default");
            Object.DestroyImmediate(defaultBow);

            var soldier = new SoldierAgentState(1, archer, Vector2.zero);
            Assert.AreEqual(2, soldier.AmmunitionLeft);
            soldier.SpendShot();
            soldier.SpendShot();
            Assert.AreEqual(0, soldier.AmmunitionLeft);
            Assert.IsFalse(soldier.HasAmmunition, "Limited ammunition is on by default");

            var serialized = new UnityEditor.SerializedObject(_tuning.Ranged);
            serialized.FindProperty("_limitedAmmunition").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(soldier.HasAmmunition, "Unlimited: always has ammunition");
        }
        finally
        {
            Object.DestroyImmediate(archer);
            Object.DestroyImmediate(bow);
            Object.DestroyImmediate(sword);
        }
    }
}
