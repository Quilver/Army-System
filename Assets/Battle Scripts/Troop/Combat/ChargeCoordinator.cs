using System.Collections.Generic;
using UnityEngine;

// Decides when soldiers start, keep or cancel a charge at an enemy soldier. Charging spreads through a troop like an
// Ising model: soldiers are more likely to charge when their neighbours already are.
public sealed class ChargeCoordinator
{
    readonly SoldierContactIndex _index;
    readonly TroopRegistry _registry;
    readonly List<ChargeRequest> _pending = new();

    readonly struct ChargeRequest
    {
        public readonly SoldierAgentState Source;
        public readonly SoldierAgentState Target;
        public readonly Troop TargetTroop;
        public readonly Troop SourceTroop;

        public ChargeRequest(Troop sourceTroop, SoldierAgentState source, SoldierAgentState target, Troop targetTroop)
        {
            SourceTroop = sourceTroop;
            Source = source;
            Target = target;
            TargetTroop = targetTroop;
        }
    }

    public ChargeCoordinator(SoldierContactIndex index, TroopRegistry registry)
    {
        _index = index;
        _registry = registry;
    }

    public void Update(float actionDistance, float deltaTime)
    {
        TickActiveCharges(actionDistance, deltaTime);
        CollectNewCharges(actionDistance, deltaTime);
        foreach (var charge in _pending)
        {
            float duration = Random.Range(0.75f, 1.15f);
            float cooldown = Random.Range(1.2f, 3f);
            charge.Source.SetCharge(charge.Target, charge.TargetTroop, duration, cooldown);
            TroopDiagnostics.RecordChargeStart(charge.SourceTroop, charge.Source, charge.TargetTroop, charge.Target,
                Vector2.Distance(charge.SourceTroop.WorldPosition(charge.Source), charge.TargetTroop.WorldPosition(charge.Target)),
                duration);
        }
    }

    void TickActiveCharges(float actionDistance, float deltaTime)
    {
        foreach (var troop in _registry.Troops)
        {
            if (troop == null) continue;
            foreach (var soldier in troop.Soldiers)
            {
                soldier.TickCharge(deltaTime);
                if (soldier.ChargeTarget == null) continue;
                bool targetValid = soldier.ChargeTargetTroop != null
                    && soldier.ChargeTargetTroop.TeamId != troop.TeamId
                    && _registry.Contains(soldier.ChargeTargetTroop)
                    && soldier.ChargeTarget.IsAlive
                    && soldier.ChargeTargetTroop.Roster.Contains(soldier.ChargeTarget);
                float distance = targetValid
                    ? Vector2.Distance(troop.WorldPosition(soldier), soldier.ChargeTargetTroop.WorldPosition(soldier.ChargeTarget))
                    : float.PositiveInfinity;
                // Same cancel condition as before, split so the end reason can be logged.
                bool invalidTarget = !targetValid;
                bool faultSlipping = troop.IsFaultSlipping;
                bool timedOut = soldier.ChargeTimeRemaining <= 0f;
                bool outOfRange = distance > actionDistance * 1.7f;
                if (invalidTarget || faultSlipping || timedOut || outOfRange)
                {
                    string reason = invalidTarget ? "invalid target" : faultSlipping ? "fault slip" : timedOut ? "time out" : "out of range";
                    TroopDiagnostics.RecordChargeEnd(troop, soldier, reason, distance);
                    soldier.CancelCharge(1.1f);
                }
            }
        }
    }

    void CollectNewCharges(float actionDistance, float deltaTime)
    {
        _pending.Clear();
        var contacts = _index.Contacts;
        for (int i = 0; i < contacts.Count; i++)
        {
            SoldierContact source = contacts[i];
            SoldierProfile profile = source.Soldier.Profile;
            if (profile == null || profile.Aggression <= 0f || source.Troop.IsPulseLocked
                || source.Troop.State == UnitState.Fleeing || source.Soldier.ChargeTarget != null
                || source.Soldier.ChargeCooldownRemaining > 0f)
                continue;

            SoldierContact? target = FindChargeTarget(source, actionDistance);
            if (!target.HasValue) continue;

            float localCharge = LocalChargeFraction(source);
            float aggression = profile.Aggression / TroopConstants.MaxAggression;
            float field = aggression * 2.4f - 1.7f + localCharge * 2.4f;
            float isingProbability = 1f / (1f + Mathf.Exp(-field));
            float rate = aggression * (0.08f + 0.25f * isingProbability + 0.4f * localCharge);
            if (Random.value >= 1f - Mathf.Exp(-rate * deltaTime)) continue;
            _pending.Add(new ChargeRequest(source.Troop, source.Soldier, target.Value.Soldier, target.Value.Troop));
        }
    }

    // Fraction of the source's troop-mates within range that are already charging.
    float LocalChargeFraction(SoldierContact source)
    {
        int neighbors = 0;
        int chargingNeighbors = 0;
        var contacts = _index.Contacts;
        for (int i = 0; i < contacts.Count; i++)
        {
            SoldierContact ally = contacts[i];
            if (ally.Troop != source.Troop || ally.Soldier == source.Soldier) continue;
            if (Vector2.Distance(source.Position, ally.Position) > 5.3f) continue;
            neighbors++;
            if (ally.Soldier.ChargeTarget != null) chargingNeighbors++;
        }
        return neighbors > 0 ? (float)chargingNeighbors / neighbors : 0f;
    }

    SoldierContact? FindChargeTarget(SoldierContact source, float actionDistance)
    {
        float nearestDistance = actionDistance;
        SoldierContact? nearest = null;
        Vector2 facing = source.Troop.Formation != null
            ? source.Troop.Formation.Facing
            : (Vector2)source.Troop.transform.right;
        float frontDepth = Vector2.Dot(source.Position - (Vector2)source.Troop.transform.position, facing);
        if (frontDepth < -source.Troop.Radius * 0.12f) return null;

        var contacts = _index.Contacts;
        for (int i = 0; i < contacts.Count; i++)
        {
            SoldierContact candidate = contacts[i];
            if (candidate.Troop.TeamId == source.Troop.TeamId) continue;
            Vector2 offset = candidate.Position - source.Position;
            float distance = offset.magnitude;
            if (distance >= nearestDistance || distance <= 0.0001f) continue;
            if (Vector2.Dot(offset / distance, facing) < 0.15f) continue;
            nearestDistance = distance;
            nearest = candidate;
        }
        return nearest;
    }
}