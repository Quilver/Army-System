using UnityEngine;

// Resolves one clash between two opposing soldiers in contact (spec §4.5, §4.6):
//   knock-back on both, then a duel on Combat Power: P_A = CP_A / (CP_A + CP_B), roll R in [0, 100], A wins when
//   R <= P_A x 100. The winner deals Strength + weapon damage + Domination, Domination = round(|R - P_A%| / 10),
//   doubled for a charging Couched Lance bearer. The damage goes through the target's armour (MitigationResolver).
//   A is the charger; if both charge, the earlier charge; if neither, the lower soldier id (Q-28).
//   A soldier fighting an enemy on its troop's flank or rear gets no support, and the Shock it takes is x3 (spec §4.8).
//   A clash ends any charge of either soldier, and both soldiers start their clash cooldown.
//   A stunned soldier (SU-107) that wins its clash deals no damage (SU-111).
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
        CombatPowerParts partsA = Power(a, positionA, aFlanked);
        CombatPowerParts partsB = Power(b, positionB, bFlanked);
        int powerA = partsA.Total;
        int powerB = partsB.Total;
        float roll = a.Troop.TroopRandom.Range(0f, 100f);
        DuelOutcome outcome = DuelRules.Resolve(powerA, powerB, roll, tuning.DominationDivisor);

        SoldierContact winner = outcome.AWins ? a : b;
        SoldierContact loser = outcome.AWins ? b : a;
        bool loserFlanked = outcome.AWins ? bFlanked : aFlanked;
        winner.Troop.MeleeMetrics.RecordDuelWin();
        var entry = new CombatLogEntry
        {
            Kind = CombatLogKind.Clash,
            Time = CombatLog.Now,
            Attacker = CombatLogSoldier.Of(a.Troop, a.Soldier),
            Defender = CombatLogSoldier.Of(b.Troop, b.Soldier),
            AttackerPower = partsA,
            DefenderPower = partsB,
            ChanceA = powerA + powerB > 0 ? powerA / (float)(powerA + powerB) : 0.5f,
            Roll = roll,
            AttackerWins = outcome.AWins,
        };
        entry.Damage = ApplyDamage(winner, loser, outcome.Domination, loserFlanked,
            outcome.AWins ? positionB - positionA : positionA - positionB);
        CombatLog.Record(entry);

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

    // Combat Power (CombatPower.Compute) with its parts, for the combat log.
    CombatPowerParts Power(SoldierContact contact, Vector2 position, bool flanked)
    {
        TroopTuning tuning = TroopTuning.Active;
        CharacterProfile profile = contact.Soldier.Character;
        bool charging = contact.Soldier.IsCharging;
        bool assault = contact.Troop.IsAssaulting;
        if (profile == null) return new CombatPowerParts { Charging = charging, Assault = assault, Flanked = flanked };
        int support = _support.SupportBonus(contact.Troop, contact.Soldier, position, flanked);
        int total = CombatPower.Compute(profile.MeleeWeaponSkill, charging, profile.Charge,
            profile.MeleeWeaponChargeBonus, assault, tuning.AssaultChargeBonus, support);
        return new CombatPowerParts
        {
            Skill = profile.MeleeWeaponSkill,
            Support = support,
            Charge = total - profile.MeleeWeaponSkill - support,
            ChargeStat = charging ? profile.Charge : 0,
            WeaponCharge = charging ? profile.MeleeWeaponChargeBonus : 0,
            AssaultCharge = charging && assault ? tuning.AssaultChargeBonus : 0,
            Weapon = profile.MeleeWeapon.DisplayName,
            Charging = charging,
            Assault = assault,
            Flanked = flanked,
            Stunned = contact.Soldier.IsStunned,
        };
    }

    // A fleeing troop (retreating or routed) that wins a clash deals no damage (Q-37), nor does a stunned winner (SU-111).
    // A loser that takes damage plays its squash and stretch along pushDirection (from the winner toward the loser).
    // Returns what happened, for the log.
    static CombatLogDamage ApplyDamage(SoldierContact winner, SoldierContact loser, int domination, bool loserFlanked,
        Vector2 pushDirection)
    {
        var log = new CombatLogDamage { Domination = domination };
        CharacterProfile profile = winner.Soldier.Character;
        if (profile == null) { log.NoDamageReason = "no character"; return log; }
        if (winner.Troop.State == TroopState.Fleeing) { log.NoDamageReason = "winner fleeing"; return log; }
        if (winner.Soldier.IsStunned) { log.NoDamageReason = "winner stunned"; return log; }
        bool lanceDouble = profile.HasCouchedLance && winner.Soldier.IsCharging;
        int raw = DamageRules.RawDamage(profile.Strength, profile.MeleeWeaponDamage, domination, lanceDouble);
        log.Strength = profile.Strength;
        log.WeaponDamage = profile.MeleeWeaponDamage;
        log.LanceDoubled = lanceDouble;
        log.Raw = raw;
        if (raw <= 0) { log.NoDamageReason = "0 raw damage"; return log; }
        AppliedDamage taken = loser.Troop.ApplyDamageDetailed(loser.Soldier, raw, winner.Troop.transform.position,
            loserFlanked, winner.Troop, profile.MeleeWeapon, out MitigationResult mitigation);
        int applied = taken.Total;
        winner.Troop.MeleeMetrics.RecordDamageDone(applied);
        loser.Troop.MeleeMetrics.RecordDamageReceived(applied);
        if (applied > 0)
        {
            loser.Troop.PlayHitSquash(loser.Soldier, pushDirection);
            BattleEvents.RaiseSoldierStruck(winner.Troop, loser.Troop, loser.Troop.WorldPosition(loser.Soldier));
        }
        FillDamage(ref log, mitigation, applied, loser.Soldier);
        return log;
    }

    // The armour roll and its result on the target, for the combat log (also used for ranged hits).
    internal static void FillDamage(ref CombatLogDamage log, in MitigationResult mitigation, int applied, SoldierAgentState target)
    {
        log.Dealt = true;
        log.Deflected = mitigation.Deflected;
        log.Bruised = mitigation.Bruised;
        log.Bled = mitigation.Bled;
        log.Overflow = mitigation.Overflow;
        log.AppliedHp = applied;
        log.HpLeft = target != null ? target.CurrentHp : 0;
        log.Killed = target != null && !target.IsAlive;
        log.Stunned = target != null && target.IsStunned;
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
        // 0.3 units: what every radius-5 troop had before the frontage (SU-103), so a clash knocks back as before.
        const float impactDistance = 0.3f;
        float maxSpeed = Mathf.Max(
            a.Soldier.Character != null ? a.Soldier.Character.MaxSpeed : 0f,
            b.Soldier.Character != null ? b.Soldier.Character.MaxSpeed : 0f);
        float impactSpeed = Mathf.Max(maxSpeed * 1.7f, 5.5f);
        a.Troop.ResolveContact(a.Soldier, -towardB * impactDistance, -towardB * impactSpeed);
        b.Troop.ResolveContact(b.Soldier, towardB * impactDistance, towardB * impactSpeed);
        a.Troop.PulseMeleeClash(a.Soldier, ClashPulseDuration);
        b.Troop.PulseMeleeClash(b.Soldier, ClashPulseDuration);
    }
}
