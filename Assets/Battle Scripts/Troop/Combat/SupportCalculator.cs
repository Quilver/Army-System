using System.Collections.Generic;
using UnityEngine;

// Counts the supporters of a clashing soldier (spec §4.5, §4.8) with a spatial query on the contact index.
//   Eligible: same army (team), alive and not fatigued, within SupportRadius of the clashing soldier (a Boar Spear
//   supporter counts within SupportRadius x 2).
//   SupportingSoldiers = min(eligible, the clashing soldier's Support). SupportBonus = SupportingSoldiers x 2.
//   A soldier clashing with an enemy that attacks its troop's flank or rear gets no support.
public sealed class SupportCalculator
{
    readonly SoldierContactIndex _index;
    readonly List<int> _nearby = new();

    public SupportCalculator(SoldierContactIndex index)
    {
        _index = index;
    }

    public int EligibleSupporters(Troop troop, SoldierAgentState soldier, Vector2 position)
    {
        TroopTuning tuning = TroopTuning.Active;
        float radius = tuning.SupportRadius;
        float maxRange = radius * Mathf.Max(1f, tuning.BoarSpearSupportRangeMultiplier);
        _index.GatherWithin(position, maxRange, _nearby);

        int eligible = 0;
        var contacts = _index.Contacts;
        foreach (int i in _nearby)
        {
            SoldierContact ally = contacts[i];
            if (ally.Soldier == soldier || ally.Troop == null || ally.Troop.TeamId != troop.TeamId) continue;
            if (!ally.Soldier.CanSupport) continue;
            SoldierProfile profile = ally.Soldier.Profile;
            float range = SupportRules.SupporterRange(radius, tuning.BoarSpearSupportRangeMultiplier,
                profile != null && profile.HasBoarSpear);
            if (SupportRules.InSupportRange(Vector2.Distance(ally.Position, position), range)) eligible++;
        }
        return eligible;
    }

    public int SupportBonus(Troop troop, SoldierAgentState soldier, Vector2 position, bool flanked)
    {
        if (flanked) return 0;
        int maxSupport = soldier.Profile != null ? soldier.Profile.Support : CombatStatLimits.Default;
        int eligible = EligibleSupporters(troop, soldier, position);
        return SupportRules.SupportBonus(eligible, maxSupport, TroopTuning.Active.SupportBonusPerAlly, false);
    }
}
