using UnityEngine;

// Applies a mitigated hit to one of the troop's soldiers and reports the consequences: a soldier death, the whole troop
// dying, or the shock that may break the troop. Overflow and damage past zero HP add no shock (spec §4.6).
public sealed class TroopDamageReceiver
{
    readonly Troop _troop;

    public TroopDamageReceiver(Troop troop)
    {
        _troop = troop;
    }

    // Returns the HP points applied to the soldier (bruise plus bleed, after the HP clamp). A flanked hit counts x3 Shock.
    public AppliedDamage Apply(SoldierAgentState target, MitigationResult mitigation, Vector2 attackerPivotPosition, bool flanked)
    {
        if (target == null || !_troop.Roster.Contains(target) || !target.IsAlive) return default;

        AppliedDamage applied = target.ApplyMitigation(mitigation);
        if (applied.Total <= 0) return applied;

        if (!target.IsAlive)
            _troop.RaiseSoldierDied(target);
        if (_troop.LivingSoldierCount == 0)
        {
            _troop.OnAllSoldiersDead();
            return applied;
        }

        _troop.MoraleState.AddShock(applied.Shock, flanked, attackerPivotPosition);
        _troop.EvaluateBreak();
        return applied;
    }
}
