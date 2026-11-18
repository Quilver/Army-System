using System;
using System.Collections.Generic;
using UnityEngine;

// Retreat through friends (SU-181, spec §5.5): a troop falling back may pass through friendly troops, but it may not
// stop inside one. Given the troop's outline and pivot, the way it falls back and how far it would go, this finds how far
// it actually goes:
//   - If it ends clear of every friend, the retreat is unchanged.
//   - If its outline would end inside a friend's, it goes on the same way to the first clear spot past the friends, if
//     the caller says that spot can be reached (terrain, the field edge, enemies standing there).
//   - If it would only end too close to a friend, or the far side cannot be reached or lies more than MaxExtraDistance
//     further, it stops short: at the furthest clear spot before the friends (where it stands, at worst).
// Clear means, for each friend, an outline gap of at least wantedGap (FriendlyGap) and pivots at least the friend's
// PivotSpacing apart (spec §2.4), or the gap and spacing it has now if those are smaller: a friend the troop already
// stands close beside (a neighbour in line) is not in the way of falling straight back past it. Whatever it had before,
// it never ends with the friend's pivot inside its outline or its own pivot inside the friend's: that is ending inside
// the friend, even for one that was already pressed into it. Pure geometry.
public static class RetreatPassage
{
    // A friendly troop the retreat may pass through: its outline, its pivot and the least pivot spacing to keep from it
    // (PivotSeparationScale x the two radii; 0 for none).
    public readonly struct Friend
    {
        public readonly TroopFootprint Outline;
        public readonly Vector2 Pivot;
        public readonly float PivotSpacing;

        public Friend(TroopFootprint outline, Vector2 pivot, float pivotSpacing)
        {
            Outline = outline;
            Pivot = pivot;
            PivotSpacing = Mathf.Max(0f, pivotSpacing);
        }

        // Outline only, no pivot spacing.
        public Friend(TroopFootprint outline) : this(outline, outline.Center, 0f) { }
    }

    // Spots are tried this far apart, then the edge of a clear stretch is found to within Tolerance.
    public const float Step = 0.25f;
    // A retreat goes at most this much further than its own distance to get past friends.
    public const float MaxExtraDistance = 40f;
    const float Tolerance = 0.01f;
    const int RefineIterations = 5;

    // outline and pivot: the retreating troop's now. Returns the distance it goes along direction.
    public static float Distance(in TroopFootprint outline, Vector2 pivot, Vector2 direction, float distance,
        IReadOnlyList<Friend> friends, float wantedGap, Func<float, bool> farSideReachable = null)
    {
        if (distance <= 0f || friends == null || friends.Count == 0 || direction.sqrMagnitude < 1e-8f) return distance;
        direction.Normalize();
        var start = new Start(outline, pivot);
        if (IsClear(start, direction, distance, friends, wantedGap)) return distance;

        // Past the friends: the first clear spot beyond the end of the retreat, when it would end inside one.
        float blocked = distance;
        float limit = EndsInside(start, direction, distance, friends) ? distance + MaxExtraDistance : distance;
        for (float d = distance + Step; d <= limit + 1e-4f; d += Step)
        {
            if (!IsClear(start, direction, d, friends, wantedGap))
            {
                blocked = d;
                continue;
            }
            float far = EdgeOfClear(start, direction, d, blocked, friends, wantedGap);
            if (farSideReachable == null || farSideReachable(far)) return far;
            break;
        }

        // Short of them: the last clear spot before the end of the retreat.
        float stillBlocked = distance;
        for (float d = distance - Step; ; d -= Step)
        {
            d = Mathf.Max(0f, d);
            if (IsClear(start, direction, d, friends, wantedGap))
                return EdgeOfClear(start, direction, d, stillBlocked, friends, wantedGap);
            if (d <= 0f) return 0f;
            stillBlocked = d;
        }
    }

    readonly struct Start
    {
        public readonly TroopFootprint Outline;
        public readonly Vector2 Pivot;

        public Start(TroopFootprint outline, Vector2 pivot)
        {
            Outline = outline;
            Pivot = pivot;
        }
    }

    // Whether the troop moved distance along direction is clear of every friend (see the class comment).
    public static bool IsClear(in TroopFootprint outline, Vector2 pivot, Vector2 direction, float distance,
        IReadOnlyList<Friend> friends, float wantedGap) =>
        IsClear(new Start(outline, pivot), direction.normalized, distance, friends, wantedGap);

    static bool IsClear(in Start start, Vector2 direction, float distance, IReadOnlyList<Friend> friends, float wantedGap)
    {
        Vector2 shift = direction * distance;
        TroopFootprint moved = start.Outline.Translated(shift);
        Vector2 movedPivot = start.Pivot + shift;
        for (int i = 0; i < friends.Count; i++)
        {
            Friend friend = friends[i];
            float neededGap = Mathf.Min(wantedGap, FootprintGap.Gap(start.Outline, friend.Outline)) - Tolerance;
            if (FootprintGap.Gap(moved, friend.Outline) < neededGap) return false;
            if (PivotsInside(moved, movedPivot, friend)) return false;
            if (friend.PivotSpacing <= 0f) continue;
            float neededSpacing = Mathf.Min(friend.PivotSpacing, Vector2.Distance(start.Pivot, friend.Pivot)) - Tolerance;
            if (Vector2.Distance(movedPivot, friend.Pivot) < neededSpacing) return false;
        }
        return true;
    }

    // Whether the troop moved distance along direction ends inside a friend: its outline overlaps the friend's more than
    // it does now, or one of the two pivots stands inside the other's outline.
    static bool EndsInside(in Start start, Vector2 direction, float distance, IReadOnlyList<Friend> friends)
    {
        Vector2 shift = direction * distance;
        TroopFootprint moved = start.Outline.Translated(shift);
        for (int i = 0; i < friends.Count; i++)
        {
            float now = FootprintGap.Gap(start.Outline, friends[i].Outline);
            if (FootprintGap.Gap(moved, friends[i].Outline) < Mathf.Min(0f, now) - Tolerance) return true;
            if (PivotsInside(moved, start.Pivot + shift, friends[i])) return true;
        }
        return false;
    }

    static bool PivotsInside(in TroopFootprint outline, Vector2 pivot, in Friend friend) =>
        outline.Contains(friend.Pivot) || friend.Outline.Contains(pivot);

    // Narrows a clear distance toward a blocked one (either side of it) and returns the clear distance nearest the edge.
    static float EdgeOfClear(in Start start, Vector2 direction, float clear, float blocked, IReadOnlyList<Friend> friends,
        float wantedGap)
    {
        for (int i = 0; i < RefineIterations && Mathf.Abs(blocked - clear) > Tolerance; i++)
        {
            float middle = (clear + blocked) * 0.5f;
            if (IsClear(start, direction, middle, friends, wantedGap)) clear = middle;
            else blocked = middle;
        }
        return clear;
    }
}
