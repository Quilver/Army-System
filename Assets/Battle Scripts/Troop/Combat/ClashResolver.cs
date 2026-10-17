using UnityEngine;

// Resolves a melee clash between two opposing soldiers: knock-back, pressure contest and damage to the loser.
public sealed class ClashResolver
{
    const float ClashPulseDuration = 0.2f;
    readonly SoldierContactIndex _index;

    public ClashResolver(SoldierContactIndex index)
    {
        _index = index;
    }

    public void Resolve(SoldierContact a, SoldierContact b, Vector2 positionA, Vector2 positionB, float actionDistance, bool aInitiated)
    {
        a.Troop.MeleeMetrics.RecordClash();
        b.Troop.MeleeMetrics.RecordClash();
        Vector2 towardB = (positionB - positionA).normalized;
        float impactDistance = Mathf.Min(0.6f, Mathf.Min(a.Troop.Radius, b.Troop.Radius) * 0.06f);
        float maxSpeed = Mathf.Max(
            a.Soldier.Profile != null ? a.Soldier.Profile.MaxSpeed : 0f,
            b.Soldier.Profile != null ? b.Soldier.Profile.MaxSpeed : 0f);
        float impactSpeed = Mathf.Max(maxSpeed * 1.7f, 5.5f);
        a.Troop.ResolveContact(a.Soldier, -towardB * impactDistance, Vector2.zero);
        b.Troop.ResolveContact(b.Soldier, towardB * impactDistance, Vector2.zero);
        a.Troop.ResolveContact(a.Soldier, Vector2.zero, -towardB * impactSpeed);
        b.Troop.ResolveContact(b.Soldier, Vector2.zero, towardB * impactSpeed);
        a.Troop.PulseMeleeClash(a.Soldier, ClashPulseDuration);
        b.Troop.PulseMeleeClash(b.Soldier, ClashPulseDuration);

        float pressureA = ClashPressure(a, towardB) + Random.Range(-0.12f, 0.12f);
        float pressureB = ClashPressure(b, -towardB) + Random.Range(-0.12f, 0.12f);
        TroopDiagnostics.RecordClash(a, b, positionA, positionB, aInitiated, pressureA, pressureB);
        if (pressureA >= pressureB)
            ResolveWinner(a, b, pressureA - pressureB, actionDistance);
        else
            ResolveWinner(b, a, pressureB - pressureA, actionDistance);
    }

    float ClashPressure(SoldierContact contact, Vector2 towardOpponent)
    {
        SoldierProfile profile = contact.Soldier.Profile;
        if (profile == null) return 0f;
        float forwardSpeed = Mathf.Max(0f, Vector2.Dot(contact.Troop.WorldVelocity(contact.Soldier), towardOpponent))
            / Mathf.Max(0.1f, profile.MaxSpeed);
        int nearbySupport = 0;
        var contacts = _index.Contacts;
        Vector2 position = contact.Troop.WorldPosition(contact.Soldier);
        for (int i = 0; i < contacts.Count; i++)
        {
            SoldierContact ally = contacts[i];
            if (ally.Troop.TeamId != contact.Troop.TeamId || ally.Soldier == contact.Soldier) continue;
            if (Vector2.Distance(ally.Position, position) < 3.3f)
                nearbySupport++;
        }
        return forwardSpeed * 0.55f
            + (contact.Soldier.ChargeTarget != null ? 0.65f : 0f)
            + profile.Aggression / TroopConstants.MaxAggression * 0.3f
            + profile.MeleeSkill / 100f * 0.5f
            + Mathf.Min(5, nearbySupport) * 0.12f;
    }

    static void ResolveWinner(SoldierContact winner, SoldierContact loser, float pressureMargin, float actionDistance)
    {
        // A clash the loser's troop is still recovering from does not count as won.
        if (!loser.Troop.RecordClashLoss(pressureMargin))
        {
            TroopDiagnostics.RecordClashOutcome(winner, loser, pressureMargin, 0f, false);
            return;
        }
        winner.Troop.MeleeMetrics.RecordHit();

        if (winner.Troop.State == UnitState.Fleeing || winner.Soldier.Profile == null || winner.Soldier.Profile.MeleeDamage <= 0f)
        {
            TroopDiagnostics.RecordClashOutcome(winner, loser, pressureMargin, 0f, true);
            return;
        }
        float damage = winner.Soldier.Profile.MeleeDamage
            * Mathf.Lerp(0.75f, 1.25f, Mathf.Clamp01(winner.Soldier.Profile.MeleeSkill / 100f))
            * FlankMultiplier(
                loser.Troop,
                (Vector2)winner.Troop.transform.position - (Vector2)loser.Troop.transform.position);
        float applied = loser.Troop.ApplyDamage(loser.Soldier, damage, winner.Troop.transform.position, actionDistance);
        winner.Troop.MeleeMetrics.RecordDamageDone(applied);
        loser.Troop.MeleeMetrics.RecordDamageReceived(applied);
        TroopDiagnostics.RecordClashOutcome(winner, loser, pressureMargin, applied, true);
    }

    static float FlankMultiplier(Troop defender, Vector2 directionToAttacker)
    {
        Vector2 facing = defender.Formation != null ? defender.Formation.Facing : (Vector2)defender.transform.right;
        float angle = Vector2.Angle(facing, directionToAttacker);
        if (angle < 45f) return 4f;
        if (angle < 120f) return 2f;
        return 1f;
    }
}