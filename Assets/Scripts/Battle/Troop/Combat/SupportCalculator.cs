using System.Collections.Generic;
using UnityEngine;

// Works out the support of a clashing soldier (spec §4.5, §4.8, SU-149) with a spatial query on the contact index.
//   Eligible: same army (team), alive and not fatigued or stunned, within SupportRadius of the clashing soldier (a Spear
//   Support supporter counts within SupportRadius x 2).
//   Each supporter gives its own Support x SupportBonusPerSupportPoint; the MaxSupporters with the highest Support count
//   (SupportRules.SupportBonus).
//   A soldier clashing with an enemy that attacks its troop's flank or rear gets no support.
public sealed class SupportCalculator
{
    readonly SoldierContactIndex _index;
    readonly List<int> _nearby = new();
    readonly List<int> _supporterSupport = new();

    public SupportCalculator(SoldierContactIndex index)
    {
        _index = index;
    }

    // The Support stat of every eligible supporter, into supporterSupport. Returns how many there are.
    public int CollectSupporters(Troop troop, SoldierAgentState soldier, Vector2 position, List<int> supporterSupport)
    {
        supporterSupport.Clear();
        TroopTuning tuning = TroopTuning.Active;
        float radius = tuning.SupportRadius;
        float maxRange = radius * Mathf.Max(1f, tuning.SpearSupportRangeMultiplier);
        _index.GatherWithin(position, maxRange, _nearby);

        var contacts = _index.Contacts;
        foreach (int i in _nearby)
        {
            SoldierContact ally = contacts[i];
            if (ally.Soldier == soldier || ally.Troop == null || ally.Troop.TeamId != troop.TeamId) continue;
            if (!ally.Soldier.CanSupport) continue;
            CharacterProfile profile = ally.Soldier.Character;
            float range = SupportRules.SupporterRange(radius, tuning.SpearSupportRangeMultiplier,
                profile != null && profile.HasSpearSupport);
            if (!SupportRules.InSupportRange(Vector2.Distance(ally.Position, position), range)) continue;
            supporterSupport.Add(profile != null ? profile.Support : CombatStatLimits.Default);
        }
        return supporterSupport.Count;
    }

    public int EligibleSupporters(Troop troop, SoldierAgentState soldier, Vector2 position) =>
        CollectSupporters(troop, soldier, position, _supporterSupport);

    public int SupportBonus(Troop troop, SoldierAgentState soldier, Vector2 position, bool flanked)
    {
        if (flanked) return 0;
        TroopTuning tuning = TroopTuning.Active;
        CollectSupporters(troop, soldier, position, _supporterSupport);
        return SupportRules.SupportBonus(_supporterSupport, tuning.MaxSupporters, tuning.SupportBonusPerSupportPoint, false);
    }
}
