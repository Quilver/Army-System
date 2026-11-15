using System.Collections.Generic;
using UnityEngine;

// Steering circles (SU-173), after "Techniques for Formation Movement Using Steering Circles" (Game AI Pro, ch. 21). Pure
// maths. A line that has to turn does not rotate on the spot: its front rank's centre follows a path of arcs of the
// formation's turn radius r and straight tangents, and the line faces along the path, so it wheels:
//   start arc  on a circle beside the start, from the start to where its tangent points at the next part;
//   tangent    a straight march;
//   end arc    (only with an end facing) on a circle beside the target, arriving facing the end facing.
// With r = half the line's length the start circle's centre is the end of the front rank on the inside of the turn, so
// that end stands while the other swings round it: the drill wheel. A larger r is a wider arc on which the inner end
// still walks forward.
// Choosing the circles: the source puts each circle on the side facing the other end and, where the two overlap, swaps
// both sides, which can send a line round a full loop to sidestep a few lines' widths. Instead every combination of
// sides (left or right at each end; at a free end only the start's) is tried, at r and at tighter radii down to
// minRadiusShare x r, and the shortest is taken, a tighter radius counting as longer (its length x (1 + (1 - share))), so
// the drill wheel is kept unless a tighter one saves a good deal. A tighter wheel turns about a point inside the line,
// whose inner end steps back a little.
public static class SteeringCircles
{
    // One point of a path: where the front rank's centre is, the way the line faces, and whether it is on an arc.
    public struct Sample
    {
        public Vector2 Position;
        public Vector2 Facing;
        public bool OnArc;
        public float Distance;   // along the path from its start
    }

    // A path in closed form: start arc on (c1, sense1) to exit, straight to enter, end arc on (c2, sense2) to the target.
    struct Plan
    {
        public float Radius;
        public Vector2 C1;
        public float Sense1;
        public Vector2 Exit;
        public bool HasEnd;
        public Vector2 C2;
        public float Sense2;
        public Vector2 Enter;
        public float Length;
    }

    const float Epsilon = 1e-5f;
    static readonly float[] RadiusShares = { 1f, 0.75f, 0.5f, 0.35f, 0.25f };

    // Builds the path into samples (about sampleSpacing apart), with its radius. Returns false when there is none (the
    // target is on the start, or no combination joins up).
    public static bool Build(Vector2 start, Vector2 startFacing, Vector2 target, Vector2? endFacing, float radius,
        float sampleSpacing, List<Sample> samples, float minRadiusShare = 0.5f)
    {
        return Build(start, startFacing, target, endFacing, radius, sampleSpacing, samples, minRadiusShare, out _);
    }

    public static bool Build(Vector2 start, Vector2 startFacing, Vector2 target, Vector2? endFacing, float radius,
        float sampleSpacing, List<Sample> samples, float minRadiusShare, out float usedRadius)
    {
        samples.Clear();
        usedRadius = radius;
        if (radius <= Epsilon || startFacing.sqrMagnitude < Epsilon) return false;
        if ((target - start).sqrMagnitude < Epsilon) return false;
        startFacing.Normalize();
        bool hasEnd = endFacing.HasValue && endFacing.Value.sqrMagnitude > Epsilon;
        Vector2 end = hasEnd ? endFacing.Value.normalized : Vector2.zero;
        sampleSpacing = Mathf.Max(0.02f, sampleSpacing);

        bool found = false;
        Plan best = default;
        float bestCost = float.PositiveInfinity;
        foreach (float share in RadiusShares)
        {
            if (share < minRadiusShare - 1e-4f) continue;
            float r = radius * share;
            float weight = 1f + (1f - share);
            for (int a = 0; a < 2; a++)
            {
                float sense1 = a == 0 ? 1f : -1f;
                Vector2 c1 = Centre(start, startFacing, sense1, r);
                if (!hasEnd)
                {
                    if ((target - c1).magnitude < r + Epsilon) continue;
                    Vector2 exit = TangentPointToward(c1, r, sense1, target);
                    float length = Sweep(c1, sense1, start, exit) * r + Vector2.Distance(exit, target);
                    Consider(new Plan { Radius = r, C1 = c1, Sense1 = sense1, Exit = exit, Length = length });
                    continue;
                }
                for (int b = 0; b < 2; b++)
                {
                    float sense2 = b == 0 ? 1f : -1f;
                    Vector2 c2 = Centre(target, end, sense2, r);
                    if (!Tangent(c1, c2, r, sense1, sense2, out Vector2 exit, out Vector2 enter)) continue;
                    float length = Sweep(c1, sense1, start, exit) * r + Vector2.Distance(exit, enter)
                        + Sweep(c2, sense2, enter, target) * r;
                    Consider(new Plan
                    {
                        Radius = r, C1 = c1, Sense1 = sense1, Exit = exit, HasEnd = true, C2 = c2, Sense2 = sense2,
                        Enter = enter, Length = length,
                    });
                }
            }

            void Consider(Plan plan)
            {
                float cost = plan.Length * weight;
                if (cost >= bestCost) return;
                bestCost = cost;
                best = plan;
                found = true;
            }
        }
        if (!found) return false;

        usedRadius = best.Radius;
        AddArc(samples, best.C1, best.Radius, best.Sense1, start, best.Exit, sampleSpacing);
        if (!best.HasEnd)
        {
            AddStraight(samples, best.Exit, target, sampleSpacing);
            return samples.Count >= 2;
        }
        AddStraight(samples, best.Exit, best.Enter, sampleSpacing);
        AddArc(samples, best.C2, best.Radius, best.Sense2, best.Enter, target, sampleSpacing);
        // Arrive facing exactly the end facing.
        Sample last = samples[samples.Count - 1];
        last.Facing = end;
        samples[samples.Count - 1] = last;
        return samples.Count >= 2;
    }

    // The angle swept from 'from' to 'to' round centre in the given sense, 0 to 2 pi.
    static float Sweep(Vector2 centre, float sense, Vector2 from, Vector2 to)
    {
        float startAngle = Mathf.Atan2(from.y - centre.y, from.x - centre.x);
        float endAngle = Mathf.Atan2(to.y - centre.y, to.x - centre.x);
        float sweep = Mathf.Repeat(sense > 0f ? endAngle - startAngle : startAngle - endAngle, Mathf.PI * 2f);
        return sweep > Mathf.PI * 2f - 1e-4f ? 0f : sweep;
    }

    // The sample at a distance along the path (interpolated), clamped to its ends.
    public static Sample At(List<Sample> samples, float distance)
    {
        if (samples.Count == 0) return default;
        if (distance <= 0f) return samples[0];
        Sample last = samples[samples.Count - 1];
        if (distance >= last.Distance) return last;
        int low = 0, high = samples.Count - 1;
        while (high - low > 1)
        {
            int mid = (low + high) / 2;
            if (samples[mid].Distance <= distance) low = mid;
            else high = mid;
        }
        Sample s0 = samples[low], s1 = samples[high];
        float span = s1.Distance - s0.Distance;
        float t = span > Epsilon ? (distance - s0.Distance) / span : 0f;
        Vector2 facing = Vector2.Lerp(s0.Facing, s1.Facing, t);
        return new Sample
        {
            Position = Vector2.Lerp(s0.Position, s1.Position, t),
            Facing = facing.sqrMagnitude > Epsilon ? facing.normalized : s0.Facing,
            OnArc = s0.OnArc || s1.OnArc,
            Distance = distance,
        };
    }

    public static float Length(List<Sample> samples) => samples.Count == 0 ? 0f : samples[samples.Count - 1].Distance;

    static Vector2 Left(Vector2 v) => new(-v.y, v.x);

    // Centre of the circle of radius r through point, tangent to facing, turning with sense (+1 left, -1 right).
    static Vector2 Centre(Vector2 point, Vector2 facing, float sense, float radius) => point + Left(facing) * (sense * radius);

    // The point on circle (centre, radius) where travel with sense heads straight at an outside point.
    static Vector2 TangentPointToward(Vector2 centre, float radius, float sense, Vector2 outside)
    {
        Vector2 d = outside - centre;
        float distance = d.magnitude;
        float beta = Mathf.Acos(Mathf.Clamp(radius / distance, -1f, 1f));
        Vector2 radial = Rotate(d / distance, -sense * beta);
        return centre + radial * radius;
    }

    // The tangent from circle a (sense sa) to circle b (sense sb): same senses an outer tangent, opposite an inner one.
    static bool Tangent(Vector2 a, Vector2 b, float radius, float sa, float sb, out Vector2 exitA, out Vector2 enterB)
    {
        Vector2 between = b - a;
        float distance = between.magnitude;
        exitA = enterB = default;
        if (distance < Epsilon) return false;
        if (Mathf.Approximately(sa, sb))
        {
            // Outer tangent: travelling along a->b with the centres on the side of the turn.
            Vector2 offset = Rotate(between / distance, -sa * Mathf.PI * 0.5f) * radius;
            exitA = a + offset;
            enterB = b + offset;
            return true;
        }
        if (distance < 2f * radius) return false;
        // Inner tangent: it crosses the midpoint between the centres.
        Vector2 middle = (a + b) * 0.5f;
        exitA = TangentPointToward(a, radius, sa, middle);
        enterB = middle + (middle - exitA);
        return true;
    }

    static void AddArc(List<Sample> samples, Vector2 centre, float radius, float sense, Vector2 from, Vector2 to,
        float spacing)
    {
        float startAngle = Mathf.Atan2(from.y - centre.y, from.x - centre.x);
        float sweep = Sweep(centre, sense, from, to);
        int steps = Mathf.Max(1, Mathf.CeilToInt(sweep * radius / spacing));
        for (int i = 0; i <= steps; i++)
        {
            float angle = startAngle + sense * sweep * i / steps;
            Vector2 radial = new(Mathf.Cos(angle), Mathf.Sin(angle));
            Add(samples, centre + radial * radius, Left(radial) * sense, sweep > 1e-4f);
        }
    }

    static void AddStraight(List<Sample> samples, Vector2 from, Vector2 to, float spacing)
    {
        Vector2 line = to - from;
        float length = line.magnitude;
        if (length < Epsilon) return;
        Vector2 facing = line / length;
        int steps = Mathf.Max(1, Mathf.CeilToInt(length / spacing));
        for (int i = 0; i <= steps; i++) Add(samples, from + line * (i / (float)steps), facing, false);
    }

    static void Add(List<Sample> samples, Vector2 position, Vector2 facing, bool onArc)
    {
        float distance = 0f;
        if (samples.Count > 0)
        {
            Sample previous = samples[samples.Count - 1];
            float step = Vector2.Distance(previous.Position, position);
            if (step < 1e-5f)
            {
                previous.Facing = facing;
                previous.OnArc |= onArc;
                samples[samples.Count - 1] = previous;
                return;
            }
            distance = previous.Distance + step;
        }
        samples.Add(new Sample { Position = position, Facing = facing.normalized, OnArc = onArc, Distance = distance });
    }

    static Vector2 Rotate(Vector2 v, float radians)
    {
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}
