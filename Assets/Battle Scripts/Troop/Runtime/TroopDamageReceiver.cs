using UnityEngine;

// Applies damage to one of the troop's soldiers and reports the consequences: a soldier death, the whole troop dying,
// or accumulated shock that may break the troop.
public sealed class TroopDamageReceiver
{
    public const float DefaultChargeDistance = 5.2f;
    readonly Troop _troop;

    public TroopDamageReceiver(Troop troop)
    {
        _troop = troop;
    }

    public float Apply(SoldierAgentState target, float damage, Vector2 attackerPivotPosition, float chargeDistance)
    {
        if (target == null || !_troop.Roster.Contains(target)) return 0f;
        float appliedDamage = target.ApplyDamage(damage);
        if (appliedDamage <= 0f) return 0f;

        if (target.LifeState == SoldierLifeState.Dead)
            _troop.RaiseSoldierDied(target);
        if (_troop.LivingSoldierCount == 0)
        {
            _troop.KillUnit();
            return appliedDamage;
        }

        _troop.MoraleState.AddShock(appliedDamage, attackerPivotPosition, chargeDistance);
        _troop.TryBeginFaultSlip();
        return appliedDamage;
    }
}