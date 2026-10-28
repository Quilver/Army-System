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
}
