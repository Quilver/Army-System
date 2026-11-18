using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Retreat through friends (SU-181, RetreatPassage): a retreat that would end inside a friendly troop goes on to the far
// side of it, clear by FriendlyGap (and the pivot spacing); if the far side cannot be reached, or it would only end too
// close, it stops short of the friend instead. Each troop here is a line 9 long and 2 deep facing +x, centred on its
// pivot, so a retreat runs toward -x.
public sealed class RetreatPassageTests
{
    const float Gap = 0.5f;
    const float Retreat = 5f;
    const float Precision = 0.03f;
    static readonly Vector2 Back = Vector2.left;

    // A line 9 long (along y) and 2 deep (along x) centred on (x, y), facing +x.
    static TroopFootprint Line(float x, float y = 0f) =>
        TroopFootprint.Rect(new Vector2(x, y - 4.5f), new Vector2(x, y + 4.5f), Vector2.right, 1f);

    static List<RetreatPassage.Friend> Friends(params TroopFootprint[] outlines)
    {
        var friends = new List<RetreatPassage.Friend>();
        foreach (var outline in outlines) friends.Add(new RetreatPassage.Friend(outline));
        return friends;
    }

    static float Distance(params TroopFootprint[] friends) =>
        RetreatPassage.Distance(Line(0f), Vector2.zero, Back, Retreat, Friends(friends), Gap);

    [Test]
    public void NoFriends_OrAClearEnd_LeavesTheRetreatAsItIs()
    {
        Assert.AreEqual(Retreat, Distance(), 1e-5f);
        Assert.AreEqual(Retreat, Distance(Line(-10f)), 1e-5f, "a friend 3 beyond the end of the retreat is not in the way");
    }

    [Test]
    public void AFriendWhereTheRetreatEnds_IsPassedThrough_ToFriendlyGapBeyondIt()
    {
        TroopFootprint friend = Line(-5f);
        float distance = Distance(friend);
        // The friend's rear edge is at -6, so our front edge goes to -6.5 and our centre to -7.5.
        Assert.AreEqual(7.5f, distance, Precision);
        TroopFootprint end = Line(0f).Translated(Back * distance);
        Assert.GreaterOrEqual(FootprintGap.Gap(end, friend), Gap - Precision, "clear of the friend at the end");
        Assert.Less(end.Center.x, friend.Center.x, "on the far side of it");
    }

    [Test]
    public void TwoFriendsWithNoRoomBetweenThem_ArePassedTogether()
    {
        // Half a unit between them: no room for a troop 2 deep with FriendlyGap either side.
        float distance = Distance(Line(-5f), Line(-7.5f));
        Assert.AreEqual(10f, distance, Precision);
    }

    [Test]
    public void TwoFriendsWithRoomBetweenThem_TheRetreatStopsBetweenThem()
    {
        float distance = Distance(Line(-5f), Line(-10.5f));
        Assert.AreEqual(7.5f, distance, Precision);
    }

    [Test]
    public void AFriendCloseBeside_DoesNotLengthenARetreatStraightBack()
    {
        // 0.3 apart side by side, closer than FriendlyGap: falling straight back keeps that gap, so it is not in the way.
        TroopFootprint beside = Line(0f, 9.3f);
        Assert.AreEqual(0.3f, FootprintGap.Gap(Line(0f), beside), 1e-4f, "setup");
        Assert.AreEqual(Retreat, Distance(beside), 1e-5f);
    }

    [Test]
    public void AFarSideThatCannotBeReached_StopsTheRetreatShortOfTheFriend()
    {
        TroopFootprint friend = Line(-5f);
        float distance = RetreatPassage.Distance(Line(0f), Vector2.zero, Back, Retreat, Friends(friend), Gap, _ => false);
        // The friend's front edge is at -4, so our rear edge stops at -3.5 and our centre at -2.5.
        Assert.AreEqual(2.5f, distance, Precision);
        Assert.GreaterOrEqual(FootprintGap.Gap(Line(0f).Translated(Back * distance), friend), Gap - Precision);
    }

    [Test]
    public void AFriendTooDeepToPass_StopsTheRetreatShortOfIt()
    {
        // A friend reaching from x = -5 to x = -60: its far side is more than MaxExtraDistance beyond the retreat.
        var deep = TroopFootprint.Rect(new Vector2(-5f, 0f), new Vector2(-60f, 0f), Vector2.up, 4.5f);
        Assert.AreEqual(3.5f, Distance(deep), Precision);
    }

    [Test]
    public void AFriendAlreadyTouching_TheRetreatStaysPut_WhenItCannotPass()
    {
        // A friend 0.2 behind us, reaching back to x = -30.
        var touching = TroopFootprint.Rect(new Vector2(-1.2f, 0f), new Vector2(-30f, 0f), Vector2.up, 4.5f);
        float distance = RetreatPassage.Distance(Line(0f), Vector2.zero, Back, Retreat, Friends(touching), Gap, _ => false);
        Assert.AreEqual(0f, distance, Precision);
    }

    // A thin friend wholly inside the retreat's reach is jumped: the retreat ends clear beyond it, unchanged.
    [Test]
    public void AThinFriendTheRetreatClearsWhole_LeavesItUnchanged()
    {
        Assert.AreEqual(Retreat, Distance(Line(-2.2f)), 1e-5f);
    }

    // Ending too close to a friend without going into it, the retreat stops FriendlyGap short rather than passing it.
    [Test]
    public void EndingTooCloseToAFriend_StopsShortOfIt()
    {
        // The friend's front edge is at -6.2: a plain retreat leaves 0.2 between us. We stop with 0.5: at 4.7.
        Assert.AreEqual(4.7f, Distance(Line(-7.2f)), Precision);
    }

    // A friend already pressed into us, its pivot inside our outline: the retreat does not stop with it still inside
    // (the overlap being no worse is not enough), it passes it.
    [Test]
    public void AFriendAlreadyPressedIn_IsNotLeftInside()
    {
        TroopFootprint pressed = TroopFootprint.Circle(new Vector2(-0.5f, 0f), 0.6f, Vector2.right);
        Assert.IsTrue(Line(0f).Contains(pressed.Center), "setup: its pivot is inside our outline");
        float distance = RetreatPassage.Distance(Line(0f), Vector2.zero, Back, 1f, Friends(pressed), Gap);
        // Our rear edge passes its pivot at 1.5.
        Assert.AreEqual(1.5f, distance, Precision);
        Assert.IsFalse(Line(0f).Translated(Back * distance).Contains(pressed.Center));
    }

    // Pivots are kept the friendly pivot spacing apart too (spec §2.4), so a friend reached does not get pushed away when
    // the retreat ends.
    [Test]
    public void ThroughAFriend_ThePivotSpacingIsKeptAsWell()
    {
        TroopFootprint friend = Line(-5f);
        var friends = new List<RetreatPassage.Friend> { new(friend, friend.Center, 3.75f) };
        float distance = RetreatPassage.Distance(Line(0f), Vector2.zero, Back, Retreat, friends, Gap);
        // FriendlyGap alone would stop at 7.5 (pivots 2.5 apart); the spacing takes us to 8.75.
        Assert.AreEqual(8.75f, distance, Precision);
    }
}
