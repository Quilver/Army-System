using System.Collections.Generic;
using UnityEngine;

// Finds soldier pairs within action distance of each other, bounces overlapping ones apart, starts clashes between
// opposing pairs and tracks which troops are in melee.
public sealed class SoldierContactResolver
{
    public const float SoldierContactDistance = 0.85f;

    readonly SoldierContactIndex _index;
    readonly ClashResolver _clash;
    readonly HashSet<Troop> _engagedTroops = new();

    public SoldierContactResolver(SoldierContactIndex index, ClashResolver clash)
    {
        _index = index;
        _clash = clash;
    }

    // Clash distance is the action distance (A_Distance).
    public void Resolve(IReadOnlyList<Troop> troops, float actionDistance)
    {
        _engagedTroops.Clear();
        float clashDistanceSquared = actionDistance * actionDistance;
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
                    ResolvePair(a, contacts[j], clashDistanceSquared, actionDistance);
                }
            }
        }

        foreach (var troop in troops)
            if (troop != null) troop.ConstrainSoldiersToRadius();
    }

    public void ApplyMeleeStates(IReadOnlyList<Troop> troops)
    {
        foreach (var troop in troops)
            if (troop != null) troop.SetMeleeState(_engagedTroops.Contains(troop));
    }

    void ResolvePair(SoldierContact a, SoldierContact b, float clashDistanceSquared, float actionDistance)
    {
        if (b.Troop == null || !a.Soldier.IsAlive || !b.Soldier.IsAlive) return;
        if (a.Troop != b.Troop
            && a.Troop.TeamId == b.Troop.TeamId
            && !a.Troop.HasThreatFacingFormation
            && !b.Troop.HasThreatFacingFormation)
            return;
        Vector2 positionA = a.Troop.WorldPosition(a.Soldier);
        Vector2 positionB = b.Troop.WorldPosition(b.Soldier);
        Vector2 difference = positionA - positionB;
        float distanceSquared = difference.sqrMagnitude;
        if (distanceSquared >= clashDistanceSquared) return;

        Vector2 normal = distanceSquared > 0.000001f
            ? difference / Mathf.Sqrt(distanceSquared)
            : (a.Troop.GetInstanceID() < b.Troop.GetInstanceID() ? Vector2.right : Vector2.left);
        float distance = Mathf.Sqrt(distanceSquared);
        // Soldiers of the same troop pass through each other, so only soldiers from different troops bounce.
        if (a.Troop != b.Troop && distance < SoldierContactDistance)
            ResolveBounce(a, b, normal, distance);

        if (a.Troop.TeamId == b.Troop.TeamId) return;
        a.Soldier.Melee.MarkEngaged();
        b.Soldier.Melee.MarkEngaged();
        _engagedTroops.Add(a.Troop);
        _engagedTroops.Add(b.Troop);
        // A clash is started by an attack: one of the two soldiers in contact spends a banked attack to begin it.
        if (!TryPickInitiator(a, b, out SoldierContact initiator)) return;
        initiator.Soldier.TrySpendMeleeAttack();
        initiator.Troop.MeleeMetrics.RecordAttack();
        SoldierContact opponent = initiator.Soldier == a.Soldier ? b : a;
        initiator.Soldier.Melee.BeginLunge(opponent.Soldier, opponent.Troop, TroopConstants.LungeDuration);
        _clash.Resolve(a, b, positionA, positionB, actionDistance, initiator.Soldier == a.Soldier);
    }

    // Chooses which soldier of the pair starts the clash. Returns false if neither has a banked attack.
    static bool TryPickInitiator(SoldierContact a, SoldierContact b, out SoldierContact initiator)
    {
        bool aReady = a.Soldier.HasMeleeAttack;
        bool bReady = b.Soldier.HasMeleeAttack;
        if (aReady && bReady) initiator = Random.value < 0.5f ? a : b;
        else if (aReady) initiator = a;
        else if (bReady) initiator = b;
        else
        {
            initiator = default;
            return false;
        }
        return true;
    }

    static void ResolveBounce(SoldierContact a, SoldierContact b, Vector2 normal, float distance)
    {
        float penetration = SoldierContactDistance - distance;
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