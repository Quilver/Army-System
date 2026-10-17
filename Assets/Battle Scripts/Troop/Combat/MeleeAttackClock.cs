using System.Collections.Generic;
using UnityEngine;

// Drives melee attack timing for every troop, once per frame after clashes are resolved.
// Each troop has a strike beat every TroopConstants.MeleeBeatInterval seconds. The beat phase is random per troop, so
// troops do not strike in step with each other. Soldiers with Discipline hold some attacks for the beat (see
// SoldierMeleeState).
public sealed class MeleeAttackClock
{
    readonly Dictionary<Troop, float> _timeToBeat = new();

    public void Tick(IReadOnlyList<Troop> troops, float deltaTime)
    {
        foreach (var troop in troops)
        {
            if (troop == null) continue;
            bool beat = AdvanceBeat(troop, deltaTime);
            IReadOnlyList<SoldierAgentState> soldiers = troop.Soldiers;
            for (int i = 0; i < soldiers.Count; i++)
            {
                SoldierAgentState soldier = soldiers[i];
                if (!soldier.IsAlive) continue;
                // Attacks accrue for the whole troop while it is in melee, not only for soldiers touching an enemy.
                if (troop.InMelee) soldier.Melee.MarkEngaged();
                SoldierProfile profile = soldier.Profile;
                float attacksPerSecond = profile != null ? profile.Aggression / TroopConstants.PerMeleeTime : 0f;
                float holdChance = profile != null
                    ? Mathf.Clamp01(profile.Discipline / TroopConstants.MaxDiscipline)
                    : 0f;
                soldier.Melee.Tick(attacksPerSecond, holdChance, beat, deltaTime);
            }
        }
    }

    // Returns true on the frame the troop's beat fires.
    bool AdvanceBeat(Troop troop, float deltaTime)
    {
        if (!_timeToBeat.TryGetValue(troop, out float remaining))
            remaining = Random.Range(0f, TroopConstants.MeleeBeatInterval);

        remaining -= deltaTime;
        bool beat = remaining <= 0f;
        if (beat) remaining = Mathf.Repeat(remaining, TroopConstants.MeleeBeatInterval);
        _timeToBeat[troop] = remaining;
        return beat;
    }
}
