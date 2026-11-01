using System.Collections.Generic;
using UnityEngine;

// The engaged frontage of two engaged troops: which of their soldiers are fighting across the gap between the lines.
// Used by the engagement view (TroopEngagementView); read-only, it never changes the battle.
//   A soldier is engaged when a living soldier of the opposing troop is within reach. Each engaged soldier, of either
//   troop, is linked to its nearest such enemy, so the links cover exactly the stretch of front that is in contact and
//   there are more of them where more soldiers fight.
//   The default reach is two front ranks held EngagementGap apart (each footprint edge is a soldier radius beyond its
//   front-rank centres) plus a slack, so the front rank counts and the rank behind it does not.
public static class EngagementFrontage
{
    public readonly struct Link
    {
        public readonly Vector2 From;      // the engaged soldier
        public readonly Vector2 To;        // its nearest enemy soldier within reach
        public readonly bool FromFirst;    // the engaged soldier belongs to the first troop of the pair

        public Link(Vector2 from, Vector2 to, bool fromFirst)
        {
            From = from;
            To = to;
            FromFirst = fromFirst;
        }

        public Vector2 Midpoint => (From + To) * 0.5f;
    }

    static readonly List<Vector2> FirstPositions = new();
    static readonly List<Vector2> SecondPositions = new();

    public static float Reach(float engagementGap, float slack) =>
        Mathf.Max(TroopConstants.ContactDistance, engagementGap + 2f * TroopConstants.SoldierRadius + Mathf.Max(0f, slack));

    // Every opposing pair of troops that are engaged this frame (Troop.Engagements), once per pair. Routed or empty troops
    // are left out.
    public static void CollectEngagedPairs(IReadOnlyList<Troop> troops, List<(Troop first, Troop second)> pairs)
    {
        pairs.Clear();
        foreach (var troop in troops)
        {
            if (!CanShow(troop)) continue;
            foreach (var record in troop.Engagements)
            {
                Troop other = record.Opponent;
                if (!record.Engaged || !CanShow(other)) continue;
                if (troop.GetInstanceID() > other.GetInstanceID()) continue;   // the other troop adds the pair
                pairs.Add((troop, other));
            }
        }
    }

    static bool CanShow(Troop troop) => troop != null && !troop.IsRouted && troop.LivingSoldierCount > 0;

    // Adds a link for every engaged soldier of either troop (see the class comment) and counts them.
    public static void Measure(Troop first, Troop second, float reach, List<Link> links,
        out int firstEngaged, out int secondEngaged)
    {
        Gather(first, second, reach, FirstPositions);
        Gather(second, first, reach, SecondPositions);
        firstEngaged = AddLinks(FirstPositions, SecondPositions, reach, true, links);
        secondEngaged = AddLinks(SecondPositions, FirstPositions, reach, false, links);
    }

    // Living soldiers of troop that are within reach of the other troop's footprint (a cheap first cut).
    static void Gather(Troop troop, Troop other, float reach, List<Vector2> positions)
    {
        positions.Clear();
        TroopFootprint footprint = other.Footprint;
        foreach (var soldier in troop.Soldiers)
        {
            if (!soldier.IsAlive) continue;
            Vector2 position = troop.WorldPosition(soldier);
            if (footprint.SignedDistance(position) <= reach) positions.Add(position);
        }
    }

    static int AddLinks(List<Vector2> soldiers, List<Vector2> enemies, float reach, bool fromFirst, List<Link> links)
    {
        float reachSquared = reach * reach;
        int engaged = 0;
        foreach (Vector2 soldier in soldiers)
        {
            float best = reachSquared;
            int nearest = -1;
            for (int i = 0; i < enemies.Count; i++)
            {
                float distance = (enemies[i] - soldier).sqrMagnitude;
                if (distance > best) continue;
                best = distance;
                nearest = i;
            }
            if (nearest < 0) continue;
            links.Add(new Link(soldier, enemies[nearest], fromFirst));
            engaged++;
        }
        return engaged;
    }
}
