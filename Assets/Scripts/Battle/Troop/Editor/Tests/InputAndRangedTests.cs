using NUnit.Framework;
using UnityEngine;

// Spec §6.2 order classification and facing, and §4.7 ranged rules.
public sealed class OrderClassifierTests
{
    const float DragMin = 8f;
    const float Slop = 24f;

    static OrderKind Classify(float dragPixels, bool enemyUnderRelease, bool sameEnemyUnderPress = false,
        bool faceHeld = false) =>
        OrderClassifier.Classify(Vector2.zero, new Vector2(dragPixels, 0f), DragMin, Slop, enemyUnderRelease,
            sameEnemyUnderPress, faceHeld);

    [Test] public void SevenPixelsOnEnemy_IsPursue() => Assert.AreEqual(OrderKind.Pursue, Classify(7f, true));

    [Test] public void SevenPixelsOnGround_IsMove() => Assert.AreEqual(OrderKind.Move, Classify(7f, false));

    // SU-156: a drag is a battle line for one troop or several (it used to be a Move for one troop, SU-129), so the player
    // can give a lone troop a front. SU-159: only off an enemy; a 9-pixel drag onto an enemy is now an attack.
    [Test] public void NinePixelsOnGround_IsLine() => Assert.AreEqual(OrderKind.Line, Classify(9f, false));

    // SU-156: Ctrl + click turns the selection in place, on the ground or on an enemy; a Ctrl drag is still a line.
    [Test] public void CtrlClick_IsFace()
    {
        Assert.AreEqual(OrderKind.Face, Classify(7f, false, faceHeld: true));
        Assert.AreEqual(OrderKind.Face, Classify(7f, true, faceHeld: true));
        Assert.AreEqual(OrderKind.Face, Classify(7f, true, sameEnemyUnderPress: true, faceHeld: true));
        Assert.AreEqual(OrderKind.Line, Classify(9f, false, faceHeld: true));
    }

    // SU-159: the mouse moving a little during a click on an enemy (past OrderDragMinPixels, short of
    // AttackClickSlopPixels) does not turn the attack into a battle line on top of the enemy.
    [Test] public void ShortDragOntoEnemy_BelowTheSlop_IsPursue()
    {
        Assert.AreEqual(OrderKind.Pursue, Classify(9f, true));
        Assert.AreEqual(OrderKind.Pursue, Classify(23.9f, true));
        Assert.AreEqual(OrderKind.Line, Classify(24f, true));
    }

    // SU-159: pressed and released on the same enemy troop: an attack, however far the mouse moved over it.
    [Test] public void DragFromAndToTheSameEnemy_IsPursue_AtAnyLength()
    {
        Assert.AreEqual(OrderKind.Pursue, Classify(30f, true, sameEnemyUnderPress: true));
        Assert.AreEqual(OrderKind.Pursue, Classify(400f, true, sameEnemyUnderPress: true));
    }

    // SU-159: a long drag that ends on an enemy but did not start on it is still a battle line.
    [Test] public void LongDragOntoAnEnemy_FromElsewhere_IsLine() =>
        Assert.AreEqual(OrderKind.Line, Classify(60f, true, sameEnemyUnderPress: false));

    // SU-159: Ctrl only matters below OrderDragMinPixels; past it the attack rules apply as without Ctrl.
    [Test] public void CtrlDragOntoEnemy_BelowTheSlop_IsPursue() =>
        Assert.AreEqual(OrderKind.Pursue, Classify(12f, true, faceHeld: true));

    // SU-160: one stretch for a multi-troop line: the drag over the summed L_nat, clamped to [0.7, 2.0].
    [Test] public void LineStretch_IsTheDragOverTheNaturalTotal_Clamped()
    {
        Assert.AreEqual(1.3f, TroopOrderPlanner.LineStretch(26f, 20f, 0.7f, 2f), 1e-5f);
        Assert.AreEqual(0.7f, TroopOrderPlanner.LineStretch(1f, 20f, 0.7f, 2f), 1e-5f);
        Assert.AreEqual(2f, TroopOrderPlanner.LineStretch(500f, 20f, 0.7f, 2f), 1e-5f);
    }

    // SU-160: the drag sets the front once it reaches a quarter of the minimum total width (0.25 x 0.7 x 20 = 3.5).
    [Test] public void FacingDeadZone_IsAQuarterOfTheMinimumWidth()
    {
        Assert.IsTrue(TroopOrderPlanner.InFacingDeadZone(3.49f, 20f, 0.7f, 0.25f));
        Assert.IsFalse(TroopOrderPlanner.InFacingDeadZone(3.5f, 20f, 0.7f, 0.25f));
    }

    // SU-160: inside the dead zone the line runs across the kept facing, on the side of the cursor.
    [Test] public void DeadZoneDirection_IsAcrossTheFacing_OnTheCursorsSide()
    {
        AssertVector(Vector2.down, TroopOrderPlanner.DeadZoneDirection(Vector2.right, new Vector2(0.3f, -1f)));
        AssertVector(Vector2.up, TroopOrderPlanner.DeadZoneDirection(Vector2.right, new Vector2(0.3f, 1f)));
        // No drag yet: the facing is the line's left-hand normal, as for a drag (OrderClassifier.LineFacing).
        AssertVector(Vector2.right, TroopOrderPlanner.DeadZoneDirection(Vector2.up, Vector2.zero));
    }

    [Test] public void BoxSelect_StartsAtEightPixels()
    {
        Assert.IsFalse(OrderClassifier.IsBoxSelect(Vector2.zero, new Vector2(7.9f, 0f), 8f));
        Assert.IsTrue(OrderClassifier.IsBoxSelect(Vector2.zero, new Vector2(8f, 0f), 8f));
    }

    // SU-156: the drag's direction sets the front: left to right faces up, right to left faces down.
    [Test]
    public void LineFacing_IsTheDragsLeftHandNormal()
    {
        AssertVector(Vector2.up, OrderClassifier.LineFacing(Vector2.zero, new Vector2(10f, 0f)));
        AssertVector(Vector2.down, OrderClassifier.LineFacing(new Vector2(10f, 0f), Vector2.zero));
        AssertVector(Vector2.left, OrderClassifier.LineFacing(Vector2.zero, new Vector2(0f, 10f)));
    }

    [Test]
    public void FaceToward_IsFromTheSelectionCentre_OrNoneOnIt()
    {
        AssertVector(Vector2.right, OrderClassifier.FaceToward(Vector2.zero, new Vector2(5f, 0f)).Value);
        Assert.IsNull(OrderClassifier.FaceToward(new Vector2(2f, 2f), new Vector2(2f, 2f)));
    }

    static void AssertVector(Vector2 expected, Vector2 actual)
    {
        Assert.AreEqual(expected.x, actual.x, 1e-5f);
        Assert.AreEqual(expected.y, actual.y, 1e-5f);
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
