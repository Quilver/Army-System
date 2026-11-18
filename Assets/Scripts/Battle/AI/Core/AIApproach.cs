using System.Collections.Generic;
using UnityEngine;

namespace LeveragedWars.Battle.AI
{
    /// <summary>
    /// The ground a troop's soldiers cover, as an oriented box: HalfLength along its front (towards its right), HalfDepth
    /// front to back. Troops offer it through IAIFootprintTroop; <see cref="Of"/> estimates one for any other troop.
    /// </summary>
    public readonly struct AIFootprint
    {
        public readonly Vector3 Centre;
        public readonly Vector3 Forward;
        public readonly float HalfLength;
        public readonly float HalfDepth;

        public AIFootprint(Vector3 centre, Vector3 forward, float halfLength, float halfDepth)
        {
            Centre = AIGeometry.Flat(centre);
            Forward = AIGeometry.FlatForward(forward);
            HalfLength = Mathf.Max(0f, halfLength);
            HalfDepth = Mathf.Max(0f, halfDepth);
        }

        public Vector3 Right => AIGeometry.Right(Forward);

        /// <summary>The troop's own footprint, or a box its frontage wide and a quarter of that deep (at least 1 unit).</summary>
        public static AIFootprint Of(IAITroop troop) =>
            troop is IAIFootprintTroop withFootprint
                ? withFootprint.Footprint
                : new AIFootprint(troop.Position, troop.Forward, troop.Frontage * 0.5f, Mathf.Max(1f, troop.Frontage * 0.25f));
    }

    /// <summary>
    /// Going round friends (SU-179). A melee troop sent at an enemy must not push through its own side's troops to get
    /// there (Movie_024: the AI's huskarls marched through their own spear line at the enemy fighting it). If the straight
    /// way to the target crosses a friend's footprint grown by the clearance, the troop takes the shortest way round its
    /// friends: from corner to corner of their grown footprints, never through one. Friends standing closer together than
    /// twice the clearance (a chain) leave no way between them, so the way goes round the whole group.
    /// Pure geometry on the ground plane, no Unity lifecycle.
    /// </summary>
    public static class AIApproach
    {
        // Corners are placed this much further out than the clearance (as a share of it, plus a fixed part), so the way
        // from one corner to the next passes clear of the box.
        const float CornerMarginShare = 0.5f;
        const float CornerMarginFixed = 0.25f;
        // Friends further than this beyond the way's half length from its middle are not considered (units).
        const float SearchMargin = 15f;
        const float Epsilon = 1e-4f;

        // Scratch, so a tick allocates nothing once the lists have grown.
        static readonly List<AIFootprint> boxes = new List<AIFootprint>();
        static readonly List<float> grows = new List<float>();
        static readonly List<Vector3> nodes = new List<Vector3>();
        static readonly List<float> cost = new List<float>();
        static readonly List<int> previous = new List<int>();
        static readonly List<bool> done = new List<bool>();

        /// <summary>
        /// True if the straight way from <paramref name="from"/> to <paramref name="to"/> crosses a friend's footprint
        /// grown by <paramref name="clearance"/>; <paramref name="waypoint"/> is then the first corner on the shortest way
        /// round. A friend whose grown footprint holds either end is checked without the clearance, and one whose own
        /// footprint holds either end is left out (the troop is already against it, or its target is). False when the way
        /// is clear or there is no way round.
        /// </summary>
        public static bool TryFindDetour(Vector3 from, Vector3 to, IReadOnlyList<AIFootprint> friends, float clearance,
            out Vector3 waypoint)
        {
            waypoint = default;
            Vector3 a = AIGeometry.Flat(from);
            Vector3 t = AIGeometry.Flat(to);
            if ((t - a).sqrMagnitude < Epsilon || friends == null || friends.Count == 0) return false;
            clearance = Mathf.Max(0f, clearance);

            // The friends that count, each with the growth it is checked with.
            boxes.Clear();
            grows.Clear();
            Vector3 middle = (a + t) * 0.5f;
            float reach = AIGeometry.FlatDistance(a, t) * 0.5f + SearchMargin;
            bool blocked = false;
            for (int i = 0; i < friends.Count; i++)
            {
                AIFootprint friend = friends[i];
                if (AIGeometry.FlatDistance(friend.Centre, middle) > reach + friend.HalfLength + friend.HalfDepth) continue;
                if (Contains(friend, a, 0f) || Contains(friend, t, 0f)) continue;
                float grow = Contains(friend, a, clearance) || Contains(friend, t, clearance) ? 0f : clearance;
                boxes.Add(friend);
                grows.Add(grow);
                if (!blocked && SegmentEntry(a, t, friend, grow, out _)) blocked = true;
            }
            if (!blocked) return false;

            // Shortest way round: A (0), T (1) and four corners per friend, each grown past its check.
            nodes.Clear();
            nodes.Add(a);
            nodes.Add(t);
            for (int i = 0; i < boxes.Count; i++)
            {
                AIFootprint box = boxes[i];
                float cornerGrow = grows[i] + clearance * CornerMarginShare + CornerMarginFixed;
                Vector3 r = box.Right * (box.HalfLength + cornerGrow);
                Vector3 f = box.Forward * (box.HalfDepth + cornerGrow);
                nodes.Add(box.Centre + r + f);
                nodes.Add(box.Centre - r + f);
                nodes.Add(box.Centre - r - f);
                nodes.Add(box.Centre + r - f);
            }
            int next = ShortestFirstStep();
            if (next < 2) return false;
            waypoint = nodes[next];
            return true;
        }

        /// <summary>True if the point lies inside the footprint grown by grow on every side.</summary>
        public static bool Contains(AIFootprint box, Vector3 point, float grow)
        {
            Vector3 d = AIGeometry.Flat(point - box.Centre);
            return Mathf.Abs(Vector3.Dot(d, box.Right)) < box.HalfLength + grow - Epsilon
                && Mathf.Abs(Vector3.Dot(d, box.Forward)) < box.HalfDepth + grow - Epsilon;
        }

        /// <summary>
        /// True if the segment a-b runs through the inside of the footprint grown by grow (touching a side or a corner does
        /// not count); entry is how far along the segment (0 to 1) it goes in.
        /// </summary>
        public static bool SegmentEntry(Vector3 a, Vector3 b, AIFootprint box, float grow, out float entry)
        {
            entry = 0f;
            Vector3 da = AIGeometry.Flat(a - box.Centre);
            Vector3 db = AIGeometry.Flat(b - box.Centre);
            float enter = 0f, exit = 1f;
            if (!Slab(Vector3.Dot(da, box.Right), Vector3.Dot(db, box.Right), box.HalfLength + grow, ref enter, ref exit) ||
                !Slab(Vector3.Dot(da, box.Forward), Vector3.Dot(db, box.Forward), box.HalfDepth + grow, ref enter, ref exit))
                return false;
            if ((exit - enter) * AIGeometry.FlatDistance(a, b) <= Epsilon * 10f) return false;
            entry = enter;
            return true;
        }

        static bool Slab(float start, float end, float half, ref float enter, ref float exit)
        {
            float delta = end - start;
            if (Mathf.Abs(delta) < 1e-6f) return Mathf.Abs(start) < half;
            float t0 = (-half - start) / delta;
            float t1 = (half - start) / delta;
            if (t0 > t1) (t0, t1) = (t1, t0);
            enter = Mathf.Max(enter, t0);
            exit = Mathf.Min(exit, t1);
            return enter < exit;
        }

        // A segment between two nodes is open if it runs through no friend's checked box.
        static bool Open(Vector3 p, Vector3 q)
        {
            for (int i = 0; i < boxes.Count; i++)
                if (SegmentEntry(p, q, boxes[i], grows[i], out _)) return false;
            return true;
        }

        // Dijkstra from A (node 0) to T (node 1). Returns the node after A on the shortest way, or -1.
        static int ShortestFirstStep()
        {
            int n = nodes.Count;
            cost.Clear();
            previous.Clear();
            done.Clear();
            for (int i = 0; i < n; i++)
            {
                cost.Add(float.PositiveInfinity);
                previous.Add(-1);
                done.Add(false);
            }
            cost[0] = 0f;
            for (int round = 0; round < n; round++)
            {
                int u = -1;
                for (int i = 0; i < n; i++)
                    if (!done[i] && (u < 0 || cost[i] < cost[u])) u = i;
                if (u < 0 || float.IsPositiveInfinity(cost[u])) break;
                done[u] = true;
                if (u == 1) break;
                for (int v = 1; v < n; v++)
                {
                    if (done[v]) continue;
                    float c = cost[u] + AIGeometry.FlatDistance(nodes[u], nodes[v]);
                    if (c >= cost[v] || !Open(nodes[u], nodes[v])) continue;
                    cost[v] = c;
                    previous[v] = u;
                }
            }
            if (previous[1] < 0) return -1;
            int step = 1;
            while (previous[step] != 0) step = previous[step];
            return step;
        }
    }
}
