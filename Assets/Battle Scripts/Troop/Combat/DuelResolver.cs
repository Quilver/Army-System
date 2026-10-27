using UnityEngine;

// Resolves one clash between two opposing soldiers in contact (spec §4.5, §4.6):
//   knock-back on both, then a duel on Combat Power: P_A = CP_A / (CP_A + CP_B), roll R in [0, 100], A wins when
//   R <= P_A x 100. The winner deals Strength + weapon damage + Domination, Domination = round(|R - P_A%| / 10),
//   doubled for a charging Lance bearer. The damage goes through the target's armour (MitigationResolver).
//   A is the charger; if both charge, the earlier charge; if neither, the lower soldier id (Q-28).
//   A soldier fighting an enemy on its troop's flank or rear gets no support, and the Shock it takes is x3 (spec §4.8).
//   A clash ends any charge of either soldier, and both soldiers start their clash cooldown.
public sealed class DuelResolver
{
    const float ClashPulseDuration = 0.2f;
    readonly SupportCalculator _support;

    public DuelResolver(SupportCalculator support)
    {
        _support = support;
    }

    public void Resolve(SoldierContact first, SoldierContact second)
    {
        TroopTuning tuning = TroopTuning.Active;
        Vector2 positionFirst = first.Troop.WorldPosition(first.Soldier);
        Vector2 positionSecond = second.Troop.WorldPosition(second.Soldier);
        TroopDebugSnapshot.RecordClash((positionFirst + positionSecond) * 0.5f);
        first.Troop.MeleeMetrics.RecordClash();
        second.Troop.MeleeMetrics.RecordClash();
        KnockBack(first, second, positionFirst, positionSecond);

        bool firstIsA = FirstIsA(first, second);
        SoldierContact a = firstIsA ? first : second;
        SoldierContact b = firstIsA ? second : first;
        Vector2 positionA = firstIsA ? positionFirst : positionSecond;
        Vector2 positionB = firstIsA ? positionSecond : positionFirst;

        bool aFlanked = a.Troop.IsFlankedBy(b.Troop);
        bool bFlanked = b.Troop.IsFlankedBy(a.Troop);
        int powerA = Power(a, positionA, aFlanked);
        int powerB = Power(b, positionB, bFlanked);
        float roll = a.Troop.TroopRandom.Range(0f, 100f);
        DuelOutcome outcome = DuelRules.Resolve(powerA, powerB, roll, tuning.DominationDivisor);

        SoldierContact winner = outcome.AWins ? a : b;
        SoldierContact loser = outcome.AWins ? b : a;
        bool loserFlanked = outcome.AWins ? bFlanked : aFlanked;
        winner.Troop.MeleeMetrics.RecordDuelWin();
        ApplyDamage(winner, loser, outcome.Domination, loserFlanked);

        EndCharge(first.Soldier);
        EndCharge(second.Soldier);
        first.Soldier.StartClashCooldown(tuning.ClashCooldownSeconds);
        second.Soldier.StartClashCooldown(tuning.ClashCooldownSeconds);
    }

    static bool FirstIsA(SoldierContact first, SoldierContact second)
    {
        SoldierAgentState x = first.Soldier;
        SoldierAgentState y = second.Soldier;
        if (!x.IsCharging && !y.IsCharging && x.Id == y.Id)
            return first.Troop.GetInstanceID() <= second.Troop.GetInstanceID();
        return DuelRules.FirstIsA(x.IsCharging, x.ChargeStartTime, x.Id, y.IsCharging, y.ChargeStartTime, y.Id);
    }

    int Power(SoldierContact contact, Vector2 position, bool flanked)
    {
        TroopTuning tuning = TroopTuning.Active;
        SoldierProfile profile = contact.Soldier.Profile;
        if (profile == null) return 0;
        int support = _support.SupportBonus(contact.Troop, contact.Soldier, position, flanked);
        return CombatPower.Compute(profile.MeleeWeaponSkill, contact.Soldier.IsCharging, profile.Charge,
            profile.MeleeWeaponChargeBonus, contact.Troop.IsAssaulting, tuning.AssaultChargeBonus, support);
    }

    // A fleeing troop (retreating or routed) that wins a clash deals no damage (Q-37).
    static void ApplyDamage(SoldierContact winner, SoldierContact loser, int domination, bool loserFlanked)
    {
        SoldierProfile profile = winner.Soldier.Profile;
        if (profile == null || winner.Troop.State == UnitState.Fleeing) return;
        bool lanceDouble = profile.HasLance && winner.Soldier.IsCharging;
        int raw = DamageRules.RawDamage(profile.Strength, profile.MeleeWeaponDamage, domination, lanceDouble);
        if (raw <= 0) return;
        int applied = loser.Troop.ApplyDamage(loser.Soldier, raw, winner.Troop.transform.position, loserFlanked);
        winner.Troop.MeleeMetrics.RecordDamageDone(applied);
        loser.Troop.MeleeMetrics.RecordDamageReceived(applied);
    }

    // One charge = one clash: the charge ends on contact and the charger's followers return to the line.
    static void EndCharge(SoldierAgentState soldier)
    {
        if (soldier.IsCharging) soldier.CancelCharge();
    }

    // Knock-back on both soldiers (magnitudes kept from the earlier clash code, provisional).
    static void KnockBack(SoldierContact a, SoldierContact b, Vector2 positionA, Vector2 positionB)
    {
        Vector2 towardB = positionB - positionA;
        towardB = towardB.sqrMagnitude > 1e-8f ? towardB.normalized : Vector2.right;
        float impactDistance = Mathf.Min(0.6f, Mathf.Min(a.Troop.Radius, b.Troop.Radius) * 0.06f);
        float maxSpeed = Mathf.Max(
            a.Soldier.Profile != null ? a.Soldier.Profile.MaxSpeed : 0f,
            b.Soldier.Profile != null ? b.Soldier.Profile.MaxSpeed : 0f);
        float impactSpeed = Mathf.Max(maxSpeed * 1.7f, 5.5f);
        a.Troop.ResolveContact(a.Soldier, -towardB * impactDistance, -towardB * impactSpeed);
        b.Troop.ResolveContact(b.Soldier, towardB * impactDistance, towardB * impactSpeed);
        a.Troop.PulseMeleeClash(a.Soldier, ClashPulseDuration);
        b.Troop.PulseMeleeClash(b.Soldier, ClashPulseDuration);
    }
}
