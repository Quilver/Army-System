using NUnit.Framework;
using UnityEngine;

// Spec §6.2 order classification and facing, and §4.7 ranged rules.
public sealed class OrderClassifierTests
{
    [Test] public void SevenPixelsOnEnemy_IsPursue() =>
        Assert.AreEqual(OrderKind.Pursue, OrderClassifier.Classify(Vector2.zero, new Vector2(7f, 0f), 8f, true, 1));

    [Test] public void SevenPixelsOnGround_IsMove() =>
        Assert.AreEqual(OrderKind.Move, OrderClassifier.Classify(Vector2.zero, new Vector2(7f, 0f), 8f, false, 1));

    [Test] public void NinePixels_SeveralTroops_IsLine() =>
        Assert.AreEqual(OrderKind.Line, OrderClassifier.Classify(Vector2.zero, new Vector2(9f, 0f), 8f, true, 2));

    // SU-16/17: a single troop is never given a facing, so its drag is a Move (even when released on an enemy).
    [Test] public void NinePixels_SingleTroop_IsMove()
    {
        Assert.AreEqual(OrderKind.Move, OrderClassifier.Classify(Vector2.zero, new Vector2(9f, 0f), 8f, false, 1));
        Assert.AreEqual(OrderKind.Move, OrderClassifier.Classify(Vector2.zero, new Vector2(9f, 0f), 8f, true, 1));
    }

    [Test] public void BoxSelect_StartsAtEightPixels()
    {
        Assert.IsFalse(OrderClassifier.IsBoxSelect(Vector2.zero, new Vector2(7.9f, 0f), 8f));
        Assert.IsTrue(OrderClassifier.IsBoxSelect(Vector2.zero, new Vector2(8f, 0f), 8f));
    }

    [Test]
    public void LineFacing_NoEnemy_IsRightHandNormal()
    {
        Vector2 facing = OrderClassifier.LineFacing(Vector2.zero, new Vector2(0f, 10f), null);
        Assert.AreEqual(Vector2.right, facing);
    }

    [Test]
    public void LineFacing_TurnsToTheEnemySide()
    {
        Vector2 facing = OrderClassifier.LineFacing(Vector2.zero, new Vector2(0f, 10f), new Vector2(-20f, 5f));
        Assert.AreEqual(Vector2.left, facing);
    }
}

public sealed class RangedRulesTests
{
    [Test]
    public void LongBow_Skill6_Is8Over18()
    {
        Assert.AreEqual(8f / 18f, RangedRules.HitChance(2, 6, 10), 1e-5f);
    }

    [TestCase(6, 8f / 18f)]
    [TestCase(8, 0.5f)]
    public void HitRate_OverManyShots_MatchesTheChance(int skill, float expected)
    {
        var random = new SeededTroopRandom(7);
        float chance = RangedRules.HitChance(2, skill, 10);
        int hits = 0;
        for (int i = 0; i < 10000; i++)
            if (random.Value() < chance) hits++;
        Assert.AreEqual(expected, hits / 10000f, 0.01f);
    }

    [Test]
    public void LongBowInterval_IsThreeSeconds_WithTenPercentJitter()
    {
        var random = new SeededTroopRandom(3);
        float total = 0f;
        for (int i = 0; i < 2000; i++)
        {
            float interval = RangedRules.FireInterval(10, 30f, 0.1f, random);
            Assert.That(interval, Is.InRange(2.7f, 3.3f));
            total += interval;
        }
        Assert.AreEqual(3f, total / 2000f, 0.03f);
    }

    [Test]
    public void RangedDamage_IsTheWeaponDamage()
    {
        Assert.AreEqual(10, RangedRules.RawDamage(10));
    }

    // D-54: within the shooter's range and within the arc of the troop's facing, measured from the shooter.
    [TestCase(0f, 10f, true)]      // straight ahead, in range
    [TestCase(0f, 21f, false)]     // straight ahead, out of range
    [TestCase(7f, 7f, true)]       // exactly on the 45 degree edge
    [TestCase(8f, 6f, false)]      // outside the arc
    [TestCase(0f, -5f, false)]     // behind
    public void CanReach_IsRangeAndFrontArcFromTheShooter(float x, float y, bool expected)
    {
        Assert.AreEqual(expected, RangedRules.CanReach(new Vector2(x, y), Vector2.up, 45f, 20f));
    }

    [Test]
    public void CanReach_AllRoundArc_IgnoresFacing()
    {
        Assert.IsTrue(RangedRules.CanReach(new Vector2(0f, -5f), Vector2.up, 180f, 20f));
        Assert.IsFalse(RangedRules.CanReach(new Vector2(0f, 5f), Vector2.up, 45f, 0f), "No range, no reach");
    }
}

// SU-136: line of sight for direct (not arced) shots.
public sealed class LineOfSightRuleTests
{
    [TestCase(5f, 0.2f, true)]     // on the line
    [TestCase(5f, 0.35f, false)]   // beside it, more than a soldier radius away
    [TestCase(-0.5f, 0f, false)]   // behind the shooter
    [TestCase(10.5f, 0f, false)]   // beyond the target
    public void SoldierWithinARadiusOfTheLine_Blocks(float x, float y, bool blocked)
    {
        var soldiers = new System.Collections.Generic.List<Vector2> { new(x, y) };
        Assert.AreEqual(blocked, RangedRules.LineBlockedBySoldiers(Vector2.zero, new Vector2(10f, 0f), soldiers,
            TroopConstants.SoldierRadius));
    }

    [Test]
    public void SightBlockingLayers_DefaultToTerrain_NotWater()
    {
        var ranged = ScriptableObject.CreateInstance<RangedTuning>();
        try
        {
            int mask = ranged.SightBlockingLayers.value;
            Assert.AreNotEqual(0, mask & (1 << LayerMask.NameToLayer("Terrain")), "Terrain blocks sight");
            Assert.AreEqual(0, mask & (1 << LayerMask.NameToLayer("Water")), "Water does not");
            Assert.AreEqual(0, mask & (1 << LayerMask.NameToLayer("Water/flat impassible terrain")), "Water does not");
        }
        finally
        {
            Object.DestroyImmediate(ranged);
        }
    }

    [Test]
    public void ArcedShot_IsOffByDefault()
    {
        var bow = RangedWeaponProfile.CreateRuntimeProfile("Test", RangedWeaponClass.Bow, 1, 10f, 1, 1);
        var arced = RangedWeaponProfile.CreateRuntimeProfile("Test", RangedWeaponClass.Bow, 1, 10f, 1, 1, arcedShot: true);
        try
        {
            Assert.IsFalse(bow.ArcedShot);
            Assert.IsTrue(arced.ArcedShot);
        }
        finally
        {
            Object.DestroyImmediate(bow);
            Object.DestroyImmediate(arced);
        }
    }
}

// SU-137: weapon Damage, ChargeBonus, Accuracy and FireRate are integers from 0 to 20; Range is not capped.
public sealed class WeaponValueLimitTests
{
    [Test]
    public void RangedWeaponValues_ClampToZeroToTwenty()
    {
        var bow = RangedWeaponProfile.CreateRuntimeProfile("Test", RangedWeaponClass.Bow, 25, 40f, -3, 21);
        try
        {
            Assert.AreEqual(20, bow.Damage);
            Assert.AreEqual(0, bow.Accuracy);
            Assert.AreEqual(20, bow.FireRate);
            Assert.AreEqual(40f, bow.Range, "Range is not capped");
        }
        finally
        {
            Object.DestroyImmediate(bow);
        }
    }

    [Test]
    public void MeleeWeaponValues_ClampToZeroToTwenty()
    {
        var sword = MeleeWeaponProfile.CreateRuntimeProfile("Test", MeleeWeaponClass.Sword, -1, 30, null);
        try
        {
            Assert.AreEqual(0, sword.Damage);
            Assert.AreEqual(20, sword.ChargeBonus);
        }
        finally
        {
            Object.DestroyImmediate(sword);
        }
    }

    // A value above 20 written straight into the serialized data (an old asset) still reads as 20.
    [Test]
    public void SerializedValuesAboveTwenty_ReadAsTwenty()
    {
        var bow = RangedWeaponProfile.CreateRuntimeProfile("Test", RangedWeaponClass.Bow, 1, 10f, 1, 1);
        try
        {
            var serialized = new UnityEditor.SerializedObject(bow);
            serialized.FindProperty("_damage").intValue = 99;
            serialized.FindProperty("_accuracy").intValue = 50;
            serialized.FindProperty("_fireRate").intValue = 21;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.AreEqual(20, bow.Damage);
            Assert.AreEqual(20, bow.Accuracy);
            Assert.AreEqual(20, bow.FireRate);
        }
        finally
        {
            Object.DestroyImmediate(bow);
        }
    }
}
