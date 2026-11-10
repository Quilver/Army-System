using NUnit.Framework;

// Troop frontage from the soldier count (TroopFrontageRule, SU-117).
public sealed class TroopFrontageRuleTests
{
    [TestCase(0, 1)]
    [TestCase(1, 1)]
    [TestCase(3, 3)]
    [TestCase(4, 4)]
    [TestCase(5, 3)]
    [TestCase(6, 3)]
    [TestCase(10, 5)]
    [TestCase(11, 4)]
    [TestCase(15, 5)]
    [TestCase(16, 6)]
    [TestCase(17, 5)]
    [TestCase(20, 5)]
    [TestCase(26, 7)]
    public void Frontage_FollowsTheSoldierCountBands(int soldiers, int frontage)
    {
        Assert.AreEqual(frontage, TroopFrontageRule.ForSoldierCount(soldiers));
    }
}
