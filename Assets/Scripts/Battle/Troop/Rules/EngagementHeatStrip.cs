using System.Collections.Generic;
using UnityEngine;

// The heat map of one engaged pair (TroopEngagementView), built from the two troops' lines. Pure maths.
//   Frame   Along runs with the two lines (their mean direction), Across from the first troop's line toward the
//           second's, Origin midway between the line centres.
//   Extent  where the two lines overlap along Along and, with LimitToReach, where the two footprints are within MaxGap
//           (A_Distance) of each other across the gap (SU-96): only the stretch of front that is within reach of the
//           enemy. Lines at an angle are closer at one end, so the band stops where they draw further apart than that.
//   Band    at each point along, from the first troop's line (SideA) to the second's (SideB), across the line gap.
//   Heat    Weight x AcrossProfile: 0 on each troop's line, 1 in the middle of the gap (4v(1 - v), v = 0 on the first
//           line, 1 on the second). Weight fades from 1 to 0 over EndFade toward both ends of the engagement. The heat
//           is a smooth gradient from the geometry alone: the number of soldiers fighting does not change it.
// When the lines are not roughly parallel (a flank or rear attack) or do not overlap, the frame, extent and band come
// from the engaged soldiers' links instead (EngagementFrontage): a straight band from the first troop's engaged
// soldiers (their mean position across) to the second's, along their extent.
public static class EngagementHeatStrip
{
    // Rows of vertices across the band are Rows + 1.
    public const int Rows = 16;
    // Lines within 45 degrees of each other are treated as facing each other.
    const float ParallelCos = 0.7071f;

    public struct Settings
    {
        public float CellWidth;       // spacing of the columns along the front (world units)
        public float EndFade;         // fade length at each end of the engagement (world units)
        public bool LimitToReach;     // keep only the stretch where the footprints are within MaxGap across the gap
        public float MaxGap;          // A_Distance: the largest footprint gap that still shows heat (world units)
        public float HalfWidthA;      // first troop's footprint half-width across its line (world units)
        public float HalfWidthB;      // second troop's footprint half-width across its line (world units)
    }

    public struct Column
    {
        public float Along;     // position along Along, from Origin
        public float SideA;     // first troop's line, along Across from Origin
        public float SideB;     // second troop's line, along Across from Origin
        public float Weight;    // 0-1: the end fade
    }

    // 0 on each troop's line, 1 in the middle of the gap.
    public static float AcrossProfile(float v)
    {
        v = Mathf.Clamp01(v);
        return 4f * v * (1f - v);
    }

    public static float Intensity(in Column column, float v) => Mathf.Clamp01(column.Weight * AcrossProfile(v));

    // a0-a1 and b0-b1 are the two troops' lines (a point for a troop with no line). Returns false when there is nothing
    // to draw (no overlap and no link).
    public static bool Build(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1, IReadOnlyList<EngagementFrontage.Link> links,
        Settings settings, List<Column> columns, out Vector2 origin, out Vector2 along, out Vector2 across)
    {
        columns.Clear();
        float cell = Mathf.Max(0.01f, settings.CellWidth);
        float linkSideA = 0f, linkSideB = 0f;
        bool fromLines = TryLineFrame(a0, a1, b0, b1, cell, out origin, out along, out across, out float start, out float end);
        if (fromLines && settings.LimitToReach)
        {
            // Facing lines that overlap: keep the stretch within reach, and draw nothing when none of it is.
            float footprints = Mathf.Max(0f, settings.HalfWidthA) + Mathf.Max(0f, settings.HalfWidthB);
            float maxCentreGap = Mathf.Max(0f, settings.MaxGap) + footprints;
            float gapAtStart = SideAt(b0, b1, start, origin, along, across) - SideAt(a0, a1, start, origin, along, across);
            float gapAtEnd = SideAt(b0, b1, end, origin, along, across) - SideAt(a0, a1, end, origin, along, across);
            if (!WithinReach(gapAtStart, gapAtEnd, maxCentreGap, ref start, ref end) || end - start < cell) return false;
        }
        else if (!fromLines && !TryLinkFrame(links, cell, out origin, out along, out across, out start, out end,
                out linkSideA, out linkSideB))
            return false;

        float fade = Mathf.Min(Mathf.Max(0f, settings.EndFade), (end - start) * 0.5f);

        // A column at each end (faded to 0) and one in the middle of every cell between them.
        int interior = Mathf.Max(0, Mathf.CeilToInt((end - start) / cell - 0.25f));
        for (int i = 0; i <= interior + 1; i++)
        {
            float t = i == 0 ? start : i == interior + 1 ? end : start + cell * (i - 0.5f);
            if (i > 0 && i <= interior && t >= end) continue;
            float endFade = fade <= 0f
                ? 1f
                : Mathf.SmoothStep(0f, 1f, (t - start) / fade) * Mathf.SmoothStep(0f, 1f, (end - t) / fade);
            columns.Add(new Column
            {
                Along = t,
                SideA = fromLines ? SideAt(a0, a1, t, origin, along, across) : linkSideA,
                SideB = fromLines ? SideAt(b0, b1, t, origin, along, across) : linkSideB,
                Weight = Mathf.Clamp01(endFade)
            });
        }
        return true;
    }

    // The part of [start, end] where the gap between the line centres, which changes linearly from gapAtStart to
    // gapAtEnd, is at most maxGap. False when no part of it is.
    public static bool WithinReach(float gapAtStart, float gapAtEnd, float maxGap, ref float start, ref float end)
    {
        bool startIn = gapAtStart <= maxGap;
        bool endIn = gapAtEnd <= maxGap;
        if (startIn && endIn) return true;
        if (!startIn && !endIn) return false;
        float crossing = Mathf.Lerp(start, end, (maxGap - gapAtStart) / (gapAtEnd - gapAtStart));
        if (startIn) end = crossing;
        else start = crossing;
        return end > start;
    }

    static bool TryLineFrame(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1, float cell, out Vector2 origin,
        out Vector2 along, out Vector2 across, out float start, out float end)
    {
        origin = ((a0 + a1) + (b0 + b1)) * 0.25f;
        along = Vector2.right;
        across = Vector2.up;
        start = end = 0f;
        Vector2 dA = a1 - a0;
        Vector2 dB = b1 - b0;
        if (dA.sqrMagnitude < 0.01f || dB.sqrMagnitude < 0.01f) return false;
        dA.Normalize();
        dB.Normalize();
        float cos = Vector2.Dot(dA, dB);
        if (Mathf.Abs(cos) < ParallelCos) return false;
        along = (dA + (cos < 0f ? -dB : dB)).normalized;
        across = new Vector2(-along.y, along.x);
        Vector2 between = (b0 + b1) * 0.5f - (a0 + a1) * 0.5f;
        if (Vector2.Dot(across, between) < 0f)
        {
            along = -along;
            across = -across;
        }

        Project(a0, a1, origin, along, out float minA, out float maxA);
        Project(b0, b1, origin, along, out float minB, out float maxB);
        start = Mathf.Max(minA, minB);
        end = Mathf.Min(maxA, maxB);
        return end - start >= cell;
    }

    static void Project(Vector2 p0, Vector2 p1, Vector2 origin, Vector2 axis, out float min, out float max)
    {
        float t0 = Vector2.Dot(p0 - origin, axis);
        float t1 = Vector2.Dot(p1 - origin, axis);
        min = Mathf.Min(t0, t1);
        max = Mathf.Max(t0, t1);
    }

    // Where the line p0-p1 crosses the across-line at t (clamped to the segment), measured along Across.
    static float SideAt(Vector2 p0, Vector2 p1, float t, Vector2 origin, Vector2 along, Vector2 across)
    {
        Vector2 direction = p1 - p0;
        float denominator = Vector2.Dot(direction, along);
        if (Mathf.Abs(denominator) < 1e-4f) return Vector2.Dot((p0 + p1) * 0.5f - origin, across);
        float lambda = Mathf.Clamp01((t - Vector2.Dot(p0 - origin, along)) / denominator);
        return Vector2.Dot(p0 + direction * lambda - origin, across);
    }

    static bool TryLinkFrame(IReadOnlyList<EngagementFrontage.Link> links, float cell, out Vector2 origin,
        out Vector2 along, out Vector2 across, out float start, out float end, out float sideA, out float sideB)
    {
        origin = Vector2.zero;
        along = Vector2.right;
        across = Vector2.up;
        start = end = 0f;
        sideA = sideB = 0f;
        if (links == null || links.Count == 0) return false;

        Vector2 direction = Vector2.zero;
        foreach (var link in links)
        {
            Vector2 offset = link.To - link.From;
            if (offset.sqrMagnitude > 1e-8f) direction += (link.FromFirst ? offset : -offset).normalized;
            origin += link.Midpoint;
        }
        origin /= links.Count;
        across = direction.sqrMagnitude > 1e-8f ? direction.normalized : Vector2.up;
        along = new Vector2(across.y, -across.x);

        start = float.PositiveInfinity;
        end = float.NegativeInfinity;
        foreach (var link in links)
        {
            float t = Vector2.Dot(link.Midpoint - origin, along);
            start = Mathf.Min(start, t);
            end = Mathf.Max(end, t);
            sideA += Vector2.Dot((link.FromFirst ? link.From : link.To) - origin, across);
            sideB += Vector2.Dot((link.FromFirst ? link.To : link.From) - origin, across);
        }
        sideA /= links.Count;
        sideB /= links.Count;
        start -= cell * 0.5f;
        end += cell * 0.5f;
        return true;
    }
}
