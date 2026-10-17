using System.Collections.Generic;
using UnityEngine;

// Allied endpoint links: a stiff spring with a small dead zone plus relative-velocity matching.
public static class AlliedLinkTension
{
    const float LinkSlack = 0.1f;
    const float LinkStiffness = 5f;
    const float LinkMaxForce = 10.4f;
    const float LinkVelocityMatching = 1f;

    public static void Apply(IReadOnlyList<Troop> troops, Dictionary<Troop, Vector2> forces)
    {
        foreach (var troop in troops)
        {
            if (troop == null) continue;
            for (int end = 0; end < 2; end++)
            {
                TroopEndpointLink link = troop.GetLineLink(end);
                if (link == null || link.Other == null || troop.GetInstanceID() >= link.Other.GetInstanceID()) continue;
                if (!forces.ContainsKey(link.Other)) continue;
                float gap = link.Tension.magnitude;
                float extension = Mathf.Max(0f, gap - LinkSlack);
                float tension = Mathf.Min(LinkMaxForce, extension * LinkStiffness);
                Vector2 force = gap > 0.0001f ? link.Tension / gap * tension : Vector2.zero;
                TroopDiagnostics.RecordTension(troop, end, link.Other, link.OtherEnd, link.Tension, tension);

                // Damp relative motion so linked troops travel together instead of oscillating or peeling apart.
                Vector2 relativeVelocity = link.Other.PivotVelocity - troop.PivotVelocity;
                force += relativeVelocity * LinkVelocityMatching;
                if (force.sqrMagnitude <= 0.000001f) continue;
                forces[troop] += force;
                forces[link.Other] -= force;
            }
        }
    }
}
