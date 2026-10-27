using System.Collections.Generic;
using UnityEngine;

// Charge scheduler (spec §4.3, §4.4). Each frame every soldier of an engaged troop charges with probability
// 1 - exp(-rate x dt), rate = EffectiveAggression / ActionWindowSeconds, so it makes about Aggression charges per window
// (Aggression + 5 during Assault). There is no neighbour contagion and no cooldown after a charge (Q-29, Q-30).
//   Target: one of the ChargeCandidateCount nearest enemy soldiers within the charge range (7.8 units), at random.
//   Followers: the troop's Discipline nearest same-troop soldiers each follow with ChargeFollowProbability. They are
//   passive: they stay near the charger and support its clash, and return to the line when the charge ends.
//   Cancel: the target dies, the charger's troop breaks, or ChargeTimeoutSeconds passes (no retargeting, Q-27).
// Candidate search uses the contact index (a spatial query), not a loop over every soldier.
public sealed class ChargeCoordinator
{
    readonly SoldierContactIndex _index;
    readonly TroopRegistry _registry;
    readonly List<int> _nearby = new();
    readonly List<(int index, float distance)> _candidates = new();
    readonly List<(SoldierAgentState soldier, float distance)> _friends = new();

    public ChargeCoordinator(SoldierContactIndex index, TroopRegistry registry)
    {
        _index = index;
        _registry = registry;
    }

    public void Update(float deltaTime)
    {
        TickActiveCharges(deltaTime);
        StartNewCharges(deltaTime);
    }

    void TickActiveCharges(float deltaTime)
    {
        foreach (var troop in _registry.Troops)
        {
            if (troop == null) continue;
            bool broken = troop.IsFaultSlipping || troop.IsRouted;
            foreach (var soldier in troop.Soldiers)
            {
                if (soldier.ChargeRole == ChargeRole.None) continue;
                soldier.TickCharge(deltaTime);
                if (!soldier.IsAlive || broken || soldier.ChargeTimeRemaining <= 0f)
                {
                    soldier.CancelCharge();
                    continue;
                }

                if (soldier.ChargeRole == ChargeRole.Charger)
                {
                    Troop targetTroop = soldier.ChargeTargetTroop;
                    bool valid = targetTroop != null
                        && targetTroop.TeamId != troop.TeamId
                        && soldier.ChargeTarget != null
                        && soldier.ChargeTarget.IsAlive
                        && targetTroop.Roster.Contains(soldier.ChargeTarget);
                    if (!valid) soldier.CancelCharge();
                }
                else
                {
                    // A follower returns to the line once its charger's charge has ended.
                    SoldierAgentState leader = soldier.ChargeLeader;
                    if (leader == null || !leader.IsAlive || !leader.IsCharging) soldier.CancelCharge();
                }
            }
        }
    }

    void StartNewCharges(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        TroopTuning tuning = TroopTuning.Active;
        float now = Time.time;
        foreach (var troop in _registry.Troops)
        {
            if (troop == null || !troop.IsEngaged || troop.IsRouted || troop.IsFaultSlipping) continue;
            ITroopRandom random = troop.TroopRandom;
            IReadOnlyList<SoldierAgentState> soldiers = troop.Soldiers;
            for (int i = 0; i < soldiers.Count; i++)
            {
                SoldierAgentState soldier = soldiers[i];
                if (!soldier.IsAlive || soldier.Profile == null || soldier.IsCommitted) continue;

                float chance = ChargeRules.ChargeProbability(troop.EffectiveAggression(soldier.Profile),
                    tuning.ActionWindowSeconds, deltaTime);
                if (random.Value() >= chance) continue;

                Vector2 position = troop.WorldPosition(soldier);
                if (!TryPickTarget(troop, position, tuning.ChargeRange, tuning.ChargeCandidateCount, random,
                        out SoldierContact target))
                    continue;

                soldier.BeginCharge(target.Soldier, target.Troop, tuning.ChargeTimeoutSeconds, now);
                troop.MeleeMetrics.RecordChargeStarted();
                PickFollowers(troop, soldier, position, tuning, random, now);
            }
        }
    }

    // A random pick among the nearest candidateCount enemy soldiers within range.
    bool TryPickTarget(Troop troop, Vector2 position, float range, int candidateCount, ITroopRandom random,
        out SoldierContact target)
    {
        target = default;
        _index.GatherWithin(position, range, _nearby);
        _candidates.Clear();
        var contacts = _index.Contacts;
        foreach (int i in _nearby)
        {
            SoldierContact contact = contacts[i];
            if (contact.Troop == null || contact.Troop.TeamId == troop.TeamId || !contact.Soldier.IsAlive) continue;
            _candidates.Add((i, (contact.Position - position).sqrMagnitude));
        }
        if (_candidates.Count == 0) return false;

        _candidates.Sort((a, b) => a.distance.CompareTo(b.distance));
        int pool = Mathf.Min(Mathf.Max(1, candidateCount), _candidates.Count);
        int pick = Mathf.Min(pool - 1, Mathf.FloorToInt(random.Value() * pool));
        target = contacts[_candidates[pick].index];
        return true;
    }

    // The Discipline nearest living, uncommitted soldiers of the same troop (Q-9) each follow with the follow chance.
    void PickFollowers(Troop troop, SoldierAgentState charger, Vector2 chargerPosition, TroopTuning tuning,
        ITroopRandom random, float now)
    {
        int count = Mathf.Max(0, troop.Discipline);
        if (count == 0) return;
        _friends.Clear();
        foreach (var soldier in troop.Soldiers)
        {
            if (soldier == charger || !soldier.IsAlive || soldier.IsCommitted) continue;
            _friends.Add((soldier, (troop.WorldPosition(soldier) - chargerPosition).sqrMagnitude));
        }
        _friends.Sort((a, b) => a.distance.CompareTo(b.distance));
        int limit = Mathf.Min(count, _friends.Count);
        for (int i = 0; i < limit; i++)
        {
            if (random.Value() >= tuning.ChargeFollowProbability) continue;
            _friends[i].soldier.BeginFollow(charger, tuning.ChargeTimeoutSeconds, now);
            troop.MeleeMetrics.RecordFollowerJoined();
        }
    }
}
