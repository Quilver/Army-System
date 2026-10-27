using System.Collections.Generic;
using UnityEngine;

// Pulls linked troops toward each other so their natural line points meet at the joint (spec §3.4.1 pinned anchor):
// a spring with a small dead zone plus relative-velocity matching. The force is applied only to the unengaged side:
// tension never moves an engaged troop out of melee. If the stretch or hinge limit is exceeded the link snaps
// (ChainLinker) instead of dragging the engaged troop. Pending links have no tension.
public static class AlliedLinkTension
{
    const float LinkVelocityMatching = 1f;

    public static void Apply(IReadOnlyList<Troop> troops, Dictionary<Troop, Vector2> forces)
    {
        TroopTuning tuning = TroopTuning.Active;
        foreach (var troop in troops)
        {
            if (troop == null) continue;
            for (int end = 0; end < TroopConstants.MaxChainLinks; end++)
            {
                if (!troop.IsLinkActive(end)) continue;
                TroopEndpointLink link = troop.GetLineLink(end);
                Troop other = link.Other;
                if (troop.GetInstanceID() >= other.GetInstanceID() || !forces.ContainsKey(other)) continue;

                bool troopFree = !troop.IsEngaged && !troop.IsRouted;
                bool otherFree = !other.IsEngaged && !other.IsRouted;
                if (!troopFree && !otherFree) continue;

                float gap = link.Tension.magnitude;
                float extension = Mathf.Max(0f, gap - tuning.LinkSlack);
                float tension = Mathf.Min(tuning.LinkMaxForce, extension * tuning.LinkStiffness);
                Vector2 force = gap > 0.0001f ? link.Tension / gap * tension : Vector2.zero;

                // Damp relative motion so linked troops travel together, but only when neither side is pinned.
                if (troopFree && otherFree)
                    force += (other.PivotVelocity - troop.PivotVelocity) * LinkVelocityMatching;
                if (force.sqrMagnitude <= 0.000001f) continue;

                if (troopFree && otherFree)
                {
                    forces[troop] += force;
                    forces[other] -= force;
                }
                else if (troopFree)
                {
                    forces[troop] += force * 2f;
                }
                else
                {
                    forces[other] -= force * 2f;
                }
            }
        }
    }
}
