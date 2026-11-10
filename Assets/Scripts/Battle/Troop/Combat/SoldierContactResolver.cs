using System.Collections.Generic;
using UnityEngine;

// Finds soldier pairs in contact (centres closer than ContactDistance, spec §4.5), bounces soldiers of different troops
// apart, and starts clashes between opposing soldiers (ClashRules):
//   (a) a charger in contact with an enemy soldier, or
//   (b) two soldiers in contact with neither charging, once each is off its clash cooldown (Q-26).
// Charge followers never start a clash of their own. Engagement is not decided here (see TroopEngagementSystem).
public sealed class SoldierContactResolver
{
    const float ContactDistance = TroopConstants.ContactDistance;

    readonly SoldierContactIndex _index;
    readonly DuelResolver _duel;

    public SoldierContactResolver(SoldierContactIndex index, DuelResolver duel)
    {
        _index = index;
        _duel = duel;
    }

    public void Resolve(IReadOnlyList<Troop> troops)
    {
        var contacts = _index.Contacts;
        for (int i = 0; i < contacts.Count; i++)
        {
            SoldierContact a = contacts[i];
            if (a.Troop == null || !a.Soldier.IsAlive) continue;
            Vector2Int cell = SoldierContactIndex.CellFor(a.Position);
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            {
                if (!_index.TryGetBucket(cell + new Vector2Int(x, y), out var bucket)) continue;
                foreach (int j in bucket)
                {
                    if (j <= i) continue;
                    ResolvePair(a, contacts[j]);
                }
            }
        }

        foreach (var troop in troops)
            if (troop != null) troop.ConstrainSoldiersToRadius();
    }

    void ResolvePair(SoldierContact a, SoldierContact b)
    {
        // Soldiers of the same troop pass through each other; allies of different troops only bounce in a battle line.
        if (b.Troop == null || a.Troop == b.Troop || !a.Soldier.IsAlive || !b.Soldier.IsAlive) return;
        bool opposing = a.Troop.TeamId != b.Troop.TeamId;
        if (!opposing && !a.Troop.HasThreatFacingFormation && !b.Troop.HasThreatFacingFormation) return;

        Vector2 positionA = a.Troop.WorldPosition(a.Soldier);
        Vector2 positionB = b.Troop.WorldPosition(b.Soldier);
        Vector2 difference = positionA - positionB;
        float distanceSquared = difference.sqrMagnitude;
        if (distanceSquared >= ContactDistance * ContactDistance) return;

        float distance = Mathf.Sqrt(distanceSquared);
        Vector2 normal = distance > 0.000001f
            ? difference / distance
            : (a.Troop.GetInstanceID() < b.Troop.GetInstanceID() ? Vector2.right : Vector2.left);
        ResolveBounce(a, b, normal, distance);
        if (!opposing) return;

        ClashKind kind = ClashRules.Classify(a.Soldier.IsCharging, b.Soldier.IsCharging,
            a.Soldier.IsFollowing, b.Soldier.IsFollowing,
            a.Soldier.ClashCooldownRemaining, b.Soldier.ClashCooldownRemaining);
        if (kind == ClashKind.None) return;
        _duel.Resolve(a, b);
    }

    static void ResolveBounce(SoldierContact a, SoldierContact b, Vector2 normal, float distance)
    {
        float penetration = ContactDistance - distance;
        a.Troop.ResolveContact(a.Soldier, normal * (penetration * 0.5f), Vector2.zero);
        b.Troop.ResolveContact(b.Soldier, -normal * (penetration * 0.5f), Vector2.zero);

        Vector2 relativeVelocity = a.Troop.WorldVelocity(a.Soldier) - b.Troop.WorldVelocity(b.Soldier);
        float closingSpeed = Vector2.Dot(relativeVelocity, normal);
        if (closingSpeed >= 0f) return;

        float impulse = -closingSpeed * 0.25f;
        a.Troop.ResolveContact(a.Soldier, Vector2.zero, normal * impulse);
        b.Troop.ResolveContact(b.Soldier, Vector2.zero, -normal * impulse);
    }
}
